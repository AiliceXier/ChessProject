# Unity Cloud Code 云端代码架构详解

## 概述

本项目使用 **Unity Cloud Code** 作为后端服务，实现多人在线对战功能。云端代码负责游戏大厅管理、棋盘状态同步、玩家匹配和实时消息推送。

---

## 1. 技术栈与核心组件

### 1.1 使用的 Unity 服务

| 服务 | 用途 |
|------|------|
| **Cloud Code** | 服务端逻辑（C# 云函数） |
| **Lobby** | 游戏大厅管理、玩家匹配 |
| **Cloud Save** | 持久化存储棋盘状态 |
| **Push Notifications** | 实时消息推送（WebSocket） |
| **Authentication** | 匿名玩家身份验证 |

### 1.2 云端代码文件结构

```
ChessCloudCode/
├── Chess.cs              # 核心云函数（HostGame, JoinGame, MakeMove, Resign）
├── ModuleConfig.cs       # 依赖注入配置
└── Properties/
    └── PublishProfiles/
        └── FolderProfile.pubxml  # 发布配置
```

---

## 2. 云端代码核心类详解

### 2.1 Chess 类 — 云函数入口

[Chess.cs](file:///d:/unity/my_chess/ChessCloudCode/Chess.cs) 包含 4 个云函数：

#### 构造函数与依赖注入

```csharp
public Chess(IGameApiClient gameApiClient, IPushClient pushClient, 
             ILogger<Chess> logger, Random rng)
{
    _gameApiClient = gameApiClient;  // 游戏 API 客户端
    _pushClient = pushClient;        // 消息推送客户端
    _logger = logger;                // 日志记录器
    _rng = rng;                      // 随机数生成器
}
```

依赖在 [ModuleConfig.cs](file:///d:/unity/my_chess/ChessCloudCode/ModuleConfig.cs) 中配置：

```csharp
public void Setup(ICloudCodeConfig config)
{
    config.Dependencies.AddSingleton(GameApiClient.Create());          // 游戏 API
    config.Dependencies.AddSingleton<IPushClient, PushClient>(_ => PushClient.Create());  // 推送
    config.Dependencies.AddSingleton(new Random());                     // 随机数
}
```

---

## 3. 云函数详解

### 3.1 HostGame — 创建游戏

**功能**：房主创建游戏大厅

**流程**：

```
玩家点击 "Create Game"
    │
    ▼
CloudCode: HostGame()
    │
    ├── 1. 创建 Lobby（2人房间）
    │   └── lobbyResult.Data.Id      → Lobby ID
    │   └── lobbyResult.Data.LobbyCode → 6位房间码
    │
    ├── 2. 初始化棋盘（FEN格式）
    │   └── "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"
    │
    ├── 3. Cloud Save 存储游戏状态
    │   ├── key: "board"         → 棋盘 FEN
    │   └── key: "whitePlayerId" → 房主 PlayerId
    │
    └── 4. 返回 HostGameResponse
        └── LobbyCode: "ABC123"
```

**代码**（第26-43行）：

```csharp
[CloudCodeFunction("HostGame")]
public async Task<HostGameResponse> HostGame(IExecutionContext context)
{
    // 1. 创建 Lobby
    var lobbyResult = await _gameApiClient.Lobby.CreateLobbyAsync(
        context, context.AccessToken, null, null,
        new CreateRequest($"{context.PlayerId}'s game", 2, false, false, 
            new Player(context.PlayerId)));
    
    // 2. 初始化棋盘
    var chessBoard = new ChessBoard();
    
    // 3. Cloud Save 存储
    await _gameApiClient.CloudSaveData.SetCustomItemBatchAsync(
        context, context.ServiceToken, context.ProjectId, lobbyResult.Data.Id,
        new SetItemBatchBody(new List<SetItemBody>(){ 
            new("board", chessBoard.ToFen()),
            new("whitePlayerId", context.PlayerId)
        }));

    return new HostGameResponse() { LobbyCode = lobbyResult.Data.LobbyCode };
}
```

---

### 3.2 JoinGame — 加入游戏

**功能**：玩家通过房间码加入游戏，或重新连接已加入的游戏

**流程**：

```
玩家输入房间码 → JoinLobbyByCode
    │
    ├── 成功 → StartGame() → 分配黑白方 → 推送消息给房主
    │
    └── 失败 → 检查是否已加入其他 Lobby → Rejoin()
```

**关键逻辑**（第45-74行）：

```csharp
[CloudCodeFunction("JoinGame")]
public async Task<JoinGameResponse> JoinGame(IExecutionContext context, string lobbyCode)
{
    try
    {
        // 尝试加入房间
        var joinLobbyResponse = await _gameApiClient.Lobby.JoinLobbyByCodeAsync(...);
        return await StartGame(context, joinLobbyResponse.Data);
    }
    catch (Exception e)
    {
        // 加入失败，检查是否已在其他房间
        var lobbyIdsResponse = await _gameApiClient.Lobby.GetJoinedLobbiesAsync(...);
        var joinedLobbyId = lobbyIdsResponse.Data.FirstOrDefault();
        
        if (string.IsNullOrEmpty(joinedLobbyId))
            throw new InvalidOperationException("Unable to join lobby...", e);
        
        // 重新连接已加入的房间
        var lobbyResponse = await _gameApiClient.Lobby.GetLobbyAsync(...);
        return await Rejoin(context, lobbyResponse.Data);
    }
}
```

#### StartGame — 开始游戏（第76-111行）

**功能**：第二位玩家加入后，随机分配黑白方，通知双方

```csharp
private async Task<JoinGameResponse> StartGame(IExecutionContext context, Lobby lobby)
{
    var opponentId = lobby.Players.Select(p => p.Id).First(id => id != context.PlayerId);
    var isWhite = _rng.NextDouble() >= 0.5;  // 随机分配

    // 保存双方 PlayerId
    await _gameApiClient.CloudSaveData.SetCustomItemBatchAsync(...,
        new SetItemBatchBody(new List<SetItemBody>()
        {
            new("board", chessBoard.ToFen()),
            new("whitePlayerId", isWhite ? context.PlayerId : opponentId),
            new("blackPlayerId", isWhite ? opponentId : context.PlayerId)
        }));

    // 推送消息给对手（房主）
    var message = new JoinGameResponse()
    {
        Session = lobby.Id,
        Board = chessBoard.ToFen(),
        OpponentId = context.PlayerId,
        IsWhite = !isWhite
    };
    await _pushClient.SendPlayerMessageAsync(
        context, JsonConvert.SerializeObject(message), "opponentJoined", opponentId);

    // 返回给当前玩家
    return new JoinGameResponse()
    {
        Session = lobby.Id,
        Board = chessBoard.ToFen(),
        OpponentId = opponentId,
        IsWhite = isWhite
    };
}
```

#### Rejoin — 重新连接（第113-145行）

**功能**：玩家断线后重新进入游戏，恢复游戏状态

```csharp
private async Task<JoinGameResponse> Rejoin(IExecutionContext context, Lobby lobby)
{
    // 从 Cloud Save 读取游戏状态
    var saveResponse = await _gameApiClient.CloudSaveData.GetCustomItemsAsync(...);
    
    var boardResult = saveResponse.Data.Results.Find(r => r.Key == "board");
    var whiteResult = saveResponse.Data.Results.Find(r => r.Key == "whitePlayerId");
    
    var chessBoard = ChessBoard.LoadFromFen(boardResult.Value.ToString());
    var playerIsWhite = whiteResult.Value.ToString() == context.PlayerId;
    
    return new JoinGameResponse()
    {
        Session = lobby.Id,
        Board = chessBoard.ToFen(),
        OpponentId = opponentId,
        IsWhite = playerIsWhite
    };
}
```

---

### 3.3 MakeMove — 执行走法

**功能**：验证并执行玩家走法，同步给对手

**流程**：

```
玩家走棋 → MakeMove(session, fromPosition, toPosition)
    │
    ├── 1. 读取 Cloud Save 中的游戏状态
    │   ├── board, whitePlayerId, blackPlayerId
    │
    ├── 2. 验证玩家身份和回合
    │   ├── 确认玩家是白方或黑方
    │   └── 确认是当前玩家的回合（chessBoard.Turn）
    │
    ├── 3. 验证走法合法性
    │   └── chessBoard.IsValidMove(new Move(fromPosition, toPosition))
    │
    ├── 4. 执行走法
    │   └── chessBoard.Move(new Move(fromPosition, toPosition))
    │
    ├── 5. 保存新状态到 Cloud Save
    │
    └── 6. 推送消息给对手
        └── messageType: "boardUpdated"
```

**代码**（第147-199行）：

```csharp
[CloudCodeFunction("MakeMove")]
public async Task<BoardUpdateResponse> MakeMove(
    IExecutionContext context, string session, string fromPosition, string toPosition)
{
    // 1. 读取游戏状态
    var saveResponse = await _gameApiClient.CloudSaveData.GetCustomItemsAsync(
        context, context.ServiceToken, context.ProjectId, session,
        new List<string> { "board", "whitePlayerId", "blackPlayerId" });

    var chessBoard = ChessBoard.LoadFromFen(
        saveResponse.Data.Results.Find(r => r.Key == "board").Value.ToString());
    var whitePlayer = saveResponse.Data.Results.Find(r => r.Key == "whitePlayerId").Value.ToString();
    var blackPlayer = saveResponse.Data.Results.Find(r => r.Key == "blackPlayerId").Value.ToString();

    // 2. 确定玩家颜色
    var playerColour = context.PlayerId switch
    {
        var value when value == whitePlayer => PieceColor.White,
        var value when value == blackPlayer => PieceColor.Black,
        _ => throw new Exception("Player is not in the game")
    };

    // 3. 检查是否轮到该玩家
    if (chessBoard.Turn != playerColour)
        throw new Exception("Invalid move, not active player");

    // 4. 验证走法
    if (!chessBoard.IsValidMove(new Move(fromPosition, toPosition)))
        throw new Exception($"Invalid move from {fromPosition} to {toPosition}");

    // 5. 执行走法
    chessBoard.Move(new Move(fromPosition, toPosition));

    // 6. 保存状态
    await _gameApiClient.CloudSaveData.SetCustomItemAsync(
        context, context.ServiceToken, context.ProjectId, session,
        new SetItemBody("board", chessBoard.ToFen()));

    // 7. 推送消息给对手
    var opponentId = playerIsWhite ? blackPlayer : whitePlayer;
    var boardUpdatedResponse = new BoardUpdateResponse
    {
        Board = chessBoard.ToFen(),
        GameOver = chessBoard.IsEndGame,
        EndgameType = chessBoard.EndGame?.EndgameType.ToString()
    };
    await _pushClient.SendPlayerMessageAsync(
        context, JsonConvert.SerializeObject(boardUpdatedResponse), "boardUpdated", opponentId);

    return boardUpdatedResponse;
}
```

---

### 3.4 Resign — 认输

**功能**：玩家认输，结束游戏并通知对手

**流程**：与 MakeMove 类似，但调用 `chessBoard.Resign(playerColour)`

---

## 4. 客户端与云端交互流程

### 4.1 整体架构图

```
┌─────────────────┐         ┌──────────────────┐
│   客户端 (Unity) │         │   Unity Cloud    │
│                 │         │                  │
│  Player.cs      │◄───────►│  Cloud Code      │
│  - Online模式   │  HTTP   │  - HostGame      │
│  - 调用云函数   │         │  - JoinGame      │
│  - 接收推送     │         │  - MakeMove      │
│                 │         │  - Resign        │
└────────┬────────┘         └────────┬─────────┘
         │                           │
         │    WebSocket 推送消息     │
         │◄──────────────────────────┘
         │
         ▼
┌─────────────────┐
│  消息处理       │
│  - boardUpdated │
│  - opponentJoined│
└─────────────────┘
```

### 4.2 客户端代码（Player.cs）

#### 初始化与订阅

```csharp
private async Task InitializeAsync()
{
    await UnityServices.InitializeAsync();
    await AuthenticationService.Instance.SignInAnonymouslyAsync();
    await SubscribeToPlayerMessages();  // 订阅推送消息
}

private Task SubscribeToPlayerMessages()
{
    var callbacks = new SubscriptionEventCallbacks();
    callbacks.MessageReceived += @event =>
    {
        switch (@event.MessageType)
        {
            case "boardUpdated":      // 对手走棋
                var message = JsonConvert.DeserializeObject<BoardUpdateResponse>(@event.Message);
                OnBoardUpdate(message);
                break;
            case "opponentJoined":    // 对手加入
                var opponentJoinedMessage = JsonConvert.DeserializeObject<JoinGameResponse>(@event.Message);
                OnGameStart(opponentJoinedMessage);
                break;
        }
    };
    return CloudCodeService.Instance.SubscribeToPlayerMessagesAsync(callbacks);
}
```

#### 调用云函数示例

```csharp
// 创建游戏
public async void CreateGame()
{
    var hostGameResponse = await CloudCodeService.Instance.CallModuleEndpointAsync<HostGameResponse>(
        "ChessCloudCode", "HostGame");
    lobbyCodeText.text = hostGameResponse.LobbyCode;
}

// 加入游戏
public async void JoinLobbyByCode()
{
    var joinGameResponse = await CloudCodeService.Instance.CallModuleEndpointAsync<JoinGameResponse>(
        "ChessCloudCode", "JoinGame",
        new Dictionary<string, object> { { "lobbyCode", sanitizedLobbyCode } });
    OnGameStart(joinGameResponse);
}

// 走棋
private async void MakeMove(GameObject piece, Vector3 toPos)
{
    var result = await CloudCodeService.Instance.CallModuleEndpointAsync<BoardUpdateResponse>(
        "ChessCloudCode", "MakeMove",
        new Dictionary<string, object>
        {
            { "session", _currentSession },
            { "fromPosition", fromFen },
            { "toPosition", toFen }
        });
    OnBoardUpdate(result);
}
```

---

## 5. 数据流与状态管理

### 5.1 游戏状态存储

**Cloud Save 数据结构**（以 Lobby ID 为 key）：

```json
{
  "board": "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1",
  "whitePlayerId": "player_abc123",
  "blackPlayerId": "player_def456"
}
```

### 5.2 消息推送格式

**boardUpdated**（对手走棋）：

```json
{
  "Board": "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1",
  "GameOver": false,
  "EndgameType": null
}
```

**opponentJoined**（对手加入）：

```json
{
  "Session": "lobby_xyz789",
  "Board": "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
  "OpponentId": "player_abc123",
  "IsWhite": true
}
```

---

## 6. 云端代码的作用总结

| 功能 | 云端代码职责 | 客户端职责 |
|------|-------------|-----------|
| **游戏创建** | 创建 Lobby，初始化棋盘，存储状态 | 调用 HostGame，显示房间码 |
| **玩家匹配** | 处理加入请求，随机分配黑白方 | 输入房间码，调用 JoinGame |
| **走法验证** | 验证回合、玩家身份、走法合法性 | 发送走法请求，接收结果 |
| **状态同步** | 保存棋盘状态，推送消息给对手 | 接收推送，更新棋盘显示 |
| **断线重连** | 从 Cloud Save 恢复游戏状态 | 调用 JoinGame，自动重连 |
| **游戏结束** | 处理认输，标记游戏结束 | 显示结束界面 |

---

## 7. 关键设计决策

### 7.1 为什么使用 Cloud Code？

1. **安全性**：走法验证在服务端执行，防止作弊
2. **状态一致性**：单点存储棋盘状态，避免客户端不一致
3. **实时性**：WebSocket 推送实现低延迟同步
4. **可靠性**：Cloud Save 持久化，支持断线重连

### 7.2 为什么使用 FEN 格式？

- FEN（Forsyth-Edwards Notation）是国际标准象棋记录格式
- 紧凑的字符串表示，包含棋盘、回合、王车易位权、吃过路兵目标等全部信息
- 便于存储、传输和恢复游戏状态

### 7.3 消息推送 vs 轮询

- **推送（WebSocket）**：对手走棋后立即通知，低延迟
- **避免轮询**：减少服务器负载和客户端电量消耗

---

## 8. 部署与发布

云端代码通过 `.pubxml` 配置文件发布到 Unity Cloud 服务：

```xml
<!-- FolderProfile.pubxml -->
<?xml version="1.0" encoding="utf-8"?>
<Project>
  <PropertyGroup>
    <PublishProtocol>FileSystem</PublishProtocol>
    <Configuration>Release</Configuration>
    <TargetFramework>net6.0</TargetFramework>
    <RuntimeIdentifier>linux-x64</RuntimeIdentifier>
    <SelfContained>true</SelfContained>
  </PropertyGroup>
</Project>
```

发布命令：

```bash
dotnet publish -c Release -r linux-x64 --self-contained
```

发布后，Unity Dashboard 中配置 Cloud Code 模块并上传。
