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
        /// On any failure (no text, no UCI, illegal UCI) it retries once with the
        /// legal-move list explicitly injected; if that also fails it returns a
        /// random legal move so the game never stalls.
        /// </summary>
        public async Task<Move> GetBestMoveAsync(ChessBoard board, bool useThinking, CancellationToken ct = default)
        {
            if (board == null) return null;

            var fen = board.ToFen();
            var turn = board.Turn == PieceColor.White ? "White" : "Black";

            // First attempt: bare prompt. The model sometimes hallucinates an
            // illegal move (e.g. "c5d4" when the position has no such pawn).
            var move = await TryGetMoveAsync(board, fen, turn, useThinking, includeLegalList: false, ct);
            if (move != null) return move;

            // Second attempt: explicitly constrain to the legal-move list so the
            // model has a closed set to pick from and can't pick an off-board square.
            Debug.LogWarning("[ClaudeApiProvider] First attempt failed; retrying with legal-move constraint.");
            move = await TryGetMoveAsync(board, fen, turn, useThinking, includeLegalList: true, ct);
            if (move != null) return move;

            // Last resort: random legal move so the game continues. Better than
            // stalling the turn and freezing the board on the player's UI.
            move = PickRandomLegalMove(board);
            if (move != null)
            {
                var label = !string.IsNullOrEmpty(move.San) ? move.San : move.ToString();
                Debug.LogWarning($"[ClaudeApiProvider] All API attempts failed; using random legal move: {label}");
            }
            return move;
        }

        private async Task<Move> TryGetMoveAsync(ChessBoard board, string fen, string turn, bool useThinking, bool includeLegalList, CancellationToken ct)
        {
            try
            {
                var systemPrompt = BuildSystemPrompt();
                var userPrompt = includeLegalList
                    ? BuildUserPromptWithLegalList(fen, turn, BuildLegalMoveList(board))
                    : BuildUserPrompt(fen, turn);
                var body = useThinking
                    ? BuildRequestBody(systemPrompt, userPrompt, thinkingEnabled: true)
                    : BuildRequestBody(systemPrompt, userPrompt, thinkingEnabled: false);

                string raw;
                try
                {
                    raw = await PostAsync(ClaudeConfig.MessagesUrl, body, ct);
                }
                catch (Exception e)
                {
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
                    // Fallback: in thinking mode the model can spend the entire max_tokens
                    // budget on the reasoning block and never emit a text block. Try to
                    // fish a move out of the thinking text itself before giving up.
                    string thinking = ExtractThinkingBlock(raw);
                    if (!string.IsNullOrEmpty(thinking))
                    {
                        text = thinking;
                        Debug.LogWarning("[ClaudeApiProvider] No text block; falling back to thinking content for UCI parse.");
                    }
                }
                if (string.IsNullOrEmpty(text))
                {
                    Debug.LogWarning($"[ClaudeApiProvider] No text/thinking block in response. Raw: {Truncate(raw, 400)}");
                    return null;
                }

                string uci = ExtractUci(text);
                Move move = !string.IsNullOrEmpty(uci) ? FindLegalMove(board, uci) : null;
                if (move == null)
                {
                    // The model replied in SAN (e.g. "Nf3", "Bxc6", "O-O") rather
                    // than UCI. Look up the matching legal move by its San field.
                    move = FindMoveBySan(board, text);
                    if (move != null)
                    {
                        Debug.LogWarning($"[ClaudeApiProvider] Parsed SAN instead of UCI from: {Truncate(text, 200)}");
                    }
                }
                if (move == null && !string.IsNullOrEmpty(uci))
                {
                    Debug.LogWarning($"[ClaudeApiProvider] UCI {uci} is not a legal move in this position.");
                }
                return move;
            }
            catch (Exception e)
            {
                // Last line of defense: never let a parser / library exception
                // tear down the AI loop. The outer GetBestMoveAsync will retry
                // or fall back to a random legal move.
                Debug.LogError($"[ClaudeApiProvider] TryGetMoveAsync swallowed {e.GetType().Name}: {e.Message}");
                return null;
            }
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

        // Constrained retry: tell the model exactly which SAN moves are legal
        // so it can't hallucinate a square that has no piece on it.
        private static string BuildUserPromptWithLegalList(string fen, string turn, string legalSanList)
        {
            return $"FEN: {fen}\nSide to move: {turn}\n" +
                   $"Legal moves (SAN): {legalSanList}\n" +
                   $"Pick exactly one move from the list above and reply with its UCI only:";
        }

        private static string BuildLegalMoveList(ChessBoard board)
        {
            var moves = board.Moves();
            if (moves == null || moves.Length == 0) return "(none)";
            var sb = new StringBuilder(moves.Length * 6);
            for (int i = 0; i < moves.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                var label = !string.IsNullOrEmpty(moves[i].San) ? moves[i].San : moves[i].ToString();
                sb.Append(label);
            }
            return sb.ToString();
        }

        // Last-resort fallback so the player's turn doesn't stall forever.
        private static Move PickRandomLegalMove(ChessBoard board)
        {
            var moves = board.Moves();
            if (moves == null || moves.Length == 0) return null;
            return moves[UnityEngine.Random.Range(0, moves.Length)];
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

        // Pull the FIRST "thinking":"..." block from the response. The gateway
        // returns the reasoning text in a {"type":"thinking","thinking":"..."}
        // content block. We use this as a fallback when the model spends all
        // its output budget on reasoning and never emits a text block.
        private static string ExtractThinkingBlock(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;
            try
            {
                var matches = Regex.Matches(raw, "\"thinking\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"");
                for (int i = 0; i < matches.Count; i++)
                {
                    var decoded = DecodeJsonString(matches[i].Groups[1].Value);
                    if (!string.IsNullOrWhiteSpace(decoded)) return decoded;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ClaudeApiProvider] thinking-extract failed: {e.Message}");
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

        // The model sometimes returns Standard Algebraic Notation (e.g. "Nf3",
        // "Bxc6", "exf3", "O-O", "O-O-O") instead of UCI. Look for a legal move
        // whose San matches the first SAN-shaped token in the text. Returns the
        // matching Move, or null if nothing matches.
        private static Move FindMoveBySan(ChessBoard board, string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            try
            {
                var moves = board.Moves();
                if (moves == null || moves.Length == 0) return null;

                // Match a SAN-shaped token: optional piece letter, optional
                // disambiguation file/rank, optional 'x', target square,
                // optional promotion (e.g. e8=Q), optional check/mate.
                var m = Regex.Match(text, "\\b([KQRBN]?[a-h]?[1-8]?x?[a-h][1-8](?:=[QRBN])?[+#]?)\\b");
                if (!m.Success) return null;
                var san = m.Value.TrimEnd('+', '#');

                foreach (var mv in moves)
                {
                    if (string.IsNullOrEmpty(mv.San)) continue;
                    if (string.Equals(mv.San, san, StringComparison.OrdinalIgnoreCase)) return mv;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ClaudeApiProvider] FindMoveBySan threw: {e.Message}");
            }
            return null;
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
            try
            {
                var legal = board.Moves(from, generateSan: false);
                foreach (var m in legal)
                {
                    if (m.NewPosition.X == to.X && m.NewPosition.Y == to.Y) return m;
                }
            }
            catch (Exception e)
            {
                // Gera throws ChessPieceNotFoundException when there's no piece on
                // `from` (the model hallucinated an empty square). Treat as
                // "not a legal move" so the caller can retry / fall back.
                Debug.LogWarning($"[ClaudeApiProvider] FindLegalMove({uci}) threw: {e.GetType().Name}: {e.Message}");
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
