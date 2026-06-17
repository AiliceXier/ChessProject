using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TMPro;
using Chess;
using Chess.AI;
using Chess.Animation;
using Chess.Audio;
using Chess.Leaderboard;
using Chess.UI;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.CloudCode.Subscriptions;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using WebSocketSharp;

public class Player : MonoBehaviour
{
    private GameObject _selectedPiece;
    public Camera playerCamera;
    public GameObject cameraPivot;
    public TextMeshProUGUI lobbyInputCodeText;
    public TextMeshProUGUI lobbyCodeText;
    public TextMeshProUGUI opponentNameText;
    
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI scoreText;

    public GameObject resignButton;
    public GameObject undoButton;
    public GameObject uiPanel;
    public TextMeshProUGUI resultText;
    public GameObject board;
    
    private TMP_InputField _lobbyInputField;
    
    public MoveHistoryUI moveHistoryUI;
    public CommandInputUI commandInputUI;
    public DifficultySelector difficultySelector;
    public EvaluationBar evaluationBar;
    public HintSystem hintSystem;
    public ChatUI chatUI;
    public MainMenuUI mainMenuUI;
    private MoveAnimator _moveAnimator;
    private GameEndAnimator _gameEndAnimator;
    
    private readonly Dictionary<string, UnityEngine.Object> _prefabs = new();
    private const string StartingBoard = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    private bool _gameStarted;
    private bool _isWhite;
    private string _currentSession;
    private Task _initializationTask;
    private bool _isInitialized;

    private readonly Color32 _selectedColor = new (0, 150, 0, 180);
    private readonly Color32 _lightColor = new(223, 210, 194, 255);
    private readonly Color32 _darkColor = new (84, 84, 84, 255);
    private readonly Color32 _checkColor = new (220, 50, 50, 255);

    private readonly List<GameObject> _moveHighlights = new();
    private GameObject _checkedKing;

    private enum GameMode { Online, Local, Robot }
    private GameMode _gameMode = GameMode.Online;
    private ChessBoard _localBoard;
    private bool _localWhiteTurn = true;
    private ChessAI _chessAI;
    // For online play, we don't drive moves through _localBoard, so its
    // ExecutedMoves is always empty. We remember the last cloud FEN to
    // compute the SAN of the just-played move by enumeration + comparison.
    private string _lastOnlineFen;
    private string _lastOnlineWonSide;
    private bool _didResignSelf;
    private int _aiDepth = 3; // 1/3 → local MinMax, 4 → Claude no-thinking, 5 → Claude thinking
    private bool _aiThinking;
    private string _pendingFen;

    private int _moveCount;
    private string _leaderboardPlayerName = "Player";
    private readonly Dictionary<string, int> _currentScores = new();
    private static readonly Dictionary<GameMode, string> GameModeToLeaderboardMode = new Dictionary<GameMode, string>
    {
        { GameMode.Robot, "robot" },
        { GameMode.Local, "local" },
        { GameMode.Online, "online" }
    };

    private async void Start()
    {
        SyncBoard(StartingBoard);
        if (moveHistoryUI == null)
            moveHistoryUI = gameObject.AddComponent<MoveHistoryUI>();
        moveHistoryUI.player = this;
        if (commandInputUI == null)
            commandInputUI = gameObject.AddComponent<CommandInputUI>();
        commandInputUI.player = this;
        if (difficultySelector == null)
            difficultySelector = gameObject.AddComponent<DifficultySelector>();
        difficultySelector.player = this;
        if (evaluationBar == null)
            evaluationBar = gameObject.AddComponent<EvaluationBar>();
        evaluationBar.player = this;
        if (hintSystem == null)
            hintSystem = gameObject.AddComponent<HintSystem>();
        hintSystem.player = this;
        if (chatUI == null)
            chatUI = gameObject.AddComponent<ChatUI>();
        chatUI.player = this;

        if (undoButton == null)
        {
            var foundUndo = GameObject.Find("UndoBtn");
            if (foundUndo != null) undoButton = foundUndo;
        }
        if (undoButton != null)
        {
            var undoBtn = undoButton.GetComponent<Button>();
            if (undoBtn != null)
            {
                undoBtn.onClick.RemoveAllListeners();
                undoBtn.onClick.AddListener(OnUndoClicked);
            }
            undoButton.SetActive(false);
        }

        if (mainMenuUI == null)
        {
            mainMenuUI = gameObject.AddComponent<MainMenuUI>();
            mainMenuUI.player = this;
        }
        _moveAnimator = GetComponent<MoveAnimator>();
        if (_moveAnimator == null)
            _moveAnimator = gameObject.AddComponent<MoveAnimator>();
        _moveAnimator.board = board;
        _gameEndAnimator = GetComponent<GameEndAnimator>();
        if (_gameEndAnimator == null)
            _gameEndAnimator = gameObject.AddComponent<GameEndAnimator>();
        _gameEndAnimator.board = board;
        _gameEndAnimator.cameraPivot = cameraPivot;
        if (mainMenuUI != null) mainMenuUI.Initialize(uiPanel);
        if (uiPanel != null)
        {
            var lobbyCodeInputTr = uiPanel.transform.Find("Lobby Code Input");
            if (lobbyCodeInputTr != null)
                _lobbyInputField = lobbyCodeInputTr.GetComponent<TMP_InputField>();
        }
        HideInGameUI();
        if (mainMenuUI != null) mainMenuUI.Show();
        _initializationTask = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            await SubscribeToPlayerMessages();
            _isInitialized = true;

            var playerId = AuthenticationService.Instance.PlayerId;
            var shortId = playerId.Length > 8 ? playerId.Substring(0, 8) : playerId;
            _leaderboardPlayerName = $"Player_{shortId}";
            FetchPlayerScores();

            Debug.Log($"Unity Services initialized. Player: {_leaderboardPlayerName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
            if (mainMenuUI != null) mainMenuUI.ShowWithResult("Failed to initialize. Please restart the game.");
        }
    }

    public async void CreateGame()
    {
        await WaitForInitialization();
        if (!_isInitialized) return;

        try
        {
            var hostGameResponse = await CloudCodeService.Instance.CallModuleEndpointAsync<HostGameResponse>("ChessCloudCode", "HostGame");
            lobbyCodeText.text = hostGameResponse.LobbyCode;
            if (mainMenuUI != null) mainMenuUI.ShowWaitingForOpponent(hostGameResponse.LobbyCode);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (mainMenuUI != null) mainMenuUI.ShowWithResult("Create game failed. Please try again.");
        }
    }

    private void SetPov()
    {
        var angle = _isWhite ? 0 : 180;
        cameraPivot.transform.eulerAngles = new Vector3(0, angle, 0);
    }

    public async void Resign()
    {
        if (_gameMode == GameMode.Robot)
        {
            _localBoard.Resign(PieceColor.White);
            SyncBoard(_localBoard.ToFen());
            ShowGameOver(GetRobotEndGameText(_localBoard.EndGame));
            SubmitGameScore(_localBoard.EndGame);
            return;
        }

        if (_gameMode == GameMode.Local)
        {
            var resigningColor = _localWhiteTurn ? PieceColor.White : PieceColor.Black;
            _localBoard.Resign(resigningColor);
            SyncBoard(_localBoard.ToFen());
            ShowGameOver(GetEndGameText(_localBoard.EndGame));
            SubmitGameScore(_localBoard.EndGame);
            return;
        }

        if (_initializationTask != null && !_initializationTask.IsCompleted)
        {
            await _initializationTask;
        }
        if (!_isInitialized) return;

        try
        {
            _didResignSelf = true;
            var boardUpdate = await CloudCodeService.Instance.CallModuleEndpointAsync<BoardUpdateResponse>("ChessCloudCode", "Resign",
                new Dictionary<string, object> { { "session", _currentSession } });
            OnBoardUpdate(boardUpdate);
        }
        catch (LobbyServiceException exception)
        {
            Debug.LogException(exception);
        }
    }
    
    public void SetLobbyCode(string code)
    {
        if (_lobbyInputField != null) _lobbyInputField.text = code;
        else if (lobbyInputCodeText != null) lobbyInputCodeText.text = code;
    }

    public async void JoinLobbyByCode()
    {
        await WaitForInitialization();
        if (!_isInitialized) return;

        try
        {
            var rawCode = _lobbyInputField != null ? _lobbyInputField.text : (lobbyInputCodeText != null ? lobbyInputCodeText.text : "");
            var sanitizedLobbyCode = Regex.Replace(rawCode, @"\s", "").Replace("\u200B", "");

            if (string.IsNullOrWhiteSpace(sanitizedLobbyCode))
            {
                if (mainMenuUI != null) mainMenuUI.ShowWithResult("Please enter a valid lobby code.");
                return;
            }
            
            var joinGameResponse = await CloudCodeService.Instance.CallModuleEndpointAsync<JoinGameResponse>("ChessCloudCode", "JoinGame",
                new Dictionary<string, object> { { "lobbyCode", sanitizedLobbyCode } });
            lobbyCodeText.text = sanitizedLobbyCode;
            
            OnGameStart(joinGameResponse);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (mainMenuUI != null) mainMenuUI.ShowWithResult("Join game failed. Check lobby code or wait for opponent.");
        }
    }

    public void StartLocalGame()
    {
        _gameMode = GameMode.Local;
        _currentSession = null;
        _localBoard = new ChessBoard();
        _localWhiteTurn = true;
        _gameStarted = true;
        _moveCount = 0;
        if (_gameEndAnimator != null) _gameEndAnimator.ResetAllPieces();
        SyncBoard(_localBoard.ToFen());
        if (mainMenuUI != null) mainMenuUI.Hide();
        resignButton.SetActive(true);
        if (undoButton != null) undoButton.SetActive(true);
        SetPovLocal();
        playerNameText.text = "White's Turn";
        FetchPlayerScores();
        if (moveHistoryUI != null) moveHistoryUI.SetBoard(_localBoard);
        ShowInGameUI();
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBGM();
    }

    public void StartRobotGame()
    {
        if (difficultySelector != null)
        {
            difficultySelector.Show();
            return;
        }
        StartRobotGameWithDifficulty(3);
    }

    public void StartRobotGameWithDifficulty(int depth)
    {
        _gameMode = GameMode.Robot;
        _currentSession = null;
        _localBoard = new ChessBoard();
        _gameStarted = true;
        _aiThinking = false;
        _moveCount = 0;
        _aiDepth = depth;
        // Depth 1 / 3 use the on-device MinMax engine.
        // Depth 4 / 5 are routed to the cloud Claude provider (see DoRobotMoveAsync).
        _chessAI = depth <= 3 ? new ChessAI(maxDepth: depth) : null;
        if (_gameEndAnimator != null) _gameEndAnimator.ResetAllPieces();
        SyncBoard(_localBoard.ToFen());
        if (mainMenuUI != null) mainMenuUI.Hide();
        if (difficultySelector != null) difficultySelector.Hide();
        resignButton.SetActive(true);
        if (undoButton != null) undoButton.SetActive(true);
        cameraPivot.transform.eulerAngles = Vector3.zero;
        playerNameText.text = "Your Turn (White)";
        FetchPlayerScores();
        if (moveHistoryUI != null) moveHistoryUI.SetBoard(_localBoard);
        ShowInGameUI();
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBGM();
    }

    private bool CurrentPlayerIsWhite() =>
        _gameMode switch
        {
            GameMode.Local => _localWhiteTurn,
            GameMode.Robot => _localWhiteTurn,
            _ => _isWhite
        };

    private void SetPovLocal() =>
        cameraPivot.transform.eulerAngles = new Vector3(0, _localWhiteTurn ? 0 : 180, 0);

    private bool MakeLocalMove(string fromFen, string toFen, bool updateUI = true)
    {
        var move = new Move(fromFen, toFen);
        if (!_localBoard.IsValidMove(move))
        {
            var isWhiteTurn = _localWhiteTurn;
            bool inCheck = isWhiteTurn ? _localBoard.WhiteKingChecked : _localBoard.BlackKingChecked;
            if (inCheck)
            {
                Debug.Log($"Invalid move (king in check): {fromFen} -> {toFen}");
                playerNameText.text = isWhiteTurn ? "White King in Check!" : "Black King in Check!";
                ShowCheckIndicator();
            }
            else
            {
                Debug.Log($"Invalid move: {fromFen} -> {toFen}");
            }
            SelectPiece(null);
            return false;
        }

        _localBoard.Move(move);
        SelectPiece(null);
        SyncBoard(_localBoard.ToFen());
        _moveCount++;
        if (moveHistoryUI != null) moveHistoryUI.RefreshDisplay();
        if (evaluationBar != null) evaluationBar.Show();

        if (_localBoard.IsEndGame)
        {
            ShowGameOver(_gameMode == GameMode.Robot
                ? GetRobotEndGameText(_localBoard.EndGame)
                : GetEndGameText(_localBoard.EndGame));
            SubmitGameScore(_localBoard.EndGame);
            return true;
        }

        if (updateUI)
        {
            _localWhiteTurn = !_localWhiteTurn;
            SetPovLocal();
            var isNowWhiteTurn = _localWhiteTurn;
            bool nowInCheck = isNowWhiteTurn ? _localBoard.WhiteKingChecked : _localBoard.BlackKingChecked;
            playerNameText.text = nowInCheck
                ? (isNowWhiteTurn ? "White's Turn - CHECK!" : "Black's Turn - CHECK!")
                : (isNowWhiteTurn ? "White's Turn" : "Black's Turn");
            Debug.Log($"[Player] MakeLocalMove: _localWhiteTurn={_localWhiteTurn}, move={fromFen}->{toFen}");
        }
        return true;
    }

    private async Task DoRobotMoveAsync()
    {
        _aiThinking = true;
        playerNameText.text = "AI Thinking...";

        try
        {
            Move aiMove = null;
            var boardSnapshot = ChessBoard.LoadFromFen(_localBoard.ToFen());

            if (_aiDepth <= 3)
            {
                // Local engine — keep on a background thread to avoid blocking the main loop.
                await Task.Run(() =>
                {
                    aiMove = _chessAI.GetBestMove(boardSnapshot);
                });
            }
            else
            {
                // Cloud engine — depth 4 = no thinking, depth 5 = extended thinking.
                bool useThinking = _aiDepth >= 5;
                playerNameText.text = useThinking ? "Cloud AI (thinking)..." : "Cloud AI...";
                // When thinking is enabled, max_tokens must be strictly greater than
                // thinking.budget_tokens, otherwise the model spends the entire output
                // budget on internal reasoning and never emits a text block (the move).
                // We don't care about token cost here, so we leave plenty of room.
                var provider = new ClaudeApiProvider(
                    maxTokens: useThinking ? 8192 : 64,
                    thinkingBudget: useThinking ? 6000 : 0,
                    timeoutSeconds: 60);
                aiMove = await provider.GetBestMoveAsync(boardSnapshot, useThinking);
            }

            if (aiMove == null || !_gameStarted || _gameMode != GameMode.Robot)
            {
                Debug.Log("[Player] AI move was null or game not active, skipping");
                return;
            }

            Debug.Log($"[Player] AI move: {aiMove.San ?? aiMove.ToString()}, animating={_moveAnimator?.IsAnimating}");
            _localBoard.Move(aiMove);
            SyncBoard(_localBoard.ToFen());
            _moveCount++;
            if (moveHistoryUI != null) moveHistoryUI.RefreshDisplay();
            if (evaluationBar != null) evaluationBar.Show();

            if (_localBoard.IsEndGame)
            {
                ShowGameOver(GetRobotEndGameText(_localBoard.EndGame));
                SubmitGameScore(_localBoard.EndGame);
            }
            else
            {
                _localWhiteTurn = true;
                cameraPivot.transform.eulerAngles = Vector3.zero;
                ShowCheckIndicator();
                bool whiteInCheck = _localBoard.WhiteKingChecked;
                playerNameText.text = whiteInCheck ? "Your Turn - CHECK!" : "Your Turn (White)";
                Debug.Log("[Player] AI turn complete, _localWhiteTurn=true, _aiThinking will be set to false");
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            _localWhiteTurn = true;
            playerNameText.text = "Your Turn (White)";
        }
        finally
        {
            _aiThinking = false;
        }
    }

    private void SyncBoard(string fen)
    {
        try
        {
            if (_moveAnimator != null && _moveAnimator.IsAnimating)
            {
                Debug.Log($"[Player] SyncBoard: animation in progress, queuing pending FEN");
                _pendingFen = fen;
                return;
            }

            var boardState = FenToDict(fen);

            if (_moveAnimator != null)
            {
                StartCoroutine(_moveAnimator.AnimateSyncBoard(boardState, _prefabs, (go, c) =>
                {
                    go.name = GetPieceTypeName(c) + (char.IsUpper(c) ? "Light" : "Dark") + "(Clone)";
                }, OnBoardAnimationComplete));
            }
            else
            {
                ApplyBoardStateInstant(boardState);
            }
        }
        catch (CloudCodeException exception)
        {
            Debug.LogException(exception);
        }
    }

    private void OnBoardAnimationComplete()
    {
        ShowCheckIndicator();

        if (_pendingFen != null)
        {
            Debug.Log("[Player] OnBoardAnimationComplete: applying pending FEN");
            var fen = _pendingFen;
            _pendingFen = null;
            SyncBoard(fen);
        }
    }

    private void ApplyBoardStateInstant(Dictionary<Tuple<int, int>, char> boardState)
    {
        foreach (Transform child in board.transform)
        {
            Destroy(child.gameObject);
        }
        foreach (var piece in boardState)
        {
            var pieceType = char.ToLower(piece.Value) switch
            {
                'p' => "Pawn",
                'n' => "Knight",
                'b' => "Bishop",
                'r' => "Rook",
                'q' => "Queen",
                'k' => "King",
                _ => ""
            };
            var prefabName = pieceType + (char.IsUpper(piece.Value) ? "Light" : "Dark");
            if (!_prefabs.ContainsKey(prefabName))
            {
                _prefabs[prefabName] = Resources.Load($"{pieceType}/Prefabs/{prefabName}");
            }

            var newObject = Instantiate(_prefabs[prefabName], board.transform);
            newObject.GameObject().transform.position = new Vector3(piece.Key.Item1, 0, piece.Key.Item2);
            newObject.GameObject().transform.rotation = Quaternion.Euler(0, char.IsLower(piece.Value) ? 180 : 0, 0);
        }
    }

    private static string GetPieceTypeName(char c)
    {
        return char.ToLower(c) switch
        {
            'p' => "Pawn",
            'n' => "Knight",
            'b' => "Bishop",
            'r' => "Rook",
            'q' => "Queen",
            'k' => "King",
            _ => ""
        };
    }

    private async void MakeMove(GameObject piece, Vector3 toPos)
    {
        if (piece == null) return;
        ClearMoveHighlights();

        var fromFen = PosToFen(piece.transform.position);
        var toFen = PosToFen(toPos);

        if (_gameMode == GameMode.Local)
        {
            MakeLocalMove(fromFen, toFen);
            return;
        }

        if (_gameMode == GameMode.Robot)
        {
            if (MakeLocalMove(fromFen, toFen, updateUI: true) && !_localBoard.IsEndGame)
            {
                _ = DoRobotMoveAsync();
            }
            return;
        }

        if (_initializationTask != null && !_initializationTask.IsCompleted)
        {
            await _initializationTask;
        }
        if (!_isInitialized) return;

        var result = await CloudCodeService.Instance.CallModuleEndpointAsync<BoardUpdateResponse>(
            "ChessCloudCode",
            "MakeMove",
            new Dictionary<string, object>
            {
                { "session", _currentSession },
                { "fromPosition", fromFen },
                { "toPosition", toFen }
            });

        SelectPiece(null);
        OnBoardUpdate(result);
    }

    private async void OnBoardUpdate(BoardUpdateResponse boardUpdateResponse)
    {
        SyncBoard(boardUpdateResponse.Board);

        // Keep the move history panel in sync with the cloud-authoritative state
        // for online games. _localBoard's ExecutedMoves is always empty (FEN has
        // no move list), so we maintain the history via PushMove on the UI and
        // compute the SAN of the just-played move by enumeration + FEN diff.
        try
        {
            _localBoard = ChessBoard.LoadFromFen(boardUpdateResponse.Board);
            if (moveHistoryUI != null)
            {
                moveHistoryUI.SetBoard(_localBoard);
                if (_gameMode == GameMode.Online && _lastOnlineFen != null)
                {
                    var san = FindSanBetween(_lastOnlineFen, boardUpdateResponse.Board);
                    if (!string.IsNullOrEmpty(san))
                    {
                        moveHistoryUI.PushMove(san);
                    }
                    else
                    {
                        Debug.LogWarning($"[Player] OnBoardUpdate: no legal move bridges FENs. " +
                                         $"old={_lastOnlineFen} new={boardUpdateResponse.Board}");
                    }
                }
                else
                {
                    moveHistoryUI.RefreshDisplay();
                }
            }
            _lastOnlineFen = boardUpdateResponse.Board;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Player] OnBoardUpdate: failed to refresh move history: {e.Message}");
        }

        if (boardUpdateResponse.GameOver)
        {
            _lastOnlineWonSide = boardUpdateResponse.WonSide;
            ShowGameOver(boardUpdateResponse.EndgameType);
            SubmitOnlineScore(boardUpdateResponse.EndgameType);
        }
        else
        {
            var fenParts = boardUpdateResponse.Board.Split(' ');
            var isWhiteTurn = fenParts.Length > 1 && fenParts[1] == "w";
            playerNameText.text = isWhiteTurn == _isWhite ? "Your Turn" : "Opponent's Turn";
        }
    }

    // Enumerate all legal moves from oldFen, run each on a fresh board, and
    // return the SAN whose resulting position matches newFen. Used to recover
    // the move played on the server during online play so we can show it in
    // the local move history.
    private static string FindSanBetween(string oldFen, string newFen)
    {
        if (string.IsNullOrEmpty(oldFen) || string.IsNullOrEmpty(newFen)) return null;
        try
        {
            var newPos = newFen.Split(' ')[0];
            var snapshot = ChessBoard.LoadFromFen(oldFen);
            foreach (var m in snapshot.Moves())
            {
                var testBoard = ChessBoard.LoadFromFen(oldFen);
                testBoard.Move(m);
                if (testBoard.ToFen().Split(' ')[0] == newPos)
                {
                    return m.San ?? m.ToString();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Player] FindSanBetween: {e.Message}");
        }
        return null;
    }

    private async void OnGameStart(JoinGameResponse joinGameResponse)
    {
        Debug.Log($"Opponent joined: {joinGameResponse.OpponentId}");
        _currentSession = joinGameResponse.Session;
        _lastOnlineWonSide = null;
        _didResignSelf = false;
        if (_gameEndAnimator != null) _gameEndAnimator.ResetAllPieces();
        SyncBoard(joinGameResponse.Board);
        if (mainMenuUI != null) mainMenuUI.Hide();
        if (resignButton != null) resignButton.SetActive(true);
        _isWhite = joinGameResponse.IsWhite;
        SetPov();
        _gameStarted = true;
        playerNameText.text = _isWhite ? "Your Turn (White)" : "Your Turn (Black)";
        ShowInGameUI();
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBGM();

        // Online mode never constructs _localBoard via StartLocalGame/StartRobotGame,
        // so the move history UI never has a board to render. Mirror the cloud FEN
        // into a local board now so the history panel shows the opening position
        // (and so subsequent OnBoardUpdate calls can refresh it incrementally).
        try
        {
            _localBoard = ChessBoard.LoadFromFen(joinGameResponse.Board);
            _lastOnlineFen = joinGameResponse.Board;
            if (moveHistoryUI != null)
            {
                moveHistoryUI.SetBoard(_localBoard);
                moveHistoryUI.ResetManualMoves();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Player] OnGameStart: failed to seed _localBoard from FEN: {e.Message}");
        }

        if (chatUI != null && !string.IsNullOrEmpty(_currentSession))
        {
            chatUI.ConnectToRoom(_currentSession, _leaderboardPlayerName);
        }
    }

    private async Task WaitForInitialization()
    {
        if (_initializationTask != null && !_initializationTask.IsCompleted)
        {
            await _initializationTask;
        }
    }

    private Task SubscribeToPlayerMessages()
    {
        var callbacks = new SubscriptionEventCallbacks();
        callbacks.MessageReceived += @event =>
        {
            switch (@event.MessageType)
            {
                case "boardUpdated":
                    var message = JsonConvert.DeserializeObject<BoardUpdateResponse>(@event.Message);
                    OnBoardUpdate(message);
                    break;
                case "opponentJoined":
                    if (mainMenuUI == null || !mainMenuUI.IsWaitingForOpponent)
                    {
                        Debug.Log("Ignoring opponentJoined event - not in waiting state");
                        break;
                    }
                    var opponentJoinedMessage = JsonConvert.DeserializeObject<JoinGameResponse>(@event.Message);
                    OnGameStart(opponentJoinedMessage);
                    break;
                default:
                    Debug.Log($"Got unsupported player Message: {JsonConvert.SerializeObject(@event, Formatting.Indented)}");
                    break;
            }
        };
        callbacks.ConnectionStateChanged += @event =>
        {
            if (@event == EventConnectionState.Subscribed && _currentSession != null && _gameStarted)
            {
            }
            Debug.Log($"Got player subscription ConnectionStateChanged: {@event.ToString()}");
        };
        callbacks.Kicked += () =>
        {
            Debug.Log($"Got player subscription Kicked");
        };
        callbacks.Error += @event =>
        {
            Debug.Log($"Got player subscription Error: {JsonConvert.SerializeObject(@event, Formatting.Indented)}");
        };
        return CloudCodeService.Instance.SubscribeToPlayerMessagesAsync(callbacks);
    }

    public void PlayerInteract(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (_aiThinking) return;
        if (_gameEndAnimator != null && _gameEndAnimator.IsAnimating) return;
        if (_moveAnimator != null && _moveAnimator.IsAnimating) return;
        if (_gameMode == GameMode.Online && string.IsNullOrEmpty(_currentSession)) return;
        if ((_gameMode == GameMode.Local || _gameMode == GameMode.Robot) && (!_gameStarted || (_localBoard != null && _localBoard.IsEndGame))) return;

        var mousePosition = Mouse.current.position.ReadValue();
        var rayOrigin = playerCamera.ScreenPointToRay(mousePosition);
        if (Physics.Raycast(rayOrigin, out var hitInfo))
        {
            var hitObj = hitInfo.transform.gameObject;
            var playerIsWhite = CurrentPlayerIsWhite();

            if (hitObj.name == "Board"
                || (_selectedPiece != null && hitObj.name.Contains("Light") != playerIsWhite))
            {
                var boardPos = new Vector3(Mathf.RoundToInt(hitInfo.point.x), 0, Mathf.RoundToInt(hitInfo.point.z));
                MakeMove(_selectedPiece, boardPos);
            }
            else if (hitObj.name.Contains("Light") == playerIsWhite)
            {
                SelectPiece(hitObj);
                Debug.Log($"Piece selected: {_selectedPiece.name}");
            }
            else
            {
                SelectPiece(null);
            }
        }
        else
        {
            SelectPiece(null);
        }
    }

    private void SelectPiece(GameObject piece)
    {
        ClearMoveHighlights();
        if (_selectedPiece != null)
        {
            bool wasCheckedKing = _selectedPiece == _checkedKing;
            ChangeMaterialColor(_selectedPiece,
                wasCheckedKing ? _checkColor :
                (_selectedPiece.name.Contains("Light") ? _lightColor : _darkColor));
        }
        _selectedPiece = piece;
        if (_selectedPiece == null) return;
        bool isCheckedKing = _selectedPiece == _checkedKing;
        ChangeMaterialColor(_selectedPiece, isCheckedKing ? _checkColor : _selectedColor);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPieceSelect();

        if ((_gameMode == GameMode.Local || _gameMode == GameMode.Robot) && _localBoard != null)
            ShowMoveHighlights(_selectedPiece);
    }

    private void ShowMoveHighlights(GameObject piece)
    {
        var pos = piece.transform.position;
        var x = Mathf.RoundToInt(pos.x);
        var y = Mathf.RoundToInt(pos.z);
        if (x < 0 || x > 7 || y < 0 || y > 7) return;

        var position = new Position((short)x, (short)y);
        var moves = _localBoard.Moves(position);
        if (moves == null || moves.Length == 0) return;

        foreach (var move in moves)
        {
            var nx = move.NewPosition.X;
            var ny = move.NewPosition.Y;

            Color color;
            if (move.Parameter is MoveCastle)
                color = new Color(1.0f, 0.85f, 0.0f, 0.6f);
            else if (move.CapturedPiece != null)
                color = new Color(0.85f, 0.1f, 0.1f, 0.65f);
            else
                color = new Color(0.0f, 0.55f, 0.0f, 0.45f);

            var highlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
            highlight.name = "MoveHighlight";
            UnityEngine.Object.Destroy(highlight.GetComponent<Collider>());
            var mr = highlight.GetComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Unlit/Transparent"));
            mr.material.color = color;
            highlight.transform.SetParent(board.transform, false);
            highlight.transform.localRotation = Quaternion.Euler(90, 0, 0);
            highlight.transform.localPosition = new Vector3(nx, 0.02f, ny);
            highlight.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            _moveHighlights.Add(highlight);
        }
    }

    private void ClearMoveHighlights()
    {
        foreach (var h in _moveHighlights)
        {
            if (h != null) DestroyImmediate(h);
        }
        _moveHighlights.Clear();
    }

    private void ShowCheckIndicator()
    {
        ClearCheckIndicator();

        if (_localBoard == null) return;

        bool whiteChecked = _localBoard.WhiteKingChecked;
        bool blackChecked = _localBoard.BlackKingChecked;

        if (!whiteChecked && !blackChecked) return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCheck();

        Position kingPos = whiteChecked ? _localBoard.WhiteKing : _localBoard.BlackKing;
        string kingName = whiteChecked ? "KingLight" : "KingDark";

        foreach (Transform child in board.transform)
        {
            if (child == null || child.gameObject == null) continue;
            if (child.gameObject.name.StartsWith(kingName))
            {
                int cx = Mathf.RoundToInt(child.localPosition.x);
                int cz = Mathf.RoundToInt(child.localPosition.z);
                if (cx == kingPos.X && cz == kingPos.Y)
                {
                    _checkedKing = child.gameObject;
                    ChangeMaterialColor(_checkedKing, _checkColor);
                    break;
                }
            }
        }
    }

    private void ClearCheckIndicator()
    {
        if (_checkedKing != null)
        {
            Color32 restoreColor = _checkedKing.name.Contains("Light") ? _lightColor : _darkColor;
            ChangeMaterialColor(_checkedKing, restoreColor);
            _checkedKing = null;
        }
    }

    private static Dictionary<Tuple<int, int>, char> FenToDict(string fen)
    {
        var fenParts = fen.Split(' ');
        var boardState = fenParts[0];
        var ranks = boardState.Split('/');

        var coordinatesDict = new Dictionary<Tuple<int, int>, char>();
        var x = 0;
        var y = 7;

        foreach (var rank in ranks)
        {
            foreach (var c in rank)
            {
                if (char.IsDigit(c))
                {
                    x += int.Parse(c.ToString());
                }
                else
                {
                    var coordinates = new Tuple<int, int>(x, y);
                    coordinatesDict.Add(coordinates, c);
                    x += 1;
                }
            }
            x = 0;
            y -= 1;
        }

        return coordinatesDict;
    }

    private void ChangeMaterialColor(GameObject obj, Color newColor)
    {
        if (obj == null) return;
        var selectedRenderer = obj.GetComponent<Renderer>();
        if (selectedRenderer != null)
            selectedRenderer.material.color = newColor;
    }
    
    public class HostGameResponse
    {
        public string LobbyCode { get; set; }
    }    
    
    public class BoardUpdateResponse
    {
        public string Board { get; set; }
        public bool GameOver { get; set; }
        public string EndgameType { get; set; }
        public string WonSide { get; set; }
    }

    public class JoinGameResponse
    {        
        public string Session { get; set; }
        public string Board { get; set; }
        public string OpponentId { get; set; }
        public bool IsWhite { get; set; }
    }

    private string GetEndGameText(EndGameInfo endGame)
    {
        if (endGame == null) return "Game Over";
        return endGame.EndgameType switch
        {
            EndgameType.Checkmate => endGame.WonSide == PieceColor.White ? "Checkmate - White Wins!" : "Checkmate - Black Wins!",
            EndgameType.Stalemate => "Stalemate - Draw",
            EndgameType.DrawDeclared => "Draw",
            EndgameType.Resigned => endGame.WonSide == PieceColor.White ? "White Wins by Resignation" : "Black Wins by Resignation",
            EndgameType.Timeout => endGame.WonSide == PieceColor.White ? "White Wins on Time" : "Black Wins on Time",
            EndgameType.InsufficientMaterial => "Draw - Insufficient Material",
            EndgameType.FiftyMoveRule => "Draw - Fifty Move Rule",
            EndgameType.Repetition => "Draw - Repetition",
            _ => endGame.EndgameType.ToString()
        };
    }

    private string GetRobotEndGameText(EndGameInfo endGame)
    {
        if (endGame == null) return "Game Over";
        return endGame.EndgameType switch
        {
            EndgameType.Checkmate => endGame.WonSide == PieceColor.White ? "Checkmate - You Win!" : "Checkmate - AI Wins!",
            EndgameType.Stalemate => "Stalemate - Draw",
            EndgameType.DrawDeclared => "Draw",
            EndgameType.Resigned => endGame.WonSide == PieceColor.White ? "You Win by Resignation" : "AI Wins by Resignation",
            EndgameType.Timeout => endGame.WonSide == PieceColor.White ? "You Win on Time" : "AI Wins on Time",
            EndgameType.InsufficientMaterial => "Draw - Insufficient Material",
            EndgameType.FiftyMoveRule => "Draw - Fifty Move Rule",
            EndgameType.Repetition => "Draw - Repetition",
            _ => endGame.EndgameType.ToString()
        };
    }

    private void ShowGameOver(string resultMessage)
    {
        if (resignButton != null) resignButton.SetActive(false);
        if (undoButton != null) undoButton.SetActive(false);
        playerNameText.text = "Game Over";
        _gameStarted = false;
        HideInGameUI();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
        }

        StartCoroutine(PlayGameEndAnimation(resultMessage));
    }

    private IEnumerator PlayGameEndAnimation(string resultMessage)
    {
        EndGameInfo endGame = _localBoard?.EndGame;

        if (_gameEndAnimator != null)
        {
            // Determine the winning side. For online mode, _localBoard.EndGame
            // may be null because the FEN stored in Cloud Save doesn't encode
            // endgame state for resignation / timeout. Fall back to the WonSide
            // field the server sends inside BoardUpdateResponse, or infer from
            // _didResignSelf if the server hasn't been updated yet.
            PieceColor? wonSide = endGame?.WonSide;
            if (wonSide == null && _gameMode == GameMode.Online)
            {
                if (!string.IsNullOrEmpty(_lastOnlineWonSide))
                {
                    wonSide = _lastOnlineWonSide == "White" ? PieceColor.White : PieceColor.Black;
                }
                else
                {
                    // Server didn't send WonSide (old cloud code). Infer from _didResignSelf.
                    var msg = resultMessage?.ToLower() ?? "";
                    bool isDraw = msg.Contains("stalemate") || msg.Contains("draw") ||
                                  msg.Contains("insufficient") || msg.Contains("fifty") ||
                                  msg.Contains("repetition");
                    if (!isDraw)
                    {
                        var myColor = _isWhite ? PieceColor.White : PieceColor.Black;
                        wonSide = _didResignSelf ? myColor.OppositeColor() : myColor;
                    }
                }
            }

            if (wonSide != null)
            {
                yield return StartCoroutine(_gameEndAnimator.PlayWinAnimation(wonSide, null));
            }
            else
            {
                yield return StartCoroutine(_gameEndAnimator.PlayDrawAnimation(null));
            }
        }

        if (chatUI != null) chatUI.Disconnect();
        if (mainMenuUI != null) mainMenuUI.ShowWithResult(resultMessage);
    }

    private void HideAllUIPanels()
    {
        if (moveHistoryUI != null) moveHistoryUI.Hide();
        if (commandInputUI != null) commandInputUI.Hide();
        if (chatUI != null) chatUI.Hide();
        if (difficultySelector != null) difficultySelector.Hide();
    }

    private void ShowInGameUI()
    {
        if (moveHistoryUI != null) moveHistoryUI.SetToggleButtonVisible(true);
        if (commandInputUI != null) commandInputUI.SetToggleButtonVisible(true);
        if (chatUI != null) chatUI.SetToggleButtonVisible(_gameMode == GameMode.Online);
        if (hintSystem != null) hintSystem.ShowButton();
        if (evaluationBar != null) evaluationBar.Show();
        if (resignButton != null) resignButton.SetActive(true);
        if (undoButton != null && (_gameMode == GameMode.Local || _gameMode == GameMode.Robot)) undoButton.SetActive(true);
        if (scoreText != null) scoreText.gameObject.SetActive(true);
        
        bool isOnline = _gameMode == GameMode.Online;
        if (lobbyCodeText != null) lobbyCodeText.gameObject.SetActive(isOnline);
        if (opponentNameText != null) opponentNameText.gameObject.SetActive(isOnline);
    }

    private void HideInGameUI()
    {
        HideAllUIPanels();
        if (moveHistoryUI != null) moveHistoryUI.SetToggleButtonVisible(false);
        if (commandInputUI != null) commandInputUI.SetToggleButtonVisible(false);
        if (chatUI != null) chatUI.SetToggleButtonVisible(false);
        if (hintSystem != null) hintSystem.HideButton();
        if (evaluationBar != null) evaluationBar.Hide();
        if (resignButton != null) resignButton.SetActive(false);
        if (undoButton != null) undoButton.SetActive(false);
        if (scoreText != null) scoreText.gameObject.SetActive(false);
        
        if (lobbyCodeText != null) lobbyCodeText.gameObject.SetActive(false);
        if (opponentNameText != null) opponentNameText.gameObject.SetActive(false);
    }

    public void ShowLeaderboard()
    {
        var lbPanel = GameObject.Find("LeaderboardPanel");
        if (lbPanel != null)
        {
            bool isActive = lbPanel.activeSelf;
            lbPanel.SetActive(!isActive);
        }
    }

    public void SetLeaderboardPlayerName(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            _leaderboardPlayerName = name.Trim();
            FetchPlayerScores();
        }
    }

    public string GetLeaderboardPlayerName()
    {
        return _leaderboardPlayerName;
    }

    public int GetCurrentScore(string mode)
    {
        return _currentScores.TryGetValue(mode, out var score) ? score : 0;
    }

    public void FetchPlayerScores()
    {
        if (string.IsNullOrEmpty(_leaderboardPlayerName)) return;

        foreach (var mode in new[] { "robot", "local", "online" })
        {
            var capturedMode = mode;
            StartCoroutine(LeaderboardAPI.GetPlayerRank(
                _leaderboardPlayerName,
                capturedMode,
                onSuccess: resp =>
                {
                    if (resp.success && resp.data != null)
                    {
                        _currentScores[capturedMode] = resp.data.score;
                    }
                    else
                    {
                        _currentScores[capturedMode] = 0;
                    }
                    UpdateScoreDisplay();
                },
                onError: _ => { }
            ));
        }
    }

    public void UpdateScoreDisplay()
    {
        if (scoreText == null) return;

        var mode = _gameMode switch
        {
            GameMode.Robot => "robot",
            GameMode.Local => "local",
            GameMode.Online => "online",
            _ => "robot"
        };

        var score = _currentScores.TryGetValue(mode, out var s) ? s : 0;
        scoreText.text = $"score:{score}";
    }

    private int CalculateScore(EndGameInfo endGame)
    {
        if (endGame == null) return 0;

        var isRobot = _gameMode == GameMode.Robot;
        var isLocal = _gameMode == GameMode.Local;

        if (isRobot)
        {
            if (endGame.EndgameType == EndgameType.Checkmate && endGame.WonSide == PieceColor.White)
            {
                var baseScore = 100;
                var moveBonus = Mathf.Max(0, 50 - _moveCount / 2);
                return baseScore + moveBonus;
            }
            if (endGame.EndgameType == EndgameType.Stalemate ||
                endGame.EndgameType == EndgameType.DrawDeclared ||
                endGame.EndgameType == EndgameType.InsufficientMaterial ||
                endGame.EndgameType == EndgameType.FiftyMoveRule ||
                endGame.EndgameType == EndgameType.Repetition)
            {
                return 20;
            }
            return 0;
        }

        if (isLocal)
        {
            if (endGame.EndgameType == EndgameType.Checkmate)
            {
                return 80;
            }
            if (endGame.EndgameType == EndgameType.Resigned)
            {
                return 60;
            }
            return 15;
        }

        return 0;
    }

    private void SubmitGameScore(EndGameInfo endGame)
    {
        var score = CalculateScore(endGame);
        if (score <= 0) return;

        var mode = GameModeToLeaderboardMode.TryGetValue(_gameMode, out var m) ? m : "default";
        StartCoroutine(LeaderboardAPI.SubmitScore(
            _leaderboardPlayerName,
            score,
            mode,
            onSuccess: resp =>
            {
                if (resp.success)
                {
                    Debug.Log($"[Leaderboard] Score submitted: {_leaderboardPlayerName} -> {score} (mode: {mode}, rank: #{resp.data.rank})");
                    FetchPlayerScores();
                }
            },
            onError: err =>
            {
                Debug.LogWarning($"[Leaderboard] Submit failed: {err}");
            }
        ));
    }

    private void SubmitOnlineScore(string endgameType)
    {
        var score = CalculateOnlineScore(endgameType);
        if (score <= 0) return;

        const string mode = "online";
        StartCoroutine(LeaderboardAPI.SubmitScore(
            _leaderboardPlayerName,
            score,
            mode,
            onSuccess: resp =>
            {
                if (resp.success)
                {
                    Debug.Log($"[Leaderboard] Online score submitted: {_leaderboardPlayerName} -> {score} (rank: #{resp.data.rank})");
                    FetchPlayerScores();
                }
            },
            onError: err =>
            {
                Debug.LogWarning($"[Leaderboard] Submit failed: {err}");
            }
        ));
    }

    private int CalculateOnlineScore(string endgameType)
    {
        if (string.IsNullOrEmpty(endgameType)) return 0;

        var et = endgameType.ToLower();
        var iWon = (_isWhite && et.Contains("white wins")) || (!_isWhite && et.Contains("black wins"));

        if (iWon)
        {
            if (et.Contains("checkmate"))
                return 120;
            if (et.Contains("resignation") || et.Contains("time"))
                return 100;
            return 80;
        }

        if (et.Contains("draw") || et.Contains("stalemate") ||
            et.Contains("insufficient") || et.Contains("fifty") ||
            et.Contains("repetition"))
            return 15;

        return 0;
    }

    private string PosToFen(Vector3 pos)
    {
        return (char)(pos.x + 97) + ((char)pos.z + 1).ToString();
    }

    public ChessBoard GetLocalBoard()
    {
        if (_gameMode == GameMode.Local || _gameMode == GameMode.Robot)
            return _localBoard;
        return null;
    }

    public (bool success, string error) MakeCommandMove(string cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd))
            return (false, "Command cannot be empty");

        if (_gameMode != GameMode.Local && _gameMode != GameMode.Robot)
            return (false, "Command mode only supports local and robot games");

        if (!_gameStarted || _localBoard == null || _localBoard.IsEndGame)
            return (false, "No active game in progress");

        if (_aiThinking)
            return (false, "AI is thinking, please wait");

        if (_moveAnimator != null && _moveAnimator.IsAnimating)
            return (false, "Animation in progress, please wait");

        ClearMoveHighlights();
        SelectPiece(null);

        cmd = cmd.Trim();

        if (TryParseCoordinateMove(cmd, out var fromPos, out var toPos))
        {
            var move = new Move(fromPos, toPos);
            if (!_localBoard.IsValidMove(move))
                return (false, $"Invalid move: {cmd}");

            _localBoard.Move(move);
            SyncBoard(_localBoard.ToFen());
            _moveCount++;
            if (moveHistoryUI != null) moveHistoryUI.RefreshDisplay();

            if (_localBoard.IsEndGame)
            {
                ShowGameOver(_gameMode == GameMode.Robot
                    ? GetRobotEndGameText(_localBoard.EndGame)
                    : GetEndGameText(_localBoard.EndGame));
                SubmitGameScore(_localBoard.EndGame);
                return (true, "");
            }

            if (_gameMode == GameMode.Local)
            {
                _localWhiteTurn = !_localWhiteTurn;
                SetPovLocal();
                playerNameText.text = _localWhiteTurn ? "White's Turn" : "Black's Turn";
            }
            else if (_gameMode == GameMode.Robot)
            {
                _localWhiteTurn = false;
                playerNameText.text = "AI Thinking...";
                _ = DoRobotMoveAsync();
            }

            return (true, "");
        }

        if (TryParseSanMove(cmd, out var sanMove))
        {
            if (!_localBoard.IsValidMove(sanMove))
                return (false, $"Invalid move: {cmd}");

            _localBoard.Move(sanMove);
            SyncBoard(_localBoard.ToFen());
            _moveCount++;
            if (moveHistoryUI != null) moveHistoryUI.RefreshDisplay();

            if (_localBoard.IsEndGame)
            {
                ShowGameOver(_gameMode == GameMode.Robot
                    ? GetRobotEndGameText(_localBoard.EndGame)
                    : GetEndGameText(_localBoard.EndGame));
                SubmitGameScore(_localBoard.EndGame);
                return (true, "");
            }

            if (_gameMode == GameMode.Local)
            {
                _localWhiteTurn = !_localWhiteTurn;
                SetPovLocal();
                playerNameText.text = _localWhiteTurn ? "White's Turn" : "Black's Turn";
            }
            else if (_gameMode == GameMode.Robot)
            {
                _localWhiteTurn = false;
                playerNameText.text = "AI Thinking...";
                _ = DoRobotMoveAsync();
            }

            return (true, "");
        }

        return (false, $"Unrecognized move: {cmd}\nSupported formats: e2e4 / Nf3 / O-O");
    }

    public void OnUndoClicked()
    {
        if (_aiThinking) return;
        if (_moveAnimator != null && _moveAnimator.IsAnimating) return;
        var (success, error) = UndoLastLocalMove();
        if (!success)
            Debug.LogWarning($"[Player] Undo failed: {error}");
        ClearMoveHighlights();
    }

    private bool TryParseCoordinateMove(string cmd, out string fromPos, out string toPos)
    {
        fromPos = null;
        toPos = null;

        if (cmd.Length < 4 || cmd.Length > 5) return false;

        var from = cmd.Substring(0, 2).ToLower();
        var to = cmd.Substring(2, 2).ToLower();

        if (from[0] < 'a' || from[0] > 'h' || from[1] < '1' || from[1] > '8') return false;
        if (to[0] < 'a' || to[0] > 'h' || to[1] < '1' || to[1] > '8') return false;

        fromPos = from;
        toPos = to;
        return true;
    }

    private bool TryParseSanMove(string san, out Move move)
    {
        move = null;
        if (_localBoard == null) return false;

        try
        {
            var moves = _localBoard.Moves();
            foreach (var m in moves)
            {
                if (m.San == san)
                {
                    move = m;
                    return true;
                }
            }

            foreach (var m in moves)
            {
                if (string.Equals(m.San, san, System.StringComparison.OrdinalIgnoreCase))
                {
                    move = m;
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public (bool success, string error) UndoLastLocalMove()
    {
        if (_localBoard == null || _localBoard.ExecutedMoves.Count == 0)
            return (false, "No moves to undo");

        if (_gameMode == GameMode.Robot && _localBoard.ExecutedMoves.Count >= 2)
        {
            _localBoard.Cancel();
            _localBoard.Cancel();
            _localWhiteTurn = true;
        }
        else
        {
            _localBoard.Cancel();
            _localWhiteTurn = !_localWhiteTurn;
        }

        SyncBoard(_localBoard.ToFen());
        if (moveHistoryUI != null) moveHistoryUI.RefreshDisplay();

        if (_gameMode == GameMode.Local)
        {
            SetPovLocal();
            playerNameText.text = _localWhiteTurn ? "White's Turn" : "Black's Turn";
        }
        else if (_gameMode == GameMode.Robot)
        {
            cameraPivot.transform.eulerAngles = Vector3.zero;
            playerNameText.text = "Your Turn (White)";
        }

        return (true, "");
    }

    public (bool success, string error) LoadFromFen(string fen)
    {
        if (string.IsNullOrWhiteSpace(fen))
            return (false, "FEN cannot be empty");

        try
        {
            var newBoard = ChessBoard.LoadFromFen(fen);
            if (newBoard == null)
                return (false, "Invalid FEN string");

            _localBoard = newBoard;
            _gameMode = GameMode.Local;
            _currentSession = null;
            _gameStarted = true;
            _moveCount = 0;
            _localWhiteTurn = _localBoard.Turn == PieceColor.White;

            SyncBoard(_localBoard.ToFen());
            if (mainMenuUI != null) mainMenuUI.Hide();
            resignButton.SetActive(true);
            SetPovLocal();
            playerNameText.text = _localWhiteTurn ? "White's Turn" : "Black's Turn";
            if (moveHistoryUI != null) moveHistoryUI.SetBoard(_localBoard);

            return (true, "");
        }
        catch (Exception e)
        {
            return (false, e.Message);
        }
    }

    public (bool success, string error) LoadFromPgn(string pgn)
    {
        if (string.IsNullOrWhiteSpace(pgn))
            return (false, "PGN cannot be empty");

        try
        {
            var newBoard = ChessBoard.LoadFromPgn(pgn);
            if (newBoard == null)
                return (false, "Invalid PGN string");

            _localBoard = newBoard;
            _gameMode = GameMode.Local;
            _currentSession = null;
            _gameStarted = !_localBoard.IsEndGame;
            _moveCount = _localBoard.ExecutedMoves.Count;
            _localWhiteTurn = _localBoard.Turn == PieceColor.White;

            SyncBoard(_localBoard.ToFen());

            if (_localBoard.IsEndGame)
            {
                ShowGameOver(GetEndGameText(_localBoard.EndGame));
            }
            else
            {
                if (mainMenuUI != null) mainMenuUI.Hide();
                resignButton.SetActive(true);
                SetPovLocal();
                playerNameText.text = _localWhiteTurn ? "White's Turn" : "Black's Turn";
            }

            if (moveHistoryUI != null) moveHistoryUI.SetBoard(_localBoard);

            return (true, "");
        }
        catch (Exception e)
        {
            return (false, e.Message);
        }
    }

    public async void SendChatMessage(string message)
    {
        if (_gameMode != GameMode.Online || _currentSession == null) return;

        Debug.LogWarning($"[Player] SendChatMessage called but WebSocket should be used instead. Message: {message}");
    }
}
