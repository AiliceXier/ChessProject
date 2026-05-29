# 排行榜功能优化计划

## 现状分析

### 当前问题
1. **排行榜只显示3个字段**：排名、用户名、分数 — 没有游戏模式、没有日期
2. **UI只有刷新和关闭按钮** — 无法切换模式、无法输入玩家名、无加载状态
3. **游戏结束时没有提交分数** — `Player.cs` 中从未调用 `LeaderboardAPI.SubmitScore`
4. **服务端排行榜接口不返回 `game_mode`** — 客户端无法知道每条记录属于哪个模式
5. **没有获取可用游戏模式的接口** — 客户端无法动态获取模式列表

### 现有游戏模式（来自 Player.cs）
- `Online` — 在线对战
- `Local` — 本地双人
- `Robot` — 人机对战

---

## 实施步骤

### 第一阶段：服务端优化

#### 1.1 排行榜接口返回 game_mode
- 修改 `GET /leaderboard` 的 SQL 查询，增加 `game_mode` 字段
- 修改返回数据结构，每条记录包含 `game_mode`

#### 1.2 新增获取游戏模式列表接口
- 新增 `GET /modes` 接口，返回数据库中已有的所有 game_mode 列表
- 用于客户端动态生成模式筛选标签

#### 1.3 新增获取所有模式排行榜的聚合接口
- 新增 `GET /leaderboard/all` 接口，一次性返回所有模式的排行榜数据
- 减少客户端多次请求的开销

---

### 第二阶段：Unity 数据模型更新

#### 2.1 更新 LeaderboardResponse.cs
- `ScoreEntry` 增加 `game_mode` 字段
- 新增 `ModesResponse` 类（用于 `/modes` 接口响应）
- 新增 `AllModesLeaderboardResponse` 类（用于 `/leaderboard/all` 接口响应）

---

### 第三阶段：Unity API 层更新

#### 3.1 更新 LeaderboardAPI.cs
- 新增 `GetModes()` 方法 — 获取可用游戏模式列表
- 新增 `GetAllModesLeaderboard()` 方法 — 获取所有模式排行榜

---

### 第四阶段：Unity UI 控制器重写

#### 4.1 重构 LeaderboardUI.cs
新增功能：
- **游戏模式标签页/下拉框**：切换不同模式的排行榜
- **玩家名输入框**：允许输入/修改当前玩家名
- **加载状态指示器**：数据加载时显示 Loading
- **当前玩家排名显示**：在面板底部显示"我的排名：第X名"
- **日期显示**：每条记录显示创建时间
- **模式显示**：每条记录显示所属模式（在"全部"视图下）
- **自动提交分数**：游戏结束时自动提交

具体改动：
- 增加 Inspector 字段：`modeDropdown`、`playerNameInput`、`loadingIndicator`、`myRankText`
- 增加 `currentGameMode` 运行时状态追踪
- 增加模式切换事件处理
- 增加玩家名变更事件处理
- 增加 `SubmitCurrentScore()` 公开方法供 Player.cs 调用
- 优化 `OnDataLoaded` 方法，支持显示 game_mode 和 created_at

---

### 第五阶段：游戏逻辑集成

#### 5.1 修改 Player.cs
- 在 `StartRobotGame()` 和 `StartLocalGame()` 中记录当前游戏模式字符串
- 在游戏结束时（`MakeLocalMove`、`DoRobotMoveAsync`、`Resign` 等处），调用排行榜分数提交
- 分数计算逻辑：根据游戏结果和步数计算分数
  - 人机模式胜利：基础分 100 + 残局奖励
  - 人机模式失败/平局：不提交或提交较低分数
  - 本地模式：胜者得分

---

### 第六阶段：Unity 编辑器操作（需手动完成）

#### 6.1 修改 EntryTemplate 预制体
1. 在 Unity 中打开 `Assets/Prefabs/EntryTemplate.prefab`
2. 在 EntryTemplate 根对象下新增子对象 `ModeText`（Text 组件）
3. 在 EntryTemplate 根对象下新增子对象 `DateText`（Text 组件）
4. 调整 LayoutElement 和 HorizontalLayoutGroup，使5列均匀分布：
   - RankText（排名）：宽度约 40
   - NameText（玩家名）：宽度约 100
   - ScoreText（分数）：宽度约 60
   - ModeText（模式）：宽度约 80
   - DateText（日期）：宽度约 100

#### 6.2 修改 LeaderboardPanel 场景对象
1. 打开 `ChessDemo` 场景
2. 在 LeaderboardPanel 下新增以下 UI 元素：
   - **ModeDropdown**（Dropdown 组件）：放在面板顶部标题栏区域
     - 默认选项："全部"、"人机对战"、"本地双人"、"在线对战"
   - **PlayerNameInput**（InputField 组件）：放在面板顶部
     - 默认文本："Player"
     - Placeholder 文本："输入玩家名"
   - **LoadingIndicator**（GameObject + Text）：放在列表中央
     - 默认文本："加载中..."
     - 默认隐藏
   - **MyRankText**（Text 组件）：放在面板底部
     - 默认文本："我的排名：--"
3. 在 LeaderboardUI 组件的 Inspector 中，将新增的 UI 元素拖拽到对应字段：
   - `modeDropdown` ← ModeDropdown
   - `playerNameInput` ← PlayerNameInput
   - `loadingIndicator` ← LoadingIndicator
   - `myRankText` ← MyRankText

#### 6.3 美化建议（可选）
- 给模式标签页添加背景色区分
- 前三名使用金银铜色高亮
- 给当前玩家行添加边框效果
- 排行榜面板添加半透明背景

---

## 文件修改清单

| 文件 | 修改类型 | 说明 |
|------|----------|------|
| `leaderboard-server/server.js` | 修改 | 排行榜返回 game_mode，新增 /modes 和 /leaderboard/all 接口 |
| `Chess/Assets/Leaderboard/LeaderboardResponse.cs` | 修改 | 增加 game_mode 字段，新增响应类 |
| `Chess/Assets/Leaderboard/LeaderboardAPI.cs` | 修改 | 新增 GetModes、GetAllModesLeaderboard 方法 |
| `Chess/Assets/Leaderboard/LeaderboardUI.cs` | 重写 | 增加模式切换、玩家名输入、加载状态、排名显示 |
| `Chess/Assets/Player.cs` | 修改 | 游戏结束时提交分数到排行榜 |
| `Chess/Assets/Prefabs/EntryTemplate.prefab` | 需手动修改 | 增加 ModeText、DateText 子对象 |
| `Chess/Assets/Scenes/ChessDemo.unity` | 需手动修改 | 增加 ModeDropdown、PlayerNameInput 等UI元素 |

---

## 需要用户在 Unity 编辑器中手动操作的步骤

> 以下操作无法通过代码修改完成，需要在 Unity 编辑器中手动操作：

### 步骤1：修改 EntryTemplate 预制体
1. 在 Unity Project 窗口，导航到 `Assets/Prefabs/EntryTemplate`
2. 双击打开预制体编辑模式
3. 右键 EntryTemplate 根对象 → UI → Text，重命名为 `ModeText`
4. 右键 EntryTemplate 根对象 → UI → Text，重命名为 `DateText`
5. 选中 EntryTemplate 根对象的 HorizontalLayoutGroup 组件
6. 调整各子对象的 LayoutElement preferred width：
   - RankText: 40
   - NameText: 100
   - ScoreText: 60
   - ModeText: 80
   - DateText: 100
7. 保存预制体（Ctrl+S）

### 步骤2：修改 LeaderboardPanel 场景对象
1. 打开 `ChessDemo` 场景
2. 在 Hierarchy 中展开 Canvas → LeaderboardPanel
3. 在 LeaderboardPanel 下右键 → UI → Dropdown，重命名为 `ModeDropdown`
   - 调整位置到面板顶部标题栏右侧
   - 在 Dropdown 组件的 Options 中设置：
     - Option 1: "全部"
     - Option 2: "人机对战"
     - Option 3: "本地双人"
     - Option 4: "在线对战"
4. 在 LeaderboardPanel 下右键 → UI → InputField，重命名为 `PlayerNameInput`
   - 调整位置到面板顶部标题栏左侧
   - 设置 Placeholder 文本为 "输入玩家名"
   - 设置默认 Text 为 "Player"
5. 在 LeaderboardPanel 下右键 → UI → Text，重命名为 `LoadingIndicator`
   - 调整位置到列表中央
   - 设置文本为 "加载中..."
   - 取消勾选该对象（默认隐藏）
6. 在 LeaderboardPanel 下右键 → UI → Text，重命名为 `MyRankText`
   - 调整位置到面板底部
   - 设置文本为 "我的排名：--"
   - 字号适当调大
7. 选中 LeaderboardPanel 的 LeaderboardUI 组件，在 Inspector 中：
   - 将 ModeDropdown 拖到 `modeDropdown` 字段
   - 将 PlayerNameInput 拖到 `playerNameInput` 字段
   - 将 LoadingIndicator 拖到 `loadingIndicator` 字段
   - 将 MyRankText 拖到 `myRankText` 字段
8. 保存场景（Ctrl+S）

### 步骤3：重新部署服务端
1. 将修改后的 `server.js` 上传到服务器
2. 使用 PM2 重启服务：`pm2 restart leaderboard-api`
3. 验证新接口：`curl http://121.36.101.82:3000/modes`
