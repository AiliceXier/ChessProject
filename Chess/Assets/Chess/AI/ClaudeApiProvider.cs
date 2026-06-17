using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Chess.AI
{
    /// <summary>
    /// Calls the Claude / Anthropic-compatible Messages API to ask the model
    /// for the best next move in UCI notation. The caller decides whether
    /// the model should reason (thinking) before answering.
    /// </summary>
    public class ClaudeApiProvider
    {
        // Force modern TLS for the legacy .NET Framework HTTP stack that
        // Unity's UnityWebRequest uses on Windows. Without this, handshakes
        // against modern CDNs (e.g. volces.com) often fail silently with
        // "Cannot connect to destination host".
        static ClaudeApiProvider()
        {
            try
            {
                ServicePointManager.SecurityProtocol =
                    SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                ServicePointManager.DefaultConnectionLimit = 16;
                ServicePointManager.Expect100Continue = false;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ClaudeApiProvider] SecurityProtocol init failed: {e.Message}");
            }
        }

        private readonly int _maxTokens;
        private readonly int _thinkingBudget;
        private readonly int _timeoutSeconds;

        public ClaudeApiProvider(int maxTokens = 256, int thinkingBudget = 4000, int timeoutSeconds = 30)
        {
            _maxTokens = maxTokens;
            _thinkingBudget = thinkingBudget;
            _timeoutSeconds = timeoutSeconds;
        }

        /// <summary>
        /// Asks the cloud model for the best move in the current position.
        /// </summary>
        /// <param name="board">A snapshot board. Will not be mutated.</param>
        /// <param name="useThinking">true → extended thinking (depth 5); false → direct (depth 4).</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The matching legal Move, or null if no legal move exists / parsing failed.</returns>
        public async Task<Move> GetBestMoveAsync(ChessBoard board, bool useThinking, CancellationToken ct = default)
        {
            if (board == null) return null;
            var fen = board.ToFen();
            var turn = board.Turn == PieceColor.White ? "White" : "Black";
            var systemPrompt = BuildSystemPrompt();
            var userPrompt   = BuildUserPrompt(fen, turn);

            string body = useThinking
                ? BuildRequestBody(systemPrompt, userPrompt, thinkingEnabled: true)
                : BuildRequestBody(systemPrompt, userPrompt, thinkingEnabled: false);

            string raw;
            try
            {
                raw = await PostAsync(ClaudeConfig.MessagesUrl, body, ct);
            }
            catch (Exception e)
            {
                // Surface both the inner error and a hint about likely causes.
                Debug.LogError($"[ClaudeApiProvider] HTTP call failed: {e.Message}\n" +
                               $"  url={ClaudeConfig.MessagesUrl}\n" +
                               $"  model={ClaudeConfig.Model}\n" +
                               $"  timeout={_timeoutSeconds}s\n" +
                               $"  hint: confirm DNS/firewall allow outbound to volces.com, " +
                               $"and that the .NET TLS stack can negotiate TLS 1.2+.");
                return null;
            }

            string text = ExtractTextBlock(raw);
            if (string.IsNullOrEmpty(text))
            {
                Debug.LogWarning($"[ClaudeApiProvider] No text block in response. Raw: {Truncate(raw, 400)}");
                return null;
            }

            string uci = ExtractUci(text);
            if (string.IsNullOrEmpty(uci))
            {
                Debug.LogWarning($"[ClaudeApiProvider] No UCI move parsed from: {Truncate(text, 200)}");
                return null;
            }

            var move = FindLegalMove(board, uci);
            if (move == null)
            {
                Debug.LogWarning($"[ClaudeApiProvider] UCI {uci} is not a legal move in this position.");
            }
            return move;
        }

        // ---------- Prompt construction ----------

        private static string BuildSystemPrompt()
        {
            return "You are a strong chess engine. " +
                   "You will be given a FEN position and the side to move. " +
                   "Reply with ONE move in standard UCI notation (4 or 5 chars, e.g. e2e4, e7e8q, e1g1 for castling). " +
                   "Do not include any other text, comment, or punctuation. Output the move only.";
        }

        private static string BuildUserPrompt(string fen, string turn)
        {
            return $"FEN: {fen}\nSide to move: {turn}\nBest move (UCI):";
        }

        // ---------- Request body ----------

        private string BuildRequestBody(string system, string user, bool thinkingEnabled)
        {
            var sb = new StringBuilder(512);
            sb.Append('{');
            sb.Append("\"model\":\"").Append(JsonEscape(ClaudeConfig.Model)).Append("\",");
            sb.Append("\"max_tokens\":").Append(_maxTokens).Append(',');

            if (thinkingEnabled)
            {
                sb.Append("\"thinking\":{\"type\":\"enabled\",\"budget_tokens\":").Append(_thinkingBudget).Append("},");
            }
            else
            {
                // Explicitly disable so the gateway does not insert its own thinking block.
                sb.Append("\"thinking\":{\"type\":\"disabled\"},");
            }

            sb.Append("\"system\":\"").Append(JsonEscape(system)).Append("\",");
            sb.Append("\"messages\":[{\"role\":\"user\",\"content\":\"").Append(JsonEscape(user)).Append("\"}]");
            sb.Append('}');
            return sb.ToString();
        }

        // ---------- HTTP ----------

        private async Task<string> PostAsync(string url, string body, CancellationToken ct)
        {
            using (var req = new UnityWebRequest(url, "POST"))
            {
                var bytes = Encoding.UTF8.GetBytes(body);
                req.uploadHandler = new UploadHandlerRaw(bytes);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("x-api-key", ClaudeConfig.ApiKey);
                req.SetRequestHeader("anthropic-version", ClaudeConfig.AnthropicVersion);
                req.timeout = _timeoutSeconds;

                var op = req.SendWebRequest();
                while (!op.isDone)
                {
                    if (ct.IsCancellationRequested) { req.Abort(); break; }
                    await Task.Yield();
                }

                if (req.result != UnityWebRequest.Result.Success)
                {
                    var err = req.downloadHandler != null ? req.downloadHandler.text : "(no body)";
                    throw new Exception($"{req.responseCode} {req.error} :: {Truncate(err, 300)}");
                }

                return req.downloadHandler.text;
            }
        }

        // ---------- Response parsing ----------

        // The Anthropic Messages API returns content as an array of typed blocks
        // (text, thinking, tool_use, ...). We want the FIRST text block.
        private static string ExtractTextBlock(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;
            try
            {
                // Locate every "text":"..." entry; pick the first one.
                var matches = Regex.Matches(raw, "\"text\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"");
                for (int i = 0; i < matches.Count; i++)
                {
                    var decoded = DecodeJsonString(matches[i].Groups[1].Value);
                    if (!string.IsNullOrWhiteSpace(decoded)) return decoded;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ClaudeApiProvider] text-extract failed: {e.Message}");
            }
            return null;
        }

        private static string ExtractUci(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            // UCI move: 4-5 chars, file a-h, rank 1-8, optional promotion piece.
            var m = Regex.Match(text, "\\b([a-h][1-8])([a-h][1-8])([qrbn])?\\b", RegexOptions.IgnoreCase);
            if (!m.Success) return null;
            return m.Value.ToLower();
        }

        // ---------- Move resolution ----------

        private static Move FindLegalMove(ChessBoard board, string uci)
        {
            if (string.IsNullOrEmpty(uci) || uci.Length < 4) return null;
            short fromX = (short)(uci[0] - 'a');
            short fromY = (short)(uci[1] - '1');
            short toX   = (short)(uci[2] - 'a');
            short toY   = (short)(uci[3] - '1');
            if (fromX < 0 || fromX > 7 || toX < 0 || toX > 7) return null;
            if (fromY < 0 || fromY > 7 || toY < 0 || toY > 7) return null;

            var from = new Position { X = fromX, Y = fromY };
            var to   = new Position { X = toX,   Y = toY   };
            var legal = board.Moves(from, generateSan: false);
            foreach (var m in legal)
            {
                if (m.NewPosition.X == to.X && m.NewPosition.Y == to.Y) return m;
            }
            return null;
        }

        // ---------- Helpers ----------

        private static string JsonEscape(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var sb = new StringBuilder(s.Length + 8);
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"':  sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        private static string DecodeJsonString(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (c == '\\' && i + 1 < s.Length)
                {
                    var n = s[++i];
                    switch (n)
                    {
                        case '"':  sb.Append('"');  break;
                        case '\\': sb.Append('\\'); break;
                        case '/':  sb.Append('/');  break;
                        case 'b':  sb.Append('\b'); break;
                        case 'f':  sb.Append('\f'); break;
                        case 'n':  sb.Append('\n'); break;
                        case 'r':  sb.Append('\r'); break;
                        case 't':  sb.Append('\t'); break;
                        case 'u':
                            if (i + 4 < s.Length)
                            {
                                var hex = s.Substring(i + 1, 4);
                                if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int code))
                                {
                                    sb.Append((char)code);
                                    i += 4;
                                }
                            }
                            break;
                        default: sb.Append(n); break;
                    }
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s;
            return s.Substring(0, max) + "…";
        }
    }
}
