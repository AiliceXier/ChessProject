# 修复 Robot Game AI 卡死 & 显示信息检查计划

## 问题概述

1. **AI Thinking 卡死**: 在 Robot Game 模式中，当白方走棋后，AI 一直显示 "AI Thinking..." 无法响应
2. **显示信息检查**: 检查所有游戏模式的 opponent name 和其他显示信息是否正确

---

## 问题一：AI Thinking 卡死 — 根因分析

经过深入代码审查，发现以下 **4 个关键问题** 导致 AI 卡死：

### 根因 1：`DoRobotMoveAsync` 缺少异常处理（最关键）

[Player.cs:252-289](file:///d:/unity/my_chess/Chess/Assets/Player.cs#L252-L289) 中 `DoRobotMoveAsync` 方法没有 try-catch。该方法通过 `_ = DoRobotMoveAsync()` 以 fire-and-forget 方式调用，如果 `GetBestMove` 抛出任何异常，`_aiThinking` 永远不会被重置为 `false`，导致游戏永久卡在 "AI Thinking..." 状态。

```csharp
// 当前代码 — 无异常保护
private async Task DoRobotMoveAsync()
{
    _aiThinking = true;
    playerNameText.text = "AI Thinking...";
    Move aiMove = null;
    var boardSnapshot = _localBoard;
    await Task.Run(() =>
    {
        aiMove = _chessAI.GetBestMove(boardSnapshot);  // 如果这里抛异常...
    });
    // ... 后续代码不会执行 ...
    _aiThinking = false;  // 永远不会执行！
}
```

### 根因 2：嵌套 `Task.Run` 导致线程池饥饿

[ChessGenerations.cs:28-43](file:///d:/unity/my_chess/Chess/Assets/Chess/ChessBoard/ChessGenerations.cs#L28-L43) 中 `Moves()` 方法内部使用 `Task.Run` + `Task.WaitAll` 进行并行走法生成。当 `GetBestMove` 从 `DoRobotMoveAsync` 的 `Task.Run` 中调用时，`Minimax` 递归中每次调用 `board.Moves()` 都会再创建嵌套的 `Task.Run`。

- 搜索深度 3，每层约 30 个合法走法
- 每个节点调用 `Moves()` 创建 ~16 个 Task（棋盘上约 16 个棋子）
- 总共可能创建 **30 × 16 × 30 × 16 × 30 × 16 ≈ 1100 万个 Task**
- 导致线程池严重饥饿，AI 计算极慢甚至死锁

### 根因 3：FEN 序列化/反序列化克隆棋盘极其低效

[ChessAI.cs:27-28](file:///d:/unity/my_chess/Chess/Assets/Chess/AI/ChessAI.cs#L27-L28) 和 [ChessAI.cs:57-58](file:///d:/unity/my_chess/Chess/Assets/Chess/AI/ChessAI.cs#L57-L58) 中，每个搜索节点都通过 `ChessBoard.LoadFromFen(board.ToFen())` 克隆棋盘：

```csharp
var clone = ChessBoard.LoadFromFen(board.ToFen());  // 极其昂贵！
clone.Move(new Move(move.OriginalPosition, move.NewPosition));
```

这涉及：
1. 将整个棋盘状态序列化为 FEN 字符串（字符串分配、拼接）
2. 正则表达式解析 FEN 字符串
3. 重建整个 ChessBoard 对象（包括 FenBoardBuilder、headers 等）

在搜索树中执行数万次，性能极差。

### 根因 4：`boardSnapshot` 不是深拷贝

[Player.cs:258](file:///d:/unity/my_chess/Chess/Assets/Player.cs#L258) 中：

```csharp
var boardSnapshot = _localBoard;  // 只是引用复制，不是深拷贝！
```

虽然 `GetBestMove` 内部不修改原棋盘，但 `board.Moves()` 在后台线程访问 `_localBoard` 存在线程安全隐患。

---

## 问题二：显示信息检查 — 发现的问题

### 问题 2.1：Online 模式缺少玩家名称和回合显示

[Player.cs:382-392](file:///d:/unity/my_chess/Chess/Assets/Player.cs#L382-L392) 中 `OnGameStart` 方法没有更新 `playerNameText`：

```csharp
private async void OnGameStart(JoinGameResponse joinGameResponse)
{
    Debug.Log($"Opponent joined: {joinGameResponse.OpponentId}");
    // ... 缺少 playerNameText.text 的更新！
}
```

### 问题 2.2：Online 模式走棋后缺少回合显示

[Player.cs:371-380](file:///d:/unity/my_chess/Chess/Assets/Player.cs#L371-L380) 中 `OnBoardUpdate` 方法没有更新 `playerNameText` 显示当前回合。

### 问题 2.3：各模式显示文本不一致

| 模式 | 白方回合 | 黑方回合 | AI 思考 |
|------|---------|---------|---------|
| Local | "White's Turn" | "Black's Turn" | N/A |
| Robot | "Your Turn (White)" | N/A | "AI Thinking..." |
| Online | (未设置) | (未设置) | N/A |

### 问题 2.4：结束游戏显示不够友好

`_localBoard.EndGame?.EndgameType.ToString()` 直接输出枚举名如 "Checkmate"、"Stalemate"，对用户不够友好。

---

## 修复计划

### 步骤 1：修复 `DoRobotMoveAsync` 异常处理

**文件**: `Chess/Assets/Player.cs`

在 `DoRobotMoveAsync` 方法中添加 try-catch-finally，确保 `_aiThinking` 始终被重置：

```csharp
private async Task DoRobotMoveAsync()
{
    _aiThinking = true;
    playerNameText.text = "AI Thinking...";

    try
    {
        Move aiMove = null;
        var boardSnapshot = ChessBoard.LoadFromFen(_localBoard.ToFen());  // 深拷贝
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

        if (_localBoard.IsEndGame)
        {
            uiPanel.SetActive(true);
            resignButton.SetActive(false);
            resultText.text = GetEndGameText(_localBoard.EndGame);
            playerNameText.text = "Game Over";
            _gameStarted = false;
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
```

### 步骤 2：优化 `ChessAI` — 替换 FEN 克隆为直接复制

**文件**: `Chess/Assets/Chess/AI/ChessAI.cs`

使用 `ChessBoard` 的内部构造函数 `ChessBoard(Piece?[,] pieces, List<Move> moves)` 替代 FEN 克隆：

```csharp
private static ChessBoard CloneBoard(ChessBoard board)
{
    var clone = new ChessBoard(board.pieces, board.executedMoves);
    clone.FenBuilder = board.FenBuilder;
    clone.moveIndex = board.MoveIndex;
    return clone;
}
```

在 `GetBestMove` 和 `Minimax` 中使用 `CloneBoard` 替代 `ChessBoard.LoadFromFen(board.ToFen())`。

### 步骤 3：优化 `ChessAI` — 避免嵌套 Task.Run

**文件**: `Chess/Assets/Chess/AI/ChessAI.cs`

在 AI 的 `Minimax` 和 `GetBestMove` 中，添加一个同步的走法生成方法，避免在后台线程中嵌套调用 `Moves()`（它内部使用 `Task.Run`）。

方案：创建一个 `GenerateMovesSync` 方法，直接遍历棋盘生成走法而不使用 `Task.Run`：

```csharp
private static Move[] GenerateMovesSync(ChessBoard board)
{
    var moves = new List<Move>();
    for (short i = 0; i < 8; i++)
    for (short j = 0; j < 8; j++)
    {
        if (board.pieces[i, j] != null)
        {
            var pieceMoves = board.Moves(new Position { Y = i, X = j }, generateSan: false);
            moves.AddRange(pieceMoves);
        }
    }
    return moves.ToArray();
}
```

> 注意：`board.Moves(Position, ...)` 单个棋子的走法生成不使用 `Task.Run`，只有 `board.Moves()` 全量走法生成才使用。所以遍历每个棋子单独调用是安全的。

### 步骤 4：修复 `boardSnapshot` 深拷贝问题

**文件**: `Chess/Assets/Player.cs`

将 `var boardSnapshot = _localBoard;` 改为 `var boardSnapshot = ChessBoard.LoadFromFen(_localBoard.ToFen());`，确保 AI 在独立副本上计算。

（已包含在步骤 1 的代码中）

### 步骤 5：修复 Online 模式显示信息

**文件**: `Chess/Assets/Player.cs`

5a. 在 `OnGameStart` 中添加玩家名称显示：

```csharp
private async void OnGameStart(JoinGameResponse joinGameResponse)
{
    Debug.Log($"Opponent joined: {joinGameResponse.OpponentId}");
    _currentSession = joinGameResponse.Session;
    SyncBoard(joinGameResponse.Board);
    uiPanel.SetActive(false);
    resignButton.SetActive(true);
    _isWhite = joinGameResponse.IsWhite;
    SetPov();
    _gameStarted = true;
    playerNameText.text = _isWhite ? "Your Turn (White)" : "Your Turn (Black)";
}
```

5b. 在 `OnBoardUpdate` 中添加回合显示：

```csharp
private async void OnBoardUpdate(BoardUpdateResponse boardUpdateResponse)
{
    SyncBoard(boardUpdateResponse.Board);
    if (boardUpdateResponse.GameOver)
    {
        uiPanel.SetActive(true);
        resignButton.SetActive(false);
        resultText.text = boardUpdateResponse.EndgameType;
        playerNameText.text = "Game Over";
    }
    else
    {
        playerNameText.text = "Opponent's Turn";
    }
}
```

### 步骤 6：添加友好的结束游戏文本

**文件**: `Chess/Assets/Player.cs`

添加 `GetEndGameText` 辅助方法：

```csharp
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
        _ => endGame.EndgameType.ToString()
    };
}
```

将所有使用 `_localBoard.EndGame?.EndgameType.ToString()` 的地方替换为 `GetEndGameText(_localBoard.EndGame)`。

### 步骤 7：统一 Robot 模式的结束游戏显示

**文件**: `Chess/Assets/Player.cs`

在 Robot 模式中，结束游戏文本应更具体：
- 将军杀：根据赢方显示 "Checkmate - You Win!" 或 "Checkmate - AI Wins!"
- 逼和："Stalemate - Draw"

修改 `DoRobotMoveAsync` 和 `MakeLocalMove` 中 Robot 模式的结束游戏显示逻辑。

---

## 修改文件清单

| 文件 | 修改内容 |
|------|---------|
| `Chess/Assets/Player.cs` | 添加 try-catch-finally、深拷贝、Online 显示、友好结束文本 |
| `Chess/Assets/Chess/AI/ChessAI.cs` | 替换 FEN 克隆、避免嵌套 Task.Run、添加 CloneBoard |

---

## 验证步骤

1. 启动 Robot Game，移动白方 d 兵到 d3 或 d4，确认 AI 能正常响应
2. 测试各种结束游戏场景（将杀、逼和、认输）
3. 启动 Local Game，确认回合显示正确
4. 检查 Online 模式的玩家名称和回合显示
5. 在 Unity Editor 中确认无编译错误
