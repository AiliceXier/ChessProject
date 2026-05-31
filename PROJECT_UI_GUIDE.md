# Chess 项目功能与UI详细说明文档

> 本文档按照完整游戏流程顺序，详细描述每个界面的UI设计、交互元素及其行为，供功能验收测试使用。

---

## 一、游戏启动与初始化

### 1.1 启动流程

游戏启动后自动执行以下步骤：

1. **加载场景** `ChessDemo`，场景包含：Directional Light、CameraPivot（含Main Camera）、BoardPivot（含Board棋盘）、Player游戏控制器、Canvas
2. **Player.Start()** 初始化：
   - 同步初始棋盘（标准开局FEN）
   - 创建所有UI组件实例（MoveHistoryUI、CommandInputUI、DifficultySelector、EvaluationBar、HintSystem、ChatUI、MainMenuUI）
   - MainMenuUI 接管 UIPanel，构建菜单布局
   - 隐藏所有游戏内UI面板
   - 显示主菜单
3. **异步初始化** Unity Services：
   - `UnityServices.InitializeAsync()`
   - `AuthenticationService.Instance.SignInAnonymouslyAsync()` 匿名登录
   - `SubscribeToPlayerMessages()` 订阅在线消息推送
   - 初始化完成后隐藏 Resign 按钮

### 1.2 启动后界面状态

- **棋盘**：显示标准开局32颗棋子
- **主菜单**：覆盖在棋盘上方，显示游戏模式选择
- **PlayerUIPanel**：顶部显示 "Player Name Text"（初始为空）、"Lobby Code Text"（初始为空）、"Opponent Name Text"
- **Resign Button**：隐藏状态
- **所有游戏内UI**：隐藏状态

---

## 二、主菜单界面（MainMenuUI - MainMenu State）

### 2.1 界面布局

主菜单是一个全屏半透明深色面板（颜色 rgba(0.1, 0.1, 0.12, 0.98)），使用 VerticalLayoutGroup 垂直居中排列，内边距 40px，元素间距 10px。

### 2.2 UI元素列表（从上到下顺序）

| 序号 | 元素名称 | 类型 | 文字内容 | 颜色 | 高度 | 交互行为 |
|------|---------|------|---------|------|------|---------|
| 1 | Title | TMP文本 | "Chess" | 白色, 28号, 加粗 | 36px | 无交互，纯标题 |
| 2 | OnlineTitle | TMP文本 | "Online Game" | 白色, 24号, 加粗 | 36px | **此界面隐藏** |
| 3 | Result Text | TMP文本 | （动态） | 继承原场景设置 | 36px | 仅在有结果消息时显示 |
| 4 | Local Game Button | 按钮 | "Local Game" | rgba(0.2, 0.2, 0.26) | 48px | 点击 → 启动本地双人游戏 |
| 5 | Robot Game Button | 按钮 | "vs AI" | rgba(0.2, 0.2, 0.26) | 48px | 点击 → 弹出难度选择器 |
| 6 | Online Game Button | 按钮 | "Online Game" | rgba(0.15, 0.25, 0.35) | 48px | 点击 → 切换到在线选项界面 |
| 7 | LeaderboardButton | 按钮 | （场景预设文字） | rgba(0.2, 0.3, 0.2) | 48px | 点击 → 切换排行榜面板显示/隐藏 |
| 8 | Create Button | 按钮 | （场景预设文字） | rgba(0.2, 0.5, 0.3) | 48px | **此界面隐藏** |
| 9 | Lobby Code Input | 输入框 | （空/占位符） | 场景预设 | 40px | **此界面隐藏** |
| 10 | Join Button | 按钮 | （场景预设文字） | rgba(0.2, 0.3, 0.6) | 48px | **此界面隐藏** |
| 11 | Back Button | 按钮 | "Back" | rgba(0.4, 0.2, 0.2) | 48px | **此界面隐藏** |
| 12 | WaitingPanel | 面板 | （等待界面） | — | 120px | **此界面隐藏** |

### 2.3 各按钮点击行为详解

#### "Local Game" 按钮
- **触发方法**：`Player.StartLocalGame()`（场景中绑定的onClick事件）
- **行为**：
  1. 设置游戏模式为 `Local`
  2. 创建新的 `ChessBoard` 实例（标准开局）
  3. 同步棋盘显示
  4. 隐藏主菜单面板
  5. 显示 Resign 按钮
  6. 设置视角为白方（cameraPivot Y轴旋转0°）
  7. Player Name Text 显示 "White's Turn"
  8. 设置 MoveHistoryUI 的棋盘引用
  9. 显示游戏内UI（Moves按钮、Cmd按钮、Hint按钮、评估条）
- **预期结果**：主菜单消失，棋盘可交互，白方先手

#### "vs AI" 按钮
- **触发方法**：`Player.StartRobotGame()`（场景中绑定的onClick事件）
- **行为**：
  1. 如果 DifficultySelector 存在 → 显示难度选择面板
  2. 如果 DifficultySelector 不存在 → 直接以深度3启动AI游戏
- **预期结果**：弹出难度选择弹窗（见第四节）

#### "Online Game" 按钮
- **触发方法**：`MainMenuUI.SetState(MenuState.OnlineOptions)` → `ShowOnlineMenu()`
- **行为**：
  1. 隐藏 Title
  2. 显示 OnlineTitle（"Online Game"）
  3. 隐藏 Local Game Button、Robot Game Button、Online Game Button、LeaderboardButton
  4. 显示 Create Button、Join Button、Lobby Code Input、Back Button
  5. 隐藏 WaitingPanel
- **预期结果**：切换到在线游戏选项界面（见第三节）

#### "LeaderboardButton" 按钮
- **触发方法**：`Player.ShowLeaderboard()`（场景中绑定的onClick事件）
- **行为**：
  1. 查找场景中的 "LeaderboardPanel" GameObject
  2. 切换其 active 状态（显示↔隐藏）
- **预期结果**：排行榜面板在主菜单上方显示或隐藏

---

## 三、在线游戏选项界面（MainMenuUI - OnlineOptions State）

### 3.1 界面布局

与主菜单共用同一个面板，通过显示/隐藏子元素切换内容。

### 3.2 可见UI元素

| 序号 | 元素名称 | 类型 | 文字内容 | 颜色 | 交互行为 |
|------|---------|------|---------|------|---------|
| 1 | OnlineTitle | TMP文本 | "Online Game" | 白色, 24号, 加粗 | 无交互 |
| 2 | Create Button | 按钮 | （场景预设，如"Create Room"） | rgba(0.2, 0.5, 0.3) 绿色 | 点击 → 创建在线房间 |
| 3 | Lobby Code Input | TMP输入框 | 占位符文本 | 场景预设 | 输入房间码 |
| 4 | Join Button | 按钮 | （场景预设，如"Join"） | rgba(0.2, 0.3, 0.6) 蓝色 | 点击 → 加入房间 |
| 5 | Back Button | 按钮 | "Back" | rgba(0.4, 0.2, 0.2) 红色 | 点击 → 返回主菜单 |

### 3.3 各元素交互行为详解

#### "Create Button"（创建房间）
- **触发方法**：`Player.CreateGame()`（场景中绑定的onClick事件）
- **前置条件**：Unity Services 必须已初始化完成
- **行为流程**：
  1. 等待初始化完成（`WaitForInitialization()`）
  2. 如果未初始化 → 静默返回
  3. 调用 CloudCode `ChessCloudCode.HostGame` 创建房间
  4. 获取返回的 `LobbyCode`（房间码）
  5. 设置 `lobbyCodeText.text = LobbyCode`
  6. 调用 `mainMenuUI.ShowWaitingForOpponent(LobbyCode)` → 切换到等待界面
- **异常处理**：CloudCode调用失败 → 显示 "Create game failed. Please try again."
- **预期结果**：切换到等待对手界面，显示房间码

#### "Lobby Code Input"（房间码输入框）
- **类型**：TMP_InputField
- **结构**：InputField → TextArea (RectMask2D) → Text + Placeholder
- **功能**：用户输入要加入的房间码
- **注意**：输入框内容通过 `TMP_InputField.text` 属性读取（非 TextMeshProUGUI.text）

#### "Join Button"（加入房间）
- **触发方法**：`Player.JoinLobbyByCode()`（场景中绑定的onClick事件）
- **前置条件**：Unity Services 必须已初始化完成
- **行为流程**：
  1. 等待初始化完成
  2. 读取 Lobby Code Input 的输入值（优先使用 TMP_InputField.text）
  3. 清理输入（去除空格和零宽空格）
  4. 如果输入为空 → 显示 "Please enter a valid lobby code." 并返回
  5. 调用 CloudCode `ChessCloudCode.JoinGame`，传入 `lobbyCode`
  6. 成功 → 调用 `OnGameStart(joinGameResponse)` 直接开始游戏
- **异常处理**：加入失败 → 显示 "Join game failed. Check lobby code or wait for opponent."
- **预期结果**：加入成功后直接进入游戏

#### "Back Button"（返回）
- **触发方法**：`MainMenuUI.SetState(MenuState.MainMenu)` → `ShowMainMenu()`
- **行为**：
  1. 显示 Title（"Chess"）
  2. 隐藏 OnlineTitle
  3. 显示 Local Game Button、Robot Game Button、Online Game Button、LeaderboardButton
  4. 隐藏 Create Button、Join Button、Lobby Code Input、Back Button
  5. 如果有结果文本则显示
- **预期结果**：返回主菜单界面

---

## 四、等待对手界面（MainMenuUI - WaitingForOpponent State）

### 4.1 界面布局

与主菜单共用面板，仅显示等待相关信息。

### 4.2 可见UI元素

| 序号 | 元素名称 | 类型 | 文字内容 | 颜色 | 交互行为 |
|------|---------|------|---------|------|---------|
| 1 | OnlineTitle | TMP文本 | "Online Game" | 白色, 24号, 加粗 | 无交互 |
| 2 | WaitingPanel | 面板容器 | — | — | 包含以下子元素 |
| 2a | WaitTitle | TMP文本 | "Waiting for Opponent..." | 黄色, 20号, 加粗 | 无交互 |
| 2b | CodeLabel | TMP文本 | "Room Code:" | 灰色(0.7,0.7,0.7), 16号 | 无交互 |
| 2c | CodeValue | TMP文本 | （动态房间码，如"ABCD"） | 白色, 32号, 加粗 | 无交互 |
| 3 | Back Button | 按钮 | "Back" | rgba(0.4, 0.2, 0.2) | 点击 → 返回在线选项界面 |

### 4.3 交互行为

#### 等待流程
1. 创建房间后自动进入此界面
2. 显示6位房间码（由CloudCode生成）
3. 玩家需将房间码告知对手
4. 对手在另一客户端输入房间码点击Join
5. 服务器推送 `opponentJoined` 事件
6. **状态检查**：只有当 `MainMenuUI.IsWaitingForOpponent == true` 时才处理该事件
7. 收到有效事件 → 调用 `OnGameStart()` → 进入游戏

#### "Back Button"（返回）
- **触发方法**：`MainMenuUI.SetState(MenuState.MainMenu)` → `ShowMainMenu()`
- **行为**：返回主菜单，`IsWaitingForOpponent` 变为 false
- **注意**：返回后如果对手加入，`opponentJoined` 事件会被忽略（因为不在等待状态）

---

## 五、难度选择界面（DifficultySelector）

### 5.1 界面布局

居中弹窗面板（300×280px），半透明深色背景（raycastTarget=true，阻止点击穿透），VerticalLayoutGroup 居中排列。

### 5.2 UI元素列表

| 序号 | 元素名称 | 类型 | 文字内容 | 颜色 | 高度 | 交互行为 |
|------|---------|------|---------|------|------|---------|
| 1 | Title | TMP文本 | "Select Difficulty" | 白色, 22号, 加粗 | 36px | 无交互 |
| 2 | Btn_Easy | 按钮 | "Easy (Depth 1)" | 默认: rgba(0.2,0.2,0.25) / 选中: rgba(0.3,0.55,0.8) | 40px | 点击 → 以深度1启动AI游戏 |
| 3 | Btn_Medium | 按钮 | "Medium (Depth 3)" | 同上 | 40px | 点击 → 以深度3启动AI游戏 |
| 4 | Btn_Hard | 按钮 | "Hard (Depth 4)" | 同上 | 40px | 点击 → 以深度4启动AI游戏 |
| 5 | Btn_Master | 按钮 | "Master (Depth 5)" | 同上 | 40px | 点击 → 以深度5启动AI游戏 |
| 6 | BackBtn | 按钮 | "Back" | rgba(0.5, 0.2, 0.2) | 36px | 点击 → 关闭难度选择面板 |

### 5.3 交互行为

#### 难度按钮
- **触发方法**：`DifficultySelector.OnDifficultySelected(index)` → `Player.StartRobotGameWithDifficulty(depth)`
- **行为**：
  1. 设置游戏模式为 `Robot`
  2. 创建 `ChessBoard` 和 `ChessAI(maxDepth: depth)`
  3. 同步棋盘
  4. 隐藏主菜单和难度选择面板
  5. 显示 Resign 按钮
  6. 设置视角为白方（0°）
  7. Player Name Text 显示 "Your Turn (White)"
  8. 显示游戏内UI（Moves按钮、Cmd按钮、Hint按钮、评估条）
  9. **Chat按钮不显示**（仅Online模式显示）
- **AI难度说明**：
  - Easy (Depth 1)：极弱AI，仅看1步
  - Medium (Depth 3)：中等AI，看3步
  - Hard (Depth 4)：较强AI，看4步
  - Master (Depth 5)：最强AI，看5步，思考时间较长

#### "Back" 按钮
- **触发方法**：`DifficultySelector.Hide()`
- **行为**：隐藏难度选择面板，回到主菜单

---

## 六、游戏内界面

### 6.1 PlayerUIPanel（顶部信息栏）

位于Canvas顶部，包含以下元素：

| 元素名称 | 类型 | 显示内容 | 可见条件 |
|---------|------|---------|---------|
| Player Name Text (TMP) | TMP文本 | 当前回合提示 | 始终可见 |
| Lobby Code Text (TMP) | TMP文本 | 当前房间码 | Online模式 |
| Opponent Name Text | TMP文本 | 对手名称 | Online模式 |
| Resign Button | 按钮 | "Resign" | 游戏进行中可见 |

#### Player Name Text 显示逻辑
| 游戏模式 | 白方回合 | 黑方回合 | AI思考中 | 游戏结束 |
|---------|---------|---------|---------|---------|
| Local | "White's Turn" | "Black's Turn" | — | "Game Over" |
| Robot | "Your Turn (White)" | — | "AI Thinking..." | "Game Over" |
| Online | "Your Turn (White/Black)" | "Opponent's Turn" | — | "Game Over" |

#### Resign Button（认输按钮）
- **触发方法**：`Player.Resign()`
- **行为因模式而异**：
  - **Robot模式**：白方认输 → 显示AI胜利信息，提交分数
  - **Local模式**：当前走棋方认输 → 显示对方胜利信息，提交分数
  - **Online模式**：调用 CloudCode `ChessCloudCode.Resign` → 服务器处理认输 → 收到 BoardUpdate → 显示结果

### 6.2 游戏内功能按钮（屏幕底部/侧边）

游戏开始后，以下切换按钮显示在屏幕上：

| 按钮名称 | 位置 | 文字 | 可见条件 | 点击行为 |
|---------|------|------|---------|---------|
| MoveHistoryBtn | 右下角 | "Moves" | Local/Robot/Online | 切换走棋历史面板 |
| CmdToggleBtn | 右下角（Moves上方） | "Cmd" | Local/Robot/Online | 切换命令输入面板 |
| ChatToggleBtn | 左下角 | "Chat" | **仅Online模式** | 切换聊天面板 |
| HintBtn | 左下角 | "Hint" | Local/Robot | 显示最佳走法提示 |
| EvalBar | 左侧竖条 | 评估值 | Local/Robot | 自动更新，无交互 |

### 6.3 MoveHistoryUI（走棋历史面板）

#### 面板位置与布局
- **位置**：屏幕右侧，距右边缘10px，从底部50px到顶部10px，宽260px
- **背景色**：rgba(0.12, 0.12, 0.12, 0.95)

#### 面板内元素

| 元素 | 类型 | 内容 | 行为 |
|------|------|------|------|
| Header | 横条 | 深色背景 rgba(0.18,0.18,0.18) | — |
| Title | TMP文本 | "Move History" | 无交互 |
| CloseBtn | 按钮 | "X"（红色背景） | 点击 → 隐藏面板 |
| ScrollView | 滚动区域 | 可垂直滚动 | 支持弹性滚动 |
| Content | 内容区 | 走棋记录行 | 自动滚动到底部 |

#### 走棋记录格式
每行显示：`序号. 白方走法 黑方走法`
- 序号颜色：灰色(0.55, 0.55, 0.55)
- 走法颜色：浅黄色(0.95, 0.95, 0.6)
- 偶数行背景：rgba(0.14, 0.14, 0.14)
- 奇数行背景：rgba(0.2, 0.2, 0.2)
- 无走棋时显示："No moves yet"

#### 切换按钮
- **位置**：右下角，距右边缘10px，距底部10px，宽90px，高34px
- **文字**："Moves"，14号加粗白色
- **点击**：Toggle 显示/隐藏面板

### 6.4 CommandInputUI（命令输入面板）

#### 面板位置与布局
- **位置**：屏幕左下方，宽38%屏幕，高50%屏幕，距左10px，距底50px
- **背景色**：rgba(0.12, 0.12, 0.12, 0.95)

#### 面板内元素

| 元素 | 类型 | 内容 | 行为 |
|------|------|------|------|
| Header | 横条 | 深色背景 | — |
| Title | TMP文本 | "Command Input" | 无交互 |
| CloseBtn | 按钮 | "X"（红色背景） | 点击 → 隐藏面板 |
| OutputScroll | 滚动区域 | 命令输出 | 可垂直滚动 |
| Content/OutputText | TMP文本 | 命令历史输出 | 自动滚动到底部 |
| InputRow | 横条 | 输入行 | — |
| InputField | TMP输入框 | 用户输入 | 占位符："Enter move or command..." |
| SendBtn | 按钮 | "Run"（蓝色背景） | 点击 → 执行命令 |

#### 支持的命令列表

| 命令 | 格式 | 说明 | 示例 |
|------|------|------|------|
| 走棋（坐标） | `e2e4` | 从e2到e4 | `e7e8q`（升变） |
| 走棋（SAN） | `Nf3` | 标准代数记谱法 | `O-O`（王翼易位）`O-O-O`（后翼易位） |
| help | `help` | 显示帮助 | — |
| board | `board` | 显示ASCII棋盘 | — |
| fen | `fen` | 显示当前FEN | — |
| pgn | `pgn` | 显示PGN记录 | — |
| undo | `undo` | 悔棋 | Robot模式撤销两步 |
| load fen | `load fen <FEN>` | 从FEN恢复棋盘 | `load fen rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR` |
| load pgn | `load pgn <PGN>` | 从PGN恢复棋盘 | — |
| clear | `clear` | 清屏 | — |

#### 输出颜色编码
- 普通输出：浅灰色(0.8, 0.8, 0.8)
- 成功信息：绿色(0.4, 0.9, 0.4)
- 错误信息：红色(0.9, 0.3, 0.3)
- 信息提示：蓝色(0.5, 0.7, 0.9)

#### 切换按钮
- **位置**：右下角（Moves上方），距右边缘10px，距底部50px，宽90px，高34px
- **文字**："Cmd"，14号加粗白色

### 6.5 ChatUI（聊天面板）— 仅Online模式

#### 面板位置与布局
- **位置**：与CommandInputUI相同位置（左下方）
- **背景色**：rgba(0.12, 0.12, 0.12, 0.95)

#### 面板内元素

| 元素 | 类型 | 内容 | 行为 |
|------|------|------|------|
| Header | 横条 | 深色背景 | — |
| Title | TMP文本 | "Chat" | 无交互 |
| CloseBtn | 按钮 | "X"（红色背景） | 点击 → 隐藏面板 |
| MsgScroll | 滚动区域 | 聊天消息 | 可垂直滚动 |
| InputRow | 横条 | 输入行 | — |
| InputField | TMP输入框 | 用户输入 | 占位符："Type a message..." |
| SendBtn | 按钮 | "Send"（蓝色背景） | 点击 → 发送消息 |

#### 消息显示
- 自己的消息：蓝色背景 rgba(0.2, 0.4, 0.7, 0.5)，格式：**You:** 消息内容
- 对手消息：灰色背景 rgba(0.3, 0.3, 0.3, 0.5)，格式：**对手名:** 消息内容
- 发送方式：调用 CloudCode `chess.SendMessage`

#### 切换按钮
- **位置**：左下角，距左120px，距底10px，宽100px，高34px
- **文字**："Chat"，14号加粗白色
- **仅Online模式可见**

### 6.6 EvaluationBar（评估条）— 仅Local/Robot模式

#### 位置与布局
- **位置**：屏幕最左侧，距左4px，从10%到90%高度，宽20px
- **结构**：黑色背景 + 白色填充条 + 评估数值文本

#### 显示逻辑
- 白色填充比例 = clamp(0.5 + eval × 0.05, 0.05, 0.95)
- eval = AI评估值 / 100（单位：兵值）
- 正值显示如 "+1.5"（白方优势），负值显示如 "-0.8"（黑方优势）
- 文字颜色：白方优势时黑色文字，黑方优势时白色文字
- **自动更新**：每帧通过 `Update()` 重新计算

### 6.7 HintSystem（提示系统）— 仅Local/Robot模式

#### 提示按钮
- **位置**：左下角，距左10px，距底10px，宽100px，高34px
- **文字**："Hint"，14号加粗白色
- **背景色**：rgba(0.25, 0.25, 0.3, 0.9)

#### 提示行为
1. 点击 Hint 按钮
2. 使用 ChessAI(maxDepth:3) 计算当前最佳走法
3. 在棋盘上最佳走法的起始位置显示绿色半透明高亮方块
4. 高亮方块3秒后自动消失
5. 高亮方块是3D Quad，位于棋盘上方0.02单位

---

## 七、棋盘交互

### 7.1 棋盘坐标系
- X轴：0-7 对应 a-h 列
- Z轴：0-7 对应 1-8 行
- Board 位于 BoardPivot 下，localPosition (-3.5, 0, -3.5)

### 7.2 棋子选择与移动

#### 选择棋子
- **触发**：鼠标点击（PlayerInteract 输入事件）
- **条件**：
  - 游戏已开始且未结束
  - AI未在思考（Robot模式）
  - Online模式必须有有效session
  - 点击的棋子属于当前走棋方
- **视觉反馈**：选中棋子变为蓝色 rgba(84, 84, 255)
- **取消选择**：点击空白处或无效位置

#### 移动棋子
- **触发**：选中棋子后点击目标位置
- **条件**：目标位置为棋盘空格或对方棋子
- **移动动画**：MoveAnimator 处理平滑移动动画
- **走棋后状态更新**：
  - 同步棋盘
  - 刷新走棋历史
  - 更新评估条
  - 检查游戏结束条件

### 7.3 视角切换
- **Local模式**：每步走完后自动切换视角（白方0°/黑方180°）
- **Robot模式**：始终白方视角（0°）
- **Online模式**：根据分配的颜色固定视角

---

## 八、游戏结束

### 8.1 结束条件

| 结束类型 | Local模式提示 | Robot模式提示 |
|---------|-------------|-------------|
| Checkmate | "Checkmate - White/Black Wins!" | "Checkmate - You Win!/AI Wins!" |
| Stalemate | "Stalemate - Draw" | "Stalemate - Draw" |
| Resigned | "White/Black Wins by Resignation" | "You Win/AI Wins by Resignation" |
| Timeout | "White/Black Wins on Time" | "You Win/AI Wins on Time" |
| Insufficient Material | "Draw - Insufficient Material" | 同左 |
| Fifty Move Rule | "Draw - Fifty Move Rule" | 同左 |
| Repetition | "Draw - Repetition" | 同左 |
| Draw Declared | "Draw" | 同左 |

### 8.2 游戏结束后的界面变化
1. 隐藏 Resign 按钮
2. Player Name Text 显示 "Game Over"
3. 隐藏所有游戏内UI面板和按钮
4. 显示主菜单，Result Text 显示结果消息
5. 自动提交分数到排行榜

### 8.3 分数计算

#### Robot模式
| 结果 | 分数 |
|------|------|
| 白方将杀胜 | 100 + max(0, 50 - 半步数) |
| 和棋 | 20 |
| 其他 | 0 |

#### Local模式
| 结果 | 分数 |
|------|------|
| 将杀 | 80 |
| 认输 | 60 |
| 和棋 | 15 |

#### Online模式
| 结果 | 分数 |
|------|------|
| 将杀胜 | 120 |
| 认输/超时胜 | 100 |
| 其他胜利 | 80 |
| 和棋 | 15 |

---

## 九、排行榜界面（LeaderboardUI）

### 9.1 界面元素

排行榜面板是场景预设的UI，包含以下元素：

| 元素名称 | 类型 | 功能 |
|---------|------|------|
| TitleText | Text | 标题 |
| ModeDropdown | TMP_Dropdown | 模式筛选（全部/人机对战/本地双人/在线对战） |
| PlayerNameInput | TMP_InputField | 玩家名输入 |
| EntryScrollView | ScrollRect | 排行榜条目滚动列表 |
| RefreshButton | Button | 刷新数据 |
| CloseButton | Button | 关闭排行榜 |
| LoadingIndicator | Text | 加载中提示 |
| MyRankText | Text | "我的排名：第 X 名" |

### 9.2 交互行为

#### ModeDropdown（模式筛选）
- 选项：全部 / 人机对战 / 本地双人 / 在线对战
- 切换选项 → 自动刷新排行榜数据
- "全部"模式：合并所有模式数据按分数排序

#### PlayerNameInput（玩家名输入）
- 默认值："Player"
- 编辑完成后（onEndEdit）→ 更新 Player 的排行榜玩家名
- 当前玩家的条目会高亮显示

#### RefreshButton（刷新）
- 点击 → 重新从服务器获取排行榜数据
- 加载中显示 LoadingIndicator

#### CloseButton（关闭）
- 点击 → 隐藏排行榜面板

#### Tab键快捷键
- 按Tab键 → 切换排行榜显示/隐藏

#### 排行榜条目
- 前三名特殊颜色：🥇金色、🥈银色、🥉铜色
- 当前玩家条目：浅黄色高亮
- 每条显示：排名、玩家名、分数、模式、日期

---

## 十、在线游戏完整流程

### 10.1 创建房间流程

```
主菜单 → 点击"Online Game" → 在线选项界面 → 点击"Create Room"
    ↓
等待Unity Services初始化
    ↓
调用CloudCode HostGame → 获取LobbyCode
    ↓
切换到等待对手界面（显示房间码）
    ↓
将房间码告知对手（通过其他渠道）
    ↓
等待 opponentJoined 事件（状态检查：IsWaitingForOpponent == true）
    ↓
收到事件 → OnGameStart() → 进入游戏
```

### 10.2 加入房间流程

```
主菜单 → 点击"Online Game" → 在线选项界面 → 在输入框输入房间码 → 点击"Join"
    ↓
等待Unity Services初始化
    ↓
读取并验证房间码（非空检查）
    ↓
调用CloudCode JoinGame（传入lobbyCode）
    ↓
成功 → OnGameStart() → 直接进入游戏
失败 → 显示错误信息
```

### 10.3 在线游戏中的消息订阅

| 事件类型 | 触发条件 | 处理方法 |
|---------|---------|---------|
| opponentJoined | 对手加入房间 | OnGameStart()（需IsWaitingForOpponent==true） |
| boardUpdated | 对手走棋 | OnBoardUpdate() → 同步棋盘 |

---

## 十一、完整界面状态转换图

```
                    ┌─────────────┐
                    │  游戏启动    │
                    └──────┬──────┘
                           ↓
                    ┌─────────────┐
              ┌────→│  主菜单      │←────┐
              │     │ (MainMenu)  │     │
              │     └──────┬──────┘     │
              │            │            │
              │   ┌────────┼────────┐   │
              │   ↓        ↓        ↓   │
              │ Local    vs AI    Online │
              │ Game     Game     Game   │
              │   │       │        │    │
              │   │   ┌───┴───┐    ↓    │
              │   │   │难度   │ ┌──────┐│
              │   │   │选择   │ │在线  ││
              │   │   └───┬───┘ │选项  ││
              │   │       │     └──┬───┘│
              │   │       │    ┌───┤    │
              │   │       │    ↓   ↓    │
              │   │       │ Create Join │
              │   │       │    │   │    │
              │   │       │    ↓   │    │
              │   │       │ ┌──────┐│    │
              │   │       │ │等待  ││    │
              │   │       │ │对手  ││    │
              │   │       │ └──┬───┘│    │
              │   │       │    ↓    │    │
              │   │       │ 对手加入 │    │
              │   │       │    │    │    │
              ↓   ↓       ↓    ↓    ↓    ↓
            ┌─────────────────────────────┐
            │        游戏进行中            │
            │  (PlayerUIPanel + 功能按钮)  │
            └──────────────┬──────────────┘
                           ↓
                    ┌─────────────┐
                    │  游戏结束    │
                    │ (显示结果)   │
                    └──────┬──────┘
                           ↓
                    ┌─────────────┐
                    │  返回主菜单  │
                    │ (带结果文本) │
                    └─────────────┘
```

---

## 十二、功能验收检查清单

### 主菜单
- [ ] 启动后显示 "Chess" 标题
- [ ] 显示4个按钮：Local Game、vs AI、Online Game、Leaderboard
- [ ] 点击 Local Game → 直接进入游戏
- [ ] 点击 vs AI → 弹出难度选择
- [ ] 点击 Online Game → 切换到在线选项
- [ ] 点击 Leaderboard → 切换排行榜显示

### 在线选项
- [ ] 显示 "Online Game" 标题
- [ ] 显示 Create Room 按钮、Lobby Code Input、Join 按钮、Back 按钮
- [ ] 隐藏主菜单按钮
- [ ] 点击 Back → 返回主菜单

### 创建房间
- [ ] 点击 Create Room → 显示等待界面
- [ ] 等待界面显示 "Waiting for Opponent..."
- [ ] 等待界面显示 "Room Code:" 标签
- [ ] 等待界面显示6位房间码
- [ ] 等待界面显示 Back 按钮
- [ ] 对手加入后自动进入游戏

### 加入房间
- [ ] Lobby Code Input 可以输入文字
- [ ] 输入为空时点击 Join → 显示 "Please enter a valid lobby code."
- [ ] 输入有效房间码点击 Join → 加入游戏
- [ ] 输入无效房间码 → 显示错误信息

### 难度选择
- [ ] 显示4个难度选项：Easy/Medium/Hard/Master
- [ ] 点击难度 → 以对应深度启动AI游戏
- [ ] 点击 Back → 关闭弹窗回到主菜单
- [ ] 弹窗阻止点击穿透到主菜单

### 游戏内 - 基本交互
- [ ] 可以点击选中己方棋子（变蓝色）
- [ ] 可以点击目标位置移动棋子
- [ ] 走棋后棋盘正确更新
- [ ] 走棋后Player Name Text更新回合提示
- [ ] Robot模式AI自动走棋，显示 "AI Thinking..."
- [ ] Local模式每步自动切换视角

### 游戏内 - MoveHistory
- [ ] "Moves" 按钮在游戏内可见
- [ ] 点击 Moves → 显示走棋历史面板
- [ ] 面板显示正确的走棋记录（序号. 白方 黑方）
- [ ] 点击 X → 关闭面板
- [ ] 面板可滚动

### 游戏内 - CommandInput
- [ ] "Cmd" 按钮在游戏内可见
- [ ] 点击 Cmd → 显示命令输入面板
- [ ] 输入框可输入文字
- [ ] 按 Enter 或点击 Run → 执行命令
- [ ] help 命令显示帮助
- [ ] e2e4 格式走棋正常
- [ ] Nf3 格式走棋正常
- [ ] undo 命令悔棋正常
- [ ] fen 命令显示当前FEN
- [ ] load fen 命令恢复棋盘
- [ ] 错误命令显示红色错误信息

### 游戏内 - Chat（仅Online）
- [ ] "Chat" 按钮仅Online模式可见
- [ ] 点击 Chat → 显示聊天面板
- [ ] 输入消息点击 Send → 发送
- [ ] 自己的消息蓝色背景
- [ ] 对手消息灰色背景

### 游戏内 - Hint（仅Local/Robot）
- [ ] "Hint" 按钮仅Local/Robot模式可见
- [ ] 点击 Hint → 棋盘上显示绿色高亮
- [ ] 高亮3秒后自动消失

### 游戏内 - EvaluationBar（仅Local/Robot）
- [ ] 评估条仅Local/Robot模式可见
- [ ] 白色填充反映当前局面评估
- [ ] 数值文本正确显示评估值

### 游戏结束
- [ ] 将杀 → 显示正确结果
- [ ] 和棋 → 显示正确结果
- [ ] 认输 → 显示正确结果
- [ ] Resign 按钮隐藏
- [ ] 返回主菜单并显示结果文本
- [ ] 分数自动提交到排行榜

### 排行榜
- [ ] 显示排行榜条目
- [ ] 模式筛选下拉框工作正常
- [ ] 玩家名输入框工作正常
- [ ] 刷新按钮工作正常
- [ ] 关闭按钮工作正常
- [ ] Tab键切换显示/隐藏
- [ ] 前三名特殊颜色
- [ ] 当前玩家高亮
