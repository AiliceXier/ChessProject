# 国际象棋本地游戏与人机对战流程详解

本文档详细讲解项目中 Local Game（本地双人对战）和 Robot Game（人机对战）两种模式的完整下棋流程，帮助你深入理解游戏逻辑的实现。

## 目录

1. [整体架构概述](#1-整体架构概述)
2. [游戏模式枚举与状态管理](#2-游戏模式枚举与状态管理)
3. [本地游戏模式 (Local Game)](#3-本地游戏模式-local-game)
4. [人机对战模式 (Robot Game)](#4-人机对战模式-robot-game)
5. [AI 实现详解](#5-ai-实现详解)
6. [玩家输入处理流程](#6-玩家输入处理流程)
7. [走法执行流程](#7-走法执行流程)
8. [棋盘同步与渲染](#8-棋盘同步与渲染)
9. [游戏结束处理](#9-游戏结束处理)

---

## 1. 整体架构概述

### 1.1 核心文件位置

| 文件 | 路径 | 职责 |
|------|------|------|
| Player.cs | `Chess/Assets/Player.cs` | 游戏主控制器，处理所有游戏模式的逻辑 |
| ChessAI.cs | `Chess/Assets/Chess/AI/ChessAI.cs` | AI 实现，使用 Minimax 算法 |
| ChessBoard.cs | `Chess/Assets/Chess/ChessBoard/ChessBoard.cs` | 棋盘状态管理和走法验证 |
| Piece.cs | `Chess/Assets/Piece.cs` | Unity 棋子组件（简单的位置记录） |

### 1.2 核心类关系图

```
Player (MonoBehaviour) - 游戏主控制器
├── GameMode _gameMode - 当前游戏模式
├── ChessBoard _localBoard - 本地棋盘状态
├── ChessAI _chessAI - AI 实例
├── bool _localWhiteTurn - 本地游戏当前回合
├── bool _aiThinking - AI 是否正在思考
├── GameObject _selectedPiece - 当前选中的棋子
└── 各种 UI 组件引用

ChessAI
├── int _maxDepth - 搜索深度（默认3层）
├── GetBestMove() - 获取最佳走法
├── Minimax() - 极小极大算法
└── Evaluate() - 局面评估函数

ChessBoard
├── Piece?[,] pieces - 8x8 棋子数组
├── List<Move> executedMoves - 已执行走法
├── PieceColor Turn - 当前轮到哪方
├── Move() - 执行走法
├── IsValidMove() - 验证走法
└── Moves() - 生成所有合法走法
```

---

## 2. 游戏模式枚举与状态管理

### 2.1 游戏模式枚举

```csharp
private enum GameMode { Online, Local, Robot }
private GameMode _gameMode = GameMode.Online;  // 默认为在线模式
```

### 2.2 关键状态变量

```csharp
// 本地棋盘实例（Local 和 Robot 模式使用）
private ChessBoard _localBoard;

// 本地游戏当前回合（true=白方，false=黑方）
private bool _localWhiteTurn = true;

// AI 实例（仅 Robot 模式使用）
private ChessAI _chessAI;

// AI 是否正在思考（防止玩家在 AI 思考时操作）
private bool _aiThinking;

// 游戏是否开始
private bool _gameStarted;
```

### 2.3 判断当前玩家是否为白方

```csharp
private bool CurrentPlayerIsWhite() =>
    _gameMode switch
    {
        GameMode.Local => _localWhiteTurn,    // 本地模式：根据回合判断
        GameMode.Robot => true,               // 人机模式：玩家总是白方
        _ => _isWhite                         // 在线模式：根据服务器分配
    };
```

---

## 3. 本地游戏模式 (Local Game)

### 3.1 启动本地游戏

```csharp
public void StartLocalGame()
{
    // 1. 设置游戏模式为本地
    _gameMode = GameMode.Local;
    
    // 2. 清空在线会话
    _currentSession = null;
    
    // 3. 创建新的棋盘实例
    _localBoard = new ChessBoard();
    
    // 4. 初始化回合（白方先走）
    _localWhiteTurn = true;
    
    // 5. 标记游戏开始
    _gameStarted = true;
    
    // 6. 同步棋盘显示
    SyncBoard(_localBoard.ToFen());
    
    // 7. 隐藏 UI 面板
    uiPanel.SetActive(false);
    
    // 8. 显示认输按钮
    resignButton.SetActive(true);
    
    // 9. 设置视角（白方视角）
    SetPovLocal();
    
    // 10. 更新玩家名称显示
    playerNameText.text = "White's Turn";
}
```

### 3.2 设置本地视角

```csharp
private void SetPovLocal() =>
    // 根据当前回合旋转摄像机
    // 白方回合：0度（正对白方）
    // 黑方回合：180度（正对黑方）
    cameraPivot.transform.eulerAngles = new Vector3(0, _localWhiteTurn ? 0 : 180, 0);
```

### 3.3 本地走法执行流程

```csharp
private bool MakeLocalMove(string fromFen, string toFen, bool updateUI = true)
{
    // 1. 创建走法对象
    var move = new Move(fromFen, toFen);
    
    // 2. 验证走法合法性
    if (!_localBoard.IsValidMove(move))
    {
        Debug.Log($"Invalid move: {fromFen} -> {toFen}");
        SelectPiece(null);  // 取消选中
        return false;
    }

    // 3. 执行走法
    _localBoard.Move(move);
    
    // 4. 取消选中棋子
    SelectPiece(null);
    
    // 5. 同步棋盘显示
    SyncBoard(_localBoard.ToFen());

    // 6. 检查游戏是否结束
    if (_localBoard.IsEndGame)
    {
        uiPanel.SetActive(true);
        resignButton.SetActive(false);
        resultText.text = _localBoard.EndGame?.EndgameType.ToString();
        playerNameText.text = "Game Over";
        _gameStarted = false;
        return true;
    }

    // 7. 更新 UI（如果需要）
    if (updateUI)
    {
        // 切换回合
        _localWhiteTurn = !_localWhiteTurn;
        // 旋转视角
        SetPovLocal();
        // 更新显示文本
        playerNameText.text = _localWhiteTurn ? "White's Turn" : "Black's Turn";
    }
    return true;
}
```

### 3.4 本地游戏认输流程

```csharp
public void Resign()
{
    // ... 其他模式处理 ...

    if (_gameMode == GameMode.Local)
    {
        // 1. 确定认输方
        var resigningColor = _localWhiteTurn ? PieceColor.White : PieceColor.Black;
        
        // 2. 执行认输
        _localBoard.Resign(resigningColor);
        
        // 3. 同步棋盘
        SyncBoard(_localBoard.ToFen());
        
        // 4. 显示结果面板
        uiPanel.SetActive(true);
        resignButton.SetActive(false);
        
        // 5. 显示认输信息
        resultText.text = $"{(_localWhiteTurn ? "White" : "Black")} resigns. " +
                         $"{(_localWhiteTurn ? "Black" : "White")} wins!";
        playerNameText.text = "Game Over";
        
        // 6. 标记游戏结束
        _gameStarted = false;
        return;
    }
}
```

---

## 4. 人机对战模式 (Robot Game)

### 4.1 启动人机对战

```csharp
public void StartRobotGame()
{
    // 1. 设置游戏模式为人机
    _gameMode = GameMode.Robot;
    
    // 2. 清空在线会话
    _currentSession = null;
    
    // 3. 创建新的棋盘实例
    _localBoard = new ChessBoard();
    
    // 4. 标记游戏开始
    _gameStarted = true;
    
    // 5. 初始化 AI 思考状态
    _aiThinking = false;
    
    // 6. 创建 AI 实例（搜索深度为3）
    _chessAI = new ChessAI(maxDepth: 3);
    
    // 7. 同步棋盘显示
    SyncBoard(_localBoard.ToFen());
    
    // 8. 隐藏 UI 面板
    uiPanel.SetActive(false);
    
    // 9. 显示认输按钮
    resignButton.SetActive(true);
    
    // 10. 设置视角（固定为白方视角）
    cameraPivot.transform.eulerAngles = Vector3.zero;
    
    // 11. 更新玩家名称显示
    playerNameText.text = "Your Turn (White)";
}
```

### 4.2 人机模式走法流程

```csharp
private async void MakeMove(GameObject piece, Vector3 toPos)
{
    if (piece == null) return;

    var fromFen = PosToFen(piece.transform.position);
    var toFen = PosToFen(toPos); 

    // 本地模式：直接执行走法
    if (_gameMode == GameMode.Local)
    {
        MakeLocalMove(fromFen, toFen);
        return;
    }

    // 人机模式：玩家走完后 AI 走
    if (_gameMode == GameMode.Robot)
    {
        // 1. 执行玩家走法（不更新回合 UI）
        if (MakeLocalMove(fromFen, toFen, updateUI: false) && !_localBoard.IsEndGame)
        {
            // 2. 触发 AI 走法（异步）
            _ = DoRobotMoveAsync();
        }
        return;
    }

    // 在线模式：调用云服务...
}
```

### 4.3 AI 走法执行流程

```csharp
private async Task DoRobotMoveAsync()
{
    // 1. 标记 AI 正在思考
    _aiThinking = true;
    playerNameText.text = "AI Thinking...";

    Move aiMove = null;
    var boardSnapshot = _localBoard;
    
    // 2. 在后台线程计算最佳走法（避免阻塞主线程）
    await Task.Run(() =>
    {
        aiMove = _chessAI.GetBestMove(boardSnapshot);
    });

    // 3. 检查游戏状态是否已改变（玩家可能已退出）
    if (aiMove == null || !_gameStarted || _gameMode != GameMode.Robot)
    {
        _aiThinking = false;
        return;
    }

    // 4. 执行 AI 走法
    _localBoard.Move(aiMove);
    SyncBoard(_localBoard.ToFen());

    // 5. 检查游戏是否结束
    if (_localBoard.IsEndGame)
    {
        uiPanel.SetActive(true);
        resignButton.SetActive(false);
        resultText.text = _localBoard.EndGame?.EndgameType.ToString();
        playerNameText.text = "Game Over";
        _gameStarted = false;
    }
    else
    {
        // 6. 切换回玩家回合
        _localWhiteTurn = true;
        cameraPivot.transform.eulerAngles = Vector3.zero;
        playerNameText.text = "Your Turn (White)";
    }

    // 7. 标记 AI 思考结束
    _aiThinking = false;
}
```

### 4.4 人机模式认输流程

```csharp
public void Resign()
{
    if (_gameMode == GameMode.Robot)
    {
        // 1. 玩家（白方）认输
        _localBoard.Resign(PieceColor.White);
        
        // 2. 同步棋盘
        SyncBoard(_localBoard.ToFen());
        
        // 3. 显示结果
        uiPanel.SetActive(true);
        resignButton.SetActive(false);
        resultText.text = "You resign. AI wins!";
        playerNameText.text = "Game Over";
        
        // 4. 标记游戏结束
        _gameStarted = false;
        return;
    }
    // ...
}
```

---

## 5. AI 实现详解

### 5.1 AI 类结构

```csharp
public class ChessAI
{
    private readonly int _maxDepth;  // 搜索深度

    public ChessAI(int maxDepth = 3)
    {
        _maxDepth = maxDepth;
    }
}
```

### 5.2 获取最佳走法

```csharp
public Move GetBestMove(ChessBoard board)
{
    // 1. 获取所有合法走法
    var moves = board.Moves(generateSan: false);
    if (moves.Length == 0)
        return null;

    // 2. 判断当前是最大化方（白方）还是最小化方（黑方）
    var isMaximizing = board.Turn == PieceColor.White;
    
    Move bestMove = null;
    int bestScore = isMaximizing ? int.MinValue : int.MaxValue;

    // 3. 遍历所有走法，使用 Minimax 评估每个走法
    foreach (var move in moves)
    {
        // 克隆棋盘（避免修改原棋盘）
        var clone = ChessBoard.LoadFromFen(board.ToFen());
        clone.Move(new Move(move.OriginalPosition, move.NewPosition));

        // 递归评估
        int score = Minimax(clone, _maxDepth - 1, 
                           int.MinValue + 1, int.MaxValue - 1, 
                           !isMaximizing);

        // 更新最佳走法
        if (isMaximizing)
        {
            if (score > bestScore) { bestScore = score; bestMove = move; }
        }
        else
        {
            if (score < bestScore) { bestScore = score; bestMove = move; }
        }
    }

    return bestMove;
}
```

### 5.3 Minimax 算法（带 Alpha-Beta 剪枝）

```csharp
private int Minimax(ChessBoard board, int depth, int alpha, int beta, bool isMaximizing)
{
    // 1. 终止条件：达到搜索深度或游戏结束
    if (depth == 0 || board.IsEndGame)
        return Evaluate(board);

    var moves = board.Moves(generateSan: false);

    if (isMaximizing)
    {
        // 白方（最大化方）
        int maxEval = int.MinValue;
        foreach (var move in moves)
        {
            var clone = ChessBoard.LoadFromFen(board.ToFen());
            clone.Move(new Move(move.OriginalPosition, move.NewPosition));
            
            int eval = Minimax(clone, depth - 1, alpha, beta, false);
            if (eval > maxEval) maxEval = eval;
            if (eval > alpha) alpha = eval;
            
            // Alpha-Beta 剪枝
            if (beta <= alpha) break;
        }
        return moves.Length == 0 ? Evaluate(board) : maxEval;
    }
    else
    {
        // 黑方（最小化方）
        int minEval = int.MaxValue;
        foreach (var move in moves)
        {
            var clone = ChessBoard.LoadFromFen(board.ToFen());
            clone.Move(new Move(move.OriginalPosition, move.NewPosition));
            
            int eval = Minimax(clone, depth - 1, alpha, beta, true);
            if (eval < minEval) minEval = eval;
            if (eval < beta) beta = eval;
            
            // Alpha-Beta 剪枝
            if (beta <= alpha) break;
        }
        return moves.Length == 0 ? Evaluate(board) : minEval;
    }
}
```

### 5.4 局面评估函数

```csharp
private static int Evaluate(ChessBoard board)
{
    // 1. 游戏结束判断
    if (board.IsEndGame)
    {
        var endgame = board.EndGame;
        if (endgame?.EndgameType == EndgameType.Checkmate)
            // 被将杀：根据轮到谁走返回极值
            return board.Turn == PieceColor.White ? -99999 : 99999;
        return 0;  // 和棋
    }

    // 2. 计算双方棋子价值总和
    int score = 0;
    for (short y = 0; y < 8; y++)
    for (short x = 0; x < 8; x++)
    {
        var piece = board.pieces[y, x];
        if (piece == null) continue;

        // 基础价值
        int value = GetPieceValue(piece.Type);
        
        // 位置奖励
        int posBonus = GetPositionBonus(piece, x, y);
        
        // 白方加分，黑方减分
        score += piece.Color == PieceColor.White 
            ? value + posBonus 
            : -(value + posBonus);
    }
    return score;
}

// 棋子基础价值
private static int GetPieceValue(PieceType type)
{
    if (type == PieceType.Pawn)   return 100;
    if (type == PieceType.Knight) return 320;
    if (type == PieceType.Bishop) return 330;
    if (type == PieceType.Rook)   return 500;
    if (type == PieceType.Queen)  return 900;
    if (type == PieceType.King)   return 20000;
    return 0;
}
```

### 5.5 位置奖励表（以兵为例）

```csharp
private static int GetPositionBonus(Piece piece, short x, short y)
{
    bool isWhite = piece.Color == PieceColor.White;
    
    // 对于黑方，翻转棋盘（从黑方视角看）
    short row = isWhite ? y : (short)(7 - y);
    short col = x;

    if (piece.Type == PieceType.Pawn)
    {
        // 兵的位置价值表
        // 鼓励兵向前推进，控制中心
        var table = new short[8, 8]
        {
            { 0,  0,  0,  0,  0,  0,  0,  0},  // 第8行（升变行）
            {50, 50, 50, 50, 50, 50, 50, 50},  // 第7行（即将升变）
            {10, 10, 20, 30, 30, 20, 10, 10},  // 第6行
            { 5,  5, 10, 25, 25, 10,  5,  5},  // 第5行
            { 0,  0,  0, 20, 20,  0,  0,  0},  // 第4行
            { 5, -5,-10,  0,  0,-10, -5,  5},  // 第3行
            { 5, 10, 10,-20,-20, 10, 10,  5},  // 第2行（初始行）
            { 0,  0,  0,  0,  0,  0,  0,  0}   // 第1行
        };
        return table[row, col];
    }
    // ... 其他棋子的位置表
}
```

### 5.6 AI 算法流程图

```
GetBestMove(board)
    │
    ├── 获取所有合法走法 moves = board.Moves()
    │
    ├── 判断当前方 isMaximizing = (board.Turn == White)
    │
    └── 遍历每个走法
            │
            ├── 克隆棋盘 clone = LoadFromFen(board.ToFen())
            ├── 执行走法 clone.Move(move)
            ├── 递归评估 score = Minimax(clone, depth-1, alpha, beta, !isMaximizing)
            │
            └── 更新最佳走法
                    如果是白方且 score > bestScore → 更新
                    如果是黑方且 score < bestScore → 更新

Minimax(board, depth, alpha, beta, isMaximizing)
    │
    ├── 终止条件检查
    │       depth == 0 或 board.IsEndGame → 返回 Evaluate(board)
    │
    └── 获取所有走法 moves = board.Moves()
            │
            ├── 如果是最大化方（白方）
            │       遍历走法，取最大评估值
            │       更新 alpha
            │       Alpha-Beta 剪枝：if beta <= alpha break
            │
            └── 如果是最小化方（黑方）
                    遍历走法，取最小评估值
                    更新 beta
                    Alpha-Beta 剪枝：if beta <= alpha break
```

---

## 6. 玩家输入处理流程

### 6.1 输入处理主方法

```csharp
public void PlayerInteract(InputAction.CallbackContext context)
{
    // 1. 检查输入是否触发
    if (!context.performed) return;
    
    // 2. 检查 AI 是否正在思考（人机模式）
    if (_aiThinking) return;
    
    // 3. 检查在线模式会话
    if (_gameMode == GameMode.Online && string.IsNullOrEmpty(_currentSession)) return;
    
    // 4. 检查本地/人机模式游戏状态
    if ((_gameMode == GameMode.Local || _gameMode == GameMode.Robot) && 
        (!_gameStarted || (_localBoard != null && _localBoard.IsEndGame))) 
        return;

    // 5. 获取鼠标位置并发射射线
    var mousePosition = Mouse.current.position.ReadValue();
    var rayOrigin = playerCamera.ScreenPointToRay(mousePosition);
    
    if (Physics.Raycast(rayOrigin, out var hitInfo))
    {
        var hitObj = hitInfo.transform.gameObject;
        var playerIsWhite = CurrentPlayerIsWhite();

        // 情况1：点击棋盘或对方棋子（执行走法）
        if (hitObj.name == "Board" || 
            (_selectedPiece != null && hitObj.name.Contains("Light") != playerIsWhite))
        {
            var boardPos = new Vector3(
                Mathf.RoundToInt(hitInfo.point.x), 
                0, 
                Mathf.RoundToInt(hitInfo.point.z)
            );
            MakeMove(_selectedPiece, boardPos);
        }
        // 情况2：点击己方棋子（选中棋子）
        else if (hitObj.name.Contains("Light") == playerIsWhite)
        {
            SelectPiece(hitObj);
            Debug.Log($"Piece selected: {_selectedPiece.name}");
        }
        // 情况3：其他情况（取消选中）
        else
        {
            SelectPiece(null);
        }
    }
    else
    {
        // 点击空白处，取消选中
        SelectPiece(null);
    }
}
```

### 6.2 棋子选中逻辑

```csharp
private void SelectPiece(GameObject piece)
{
    // 1. 恢复之前选中棋子的颜色
    if (_selectedPiece != null)
    {
        ChangeMaterialColor(_selectedPiece,
            _selectedPiece.name.Contains("Light") ? _lightColor : _darkColor);
    }
    
    // 2. 更新选中棋子
    _selectedPiece = piece;
    
    // 3. 如果选中了新棋子，高亮显示
    if (_selectedPiece == null) return;
    ChangeMaterialColor(_selectedPiece, _selectedColor);
}

private void ChangeMaterialColor(GameObject obj, Color newColor)
{
    var selectedRenderer = obj.GetComponent<Renderer>();
    selectedRenderer.material.color = newColor;
}
```

### 6.3 颜色定义

```csharp
// 选中颜色（蓝色）
private readonly Color32 _selectedColor = new (84, 84, 255, 255);

// 浅色棋子原始颜色
private readonly Color32 _lightColor = new(223, 210, 194, 255);

// 深色棋子原始颜色
private readonly Color32 _darkColor = new (84, 84, 84, 255);
```

---

## 7. 走法执行流程

### 7.1 坐标转换

```csharp
// 将世界坐标转换为 FEN 表示法（如 "e2"）
private string PosToFen(Vector3 pos)
{
    // pos.x: 0-7 对应 a-h
    // pos.z: 0-7 对应 1-8
    return (char)(pos.x + 97) + ((char)pos.z + 1).ToString();
}

// 示例：
// pos(4, 0, 1) → "e2"
// pos(0, 0, 0) → "a1"
// pos(7, 0, 7) → "h8"
```

### 7.2 走法执行流程图

```
玩家点击目标位置
        │
        ▼
MakeMove(piece, toPos)
        │
        ├── 转换坐标：fromFen = PosToFen(piece.position)
        ├── 转换坐标：toFen = PosToFen(toPos)
        │
        ├── 本地模式？
        │       └── MakeLocalMove(fromFen, toFen)
        │
        ├── 人机模式？
        │       ├── MakeLocalMove(fromFen, toFen, updateUI: false)
        │       └── 如果成功 → DoRobotMoveAsync()
        │
        └── 在线模式？
                └── 调用云服务 MakeMove
```

---

## 8. 棋盘同步与渲染

### 8.1 棋盘同步方法

```csharp
private void SyncBoard(string fen)
{
    // 1. 将 FEN 字符串转换为棋子位置字典
    var boardState = FenToDict(fen);
    
    try
    {
        // 2. 清除当前棋盘上的所有棋子
        foreach (Transform child in board.transform)
        {
            Destroy(child.gameObject);
        }
        
        // 3. 遍历每个棋子位置，实例化对应棋子
        foreach (var piece in boardState)
        {
            // 解析棋子类型
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
            
            // 确定预制体名称（如 "PawnLight" 或 "RookDark"）
            var prefabName = pieceType + (char.IsUpper(piece.Value) ? "Light" : "Dark");
            
            // 加载预制体（缓存已加载的预制体）
            if (!_prefabs.ContainsKey(prefabName))
            {
                _prefabs[prefabName] = Resources.Load($"{pieceType}/Prefabs/{prefabName}");    
            }
            
            // 实例化棋子
            var newObject = Instantiate(_prefabs[prefabName], board.transform);
            
            // 设置位置
            newObject.GameObject().transform.position = 
                new Vector3(piece.Key.Item1, 0, piece.Key.Item2);
            
            // 设置旋转（黑方棋子旋转180度）
            newObject.GameObject().transform.rotation = 
                Quaternion.Euler(0, char.IsLower(piece.Value)? 180 : 0, 0);
        }
    }
    catch (CloudCodeException exception)
    {
        Debug.LogException(exception);
    }
}
```

### 8.2 FEN 转字典

```csharp
private static Dictionary<Tuple<int, int>, char> FenToDict(string fen)
{
    // 分割 FEN 字符串（只取棋盘部分）
    var fenParts = fen.Split(' ');
    var boardState = fenParts[0];
    
    // 按行分割
    var ranks = boardState.Split('/');

    var coordinatesDict = new Dictionary<Tuple<int, int>, char>();
    var x = 0;  // 列：a-h (0-7)
    var y = 7;  // 行：8-1 (7-0)

    foreach (var rank in ranks)
    {
        foreach (var c in rank)
        {
            if (char.IsDigit(c))
            {
                // 数字表示空格格数
                x += int.Parse(c.ToString());
            }
            else
            {
                // 字母表示棋子
                var coordinates = new Tuple<int, int>(x, y);
                coordinatesDict.Add(coordinates, c);
                x += 1;
            }
        }
        // 换行
        x = 0;
        y -= 1;
    }

    return coordinatesDict;
}

// FEN 示例：
// "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"
// 
// rnbqkbnr  → 第8行：黑方后排棋子
// pppppppp  → 第7行：黑方兵
// 8         → 第6行：8个空格
// 8         → 第5行：8个空格
// 8         → 第4行：8个空格
// 8         → 第3行：8个空格
// PPPPPPPP  → 第2行：白方兵
// RNBQKBNR  → 第1行：白方后排棋子
```

---

## 9. 游戏结束处理

### 9.1 结束条件检测

游戏结束可能由以下原因触发：

1. **将杀 (Checkmate)** - 一方王被将军且无法应将
2. **逼和 (Stalemate)** - 一方无合法走法但王未被将军
3. **认输 (Resign)** - 一方主动认输
4. **和棋声明 (DrawDeclared)** - 双方同意和棋
5. **超时 (Timeout)** - 一方时间用完
6. **三次重复 (Repetition)** - 同一局面出现三次
7. **50步规则 (FiftyMoveRule)** - 50步内无吃子或兵移动
8. ** insufficient material** - 双方子力不足以将杀

### 9.2 本地游戏结束处理

```csharp
// 在 MakeLocalMove 中检测
if (_localBoard.IsEndGame)
{
    // 显示结果面板
    uiPanel.SetActive(true);
    resignButton.SetActive(false);
    
    // 显示结束类型（如 "Checkmate"、"Stalemate" 等）
    resultText.text = _localBoard.EndGame?.EndgameType.ToString();
    
    // 更新标题
    playerNameText.text = "Game Over";
    
    // 标记游戏结束
    _gameStarted = false;
    return true;
}
```

### 9.3 人机模式结束处理

```csharp
// 玩家走完后检查
if (MakeLocalMove(fromFen, toFen, updateUI: false) && !_localBoard.IsEndGame)
{
    // 游戏未结束，AI 走
    _ = DoRobotMoveAsync();
}

// AI 走完后检查（在 DoRobotMoveAsync 中）
if (_localBoard.IsEndGame)
{
    uiPanel.SetActive(true);
    resignButton.SetActive(false);
    resultText.text = _localBoard.EndGame?.EndgameType.ToString();
    playerNameText.text = "Game Over";
    _gameStarted = false;
}
```

---

## 10. 流程总结

### 10.1 本地双人对战流程

```
StartLocalGame()
    │
    ├── 创建 ChessBoard 实例
    ├── _localWhiteTurn = true
    ├── 同步棋盘显示
    └── playerNameText = "White's Turn"
            │
            ▼
    玩家1（白方）点击棋子 → SelectPiece()
            │
            ▼
    玩家1 点击目标位置 → MakeMove()
            │
            ▼
    MakeLocalMove(from, to)
            │
            ├── 验证走法 → IsValidMove()
            ├── 执行走法 → _localBoard.Move()
            ├── 同步棋盘 → SyncBoard()
            ├── 检查结束？→ 显示结果
            └── 切换回合
                    │
                    ├── _localWhiteTurn = false
                    ├── 旋转视角 SetPovLocal()
                    └── playerNameText = "Black's Turn"
                            │
                            ▼
                    玩家2（黑方）走棋...
                            │
                            ▼
                    循环直到游戏结束
```

### 10.2 人机对战流程

```
StartRobotGame()
    │
    ├── 创建 ChessBoard 实例
    ├── 创建 ChessAI 实例（深度3）
    ├── 同步棋盘显示
    └── playerNameText = "Your Turn (White)"
            │
            ▼
    玩家（白方）点击棋子 → SelectPiece()
            │
            ▼
    玩家 点击目标位置 → MakeMove()
            │
            ▼
    MakeLocalMove(from, to, updateUI: false)
            │
            ├── 验证并执行走法
            ├── 同步棋盘
            └── 检查结束？
                    │
                    ├── 未结束 → DoRobotMoveAsync()
                    │           │
                    │           ├── _aiThinking = true
                    │           ├── AI 计算最佳走法（后台线程）
                    │           ├── 执行 AI 走法
                    │           ├── 同步棋盘
                    │           ├── 检查结束？
                    │           └── _aiThinking = false
                    │                   │
                    │                   └── 回到玩家回合
                    │
                    └── 结束 → 显示结果
```

---

## 11. 关键设计要点

### 11.1 状态隔离

- 使用 `ChessBoard.LoadFromFen()` 创建棋盘副本，AI 计算时不影响原棋盘
- 每次 AI 评估都使用独立的棋盘实例

### 11.2 异步处理

- AI 计算在后台线程执行（`Task.Run`），避免阻塞主线程导致界面卡顿
- 使用 `await` 等待 AI 计算完成

### 11.3 输入防护

- `_aiThinking` 标志防止玩家在 AI 思考时操作
- 游戏结束检查防止继续走棋

### 11.4 视角切换

- 本地模式：每回合自动旋转摄像机到当前玩家视角
- 人机模式：固定为白方视角（玩家视角）

---

*文档生成时间：2026-05-14*
*基于项目代码版本：Unity Multiplayer Chess Cloud Code Sample*
