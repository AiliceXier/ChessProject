using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TMPro;
using Chess;
using Chess.Animation;
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
using WebSocketSharp;

public class Player : MonoBehaviour
{
    private GameObject _selectedPiece;
    public Camera playerCamera;
    public GameObject cameraPivot;
    public TextMeshProUGUI lobbyInputCodeText;
    public TextMeshProUGUI lobbyCodeText;
    
    public TextMeshProUGUI playerNameText;

    public GameObject resignButton;
    public GameObject uiPanel;
    public TextMeshProUGUI resultText;
    public GameObject board;
    
    public MoveHistoryUI moveHistoryUI;
    public CommandInputUI commandInputUI;
    public DifficultySelector difficultySelector;
    public EvaluationBar evaluationBar;
    public HintSystem hintSystem;
    public ChatUI chatUI;
    public MainMenuUI mainMenuUI;
    private MoveAnimator _moveAnimator;
    
    private readonly Dictionary<string, UnityEngine.Object> _prefabs = new();
    private const string StartingBoard = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    private bool _gameStarted;
    private bool _isWhite;
    private string _currentSession;
    private Task _initializationTask;
    private bool _isInitialized;

    private readonly Color32 _selectedColor = new (84, 84, 255, 255);
    private readonly Color32 _lightColor = new(223, 210, 194, 255);
    private readonly Color32 _darkColor = new (84, 84, 84, 255);

    private enum GameMode { Online, Local, Robot }
    private GameMode _gameMode = GameMode.Online;
    private ChessBoard _localBoard;
    private bool _localWhiteTurn = true;
    private ChessAI _chessAI;
    private bool _aiThinking;

    private int _moveCount;
    private string _leaderboardPlayerName = "Player";
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
        if (mainMenuUI == null)
        {
            mainMenuUI = gameObject.AddComponent<MainMenuUI>();
            mainMenuUI.player = this;
        }
        _moveAnimator = gameObject.AddComponent<MoveAnimator>();
        _moveAnimator.board = board;
        uiPanel.SetActive(false);
        HideInGameUI();
        _initializationTask = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            await SubscribeToPlayerMessages();
            resignButton.SetActive(false);
            _isInitialized = true;
            Debug.Log("Unity Services initialized and player signed in successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
            resultText.text = "Failed to initialize. Please restart the game.";
            uiPanel.SetActive(true);
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
            uiPanel.SetActive(false);
            if (mainMenuUI != null) mainMenuUI.Hide();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            resultText.text = "Create game failed. Please try again.";
            uiPanel.SetActive(true);
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
        if (lobbyInputCodeText != null)
            lobbyInputCodeText.text = code;
    }

    public async void JoinLobbyByCode()
    {
        await WaitForInitialization();
        if (!_isInitialized) return;

        try
        {
            var sanitizedLobbyCode = Regex.Replace(lobbyInputCodeText.text, @"\s", "").Replace("\u200B", "");

            if (string.IsNullOrWhiteSpace(sanitizedLobbyCode))
            {
                resultText.text = "Please enter a valid lobby code.";
            uiPanel.SetActive(true);
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
            resultText.text = "Join game failed. Check lobby code or wait for opponent.";
            uiPanel.SetActive(true);
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
        SyncBoard(_localBoard.ToFen());
        uiPanel.SetActive(false);
        if (mainMenuUI != null) mainMenuUI.Hide();
        resignButton.SetActive(true);
        SetPovLocal();
        playerNameText.text = "White's Turn";
        if (moveHistoryUI != null) moveHistoryUI.SetBoard(_localBoard);
        ShowInGameUI();
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
        _chessAI = new ChessAI(maxDepth: depth);
        SyncBoard(_localBoard.ToFen());
        uiPanel.SetActive(false);
        if (mainMenuUI != null) mainMenuUI.Hide();
        if (difficultySelector != null) difficultySelector.Hide();
        resignButton.SetActive(true);
        cameraPivot.transform.eulerAngles = Vector3.zero;
        playerNameText.text = "Your Turn (White)";
        if (moveHistoryUI != null) moveHistoryUI.SetBoard(_localBoard);
        ShowInGameUI();
    }

    private bool CurrentPlayerIsWhite() =>
        _gameMode switch
        {
            GameMode.Local => _localWhiteTurn,
            GameMode.Robot => true,
            _ => _isWhite
        };

    private void SetPovLocal() =>
        cameraPivot.transform.eulerAngles = new Vector3(0, _localWhiteTurn ? 0 : 180, 0);

    private bool MakeLocalMove(string fromFen, string toFen, bool updateUI = true)
    {
        var move = new Move(fromFen, toFen);
        if (!_localBoard.IsValidMove(move))
        {
            Debug.Log($"Invalid move: {fromFen} -> {toFen}");
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
            playerNameText.text = _localWhiteTurn ? "White's Turn" : "Black's Turn";
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
            await Task.Run(() =>
            {
                aiMove = _chessAI.GetBestMove(boardSnapshot);
            });

            if (aiMove == null || !_gameStarted || _gameMode != GameMode.Robot)
            {
                return;
            }

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
                playerNameText.text = "Your Turn (White)";
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            playerNameText.text = "Your Turn (White)";
        }
        finally
        {
            _aiThinking = false;
        }
    }

    private void SyncBoard(string fen)
    {
        var boardState = FenToDict(fen);
        try
        {
            if (_moveAnimator != null && _moveAnimator.IsAnimating)
                return;

            if (_moveAnimator != null)
            {
                StartCoroutine(_moveAnimator.AnimateSyncBoard(boardState, _prefabs, (go, c) =>
                {
                    go.name = GetPieceTypeName(c) + (char.IsUpper(c) ? "Light" : "Dark") + "(Clone)";
                }));
            }
            else
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
        }
        catch (CloudCodeException exception)
        {
            Debug.LogException(exception);
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

        var fromFen = PosToFen(piece.transform.position);
        var toFen = PosToFen(toPos);

        if (_gameMode == GameMode.Local)
        {
            MakeLocalMove(fromFen, toFen);
            return;
        }

        if (_gameMode == GameMode.Robot)
        {
            if (MakeLocalMove(fromFen, toFen, updateUI: false) && !_localBoard.IsEndGame)
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
        if (boardUpdateResponse.GameOver)
        {
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

    private async void OnGameStart(JoinGameResponse joinGameResponse)
    {
        Debug.Log($"Opponent joined: {joinGameResponse.OpponentId}");
        _currentSession = joinGameResponse.Session;
        SyncBoard(joinGameResponse.Board);
        uiPanel.SetActive(false);
        if (mainMenuUI != null) mainMenuUI.Hide();
        resignButton.SetActive(true);
        _isWhite = joinGameResponse.IsWhite;
        SetPov();
        _gameStarted = true;
        playerNameText.text = _isWhite ? "Your Turn (White)" : "Your Turn (Black)";
        ShowInGameUI();
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
        if (_selectedPiece != null)
        {
            ChangeMaterialColor(_selectedPiece,
                _selectedPiece.name.Contains("Light") ? _lightColor : _darkColor);
        }
        _selectedPiece = piece;
        if (_selectedPiece == null) return;
        ChangeMaterialColor(_selectedPiece, _selectedColor);
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
        var selectedRenderer = obj.GetComponent<Renderer>();
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
        uiPanel.SetActive(true);
        resignButton.SetActive(false);
        resultText.text = resultMessage;
        playerNameText.text = "Game Over";
        _gameStarted = false;
        HideInGameUI();
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
    }

    private void HideInGameUI()
    {
        HideAllUIPanels();
        if (moveHistoryUI != null) moveHistoryUI.SetToggleButtonVisible(false);
        if (commandInputUI != null) commandInputUI.SetToggleButtonVisible(false);
        if (chatUI != null) chatUI.SetToggleButtonVisible(false);
        if (hintSystem != null) hintSystem.HideButton();
        if (evaluationBar != null) evaluationBar.Hide();
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
            _leaderboardPlayerName = name.Trim();
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
                    Debug.Log($"[Leaderboard] Score submitted: {_leaderboardPlayerName} -> {score} (mode: {mode}, rank: #{resp.data.rank})");
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
                    Debug.Log($"[Leaderboard] Online score submitted: {_leaderboardPlayerName} -> {score} (rank: #{resp.data.rank})");
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
                _ = DoRobotMoveAsync();
            }

            return (true, "");
        }

        return (false, $"Unrecognized move: {cmd}\nSupported formats: e2e4 / Nf3 / O-O");
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
            uiPanel.SetActive(false);
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
                uiPanel.SetActive(false);
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

        try
        {
            await CloudCodeService.Instance.CallModuleEndpointAsync(
                "chess",
                "SendMessage",
                new Dictionary<string, object>
                {
                    { "lobbyId", _currentSession },
                    { "message", message }
                });
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to send chat message: {e.Message}");
        }
    }
}
