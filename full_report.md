# 国际象棋对战平台课程设计报告

---

## 封面

信息与通信工程学院  
2024-2025-2 软件设计思想与方法II  
课程设计报告

项目名称：国际象棋对战平台  
完成时间：2025年  月  日  
授课教师：段景山  

小组成员：

| 姓名 | 学号 | 分工 | 贡献度 |
|------|------|------|--------|
| （用户填写） | | | |
| （用户填写） | | | |

---

## 目录

一、需求说明书  
&emsp;1.1 引言  
&emsp;&emsp;1.1.1 编写目的  
&emsp;&emsp;1.1.2 项目背景  
&emsp;&emsp;1.1.3 术语定义  
&emsp;1.2 任务概述  
&emsp;&emsp;1.2.1 目标  
&emsp;&emsp;1.2.2 用户的特点  
&emsp;1.3 功能需求  
&emsp;1.4 性能需求  

二、可行性分析  
&emsp;2.1 编写目的  
&emsp;2.2 技术可行性  
&emsp;2.3 经济或使用等可行性  
&emsp;2.4 社会因素可行性  

三、软件设计说明  
&emsp;3.1 编写目的  
&emsp;3.2 概要设计  
&emsp;3.3 详细设计  
&emsp;3.4 交互/界面设计  

四、测试计划  
&emsp;4.1 编写目的  
&emsp;4.2 计划安排  
&emsp;4.3 单元测试设计  
&emsp;4.4 总体测试设计  

五、用户操作手册  
&emsp;5.1 编写目的  
&emsp;5.2 运行环境  
&emsp;5.3 典型用例  
&emsp;5.4 使用条件  

六、体会与收获  

七、附录  
&emsp;附录A：AI辅助开发实践报告  
&emsp;附录B：程序代码简要说明  

---

## 一、需求说明书

### 1.1 引言

#### 1.1.1 编写目的

本文档为国际象棋对战平台项目的需求说明书，旨在明确项目的功能需求、性能需求和使用场景，为后续的软件设计、编码实现和测试验收提供依据。本文档面向项目开发人员、测试人员以及课程评审教师。

#### 1.1.2 项目背景

国际象棋是一项历史悠久、广泛流行的策略性棋类游戏，在全球拥有数以亿计的爱好者。传统的国际象棋对弈需要两人面对面使用实体棋盘进行，受限于时间和空间。

随着计算机技术和互联网的发展，电子国际象棋逐渐普及。目前市面上已有多种国际象棋软件，如 Chess.com、lichess.org、专业对弈软件Fritz等，它们各有特点。Chess.com和lichess提供了完善的在线对战平台和社区功能，但需要联网使用；Fritz等专业软件功能强大但价格昂贵且不跨平台。

基于以上背景，本项目旨在开发一款基于Unity引擎的国际象棋对战平台，支持本地双人对战、人机对战和在线对战三种模式，同时提供排行榜、棋谱记录、动画效果等扩展功能，力求在功能完整性和用户体验上取得平衡。

#### 1.1.3 术语定义

| 术语 | 说明 |
|------|------|
| FEN | Forsyth-Edwards Notation，棋盘状态的字符串表示法 |
| PGN | Portable Game Notation，棋谱的标准文本表示格式 |
| MinMax | 极小化极大算法，博弈树搜索的基本算法 |
| UGS | Unity Gaming Services，Unity游戏云服务平台 |
| MCP | Model Context Protocol，模型上下文协议 |
| ECS | Elastic Cloud Server，弹性云服务器 |

### 1.2 任务概述

#### 1.2.1 目标

开发一款功能完整的国际象棋对战平台，具体目标包括：

（1）基础功能：实现完整的国际象棋规则，包括所有棋子的合法走法、吃子、王车易位、兵升变、过路兵等特殊规则；能够正确判断将军、将杀、逼和等局面。

（2）双人对战：支持本地双人在同一台电脑上轮流走棋，每步自动切换视角。

（3）人机对战：实现具有不同难度等级（4级）的AI对手，满足从初学者到进阶玩家的需求。

（4）在线对战：通过网络实现两台设备之间的实时对弈，包括创建房间、加入房间、走棋同步等功能。

（5）排行榜：记录玩家的游戏成绩，支持多模式筛选和排名展示。

（6）用户体验：提供3D图形界面、走棋动画、音效、评估条、走棋提示等人性化功能。

#### 1.2.2 用户的特点

本软件的主要用户为在校大学生及国际象棋爱好者，具备基本的计算机操作能力。用户群体可分为以下几类：

- 初学者：不了解国际象棋规则或刚刚入门，需要AI低难度进行练习
- 普通爱好者：有一定基础，希望通过本地对战或在线对战与人对弈
- 进阶玩家：希望挑战高难度AI或通过排行榜与其他玩家竞争

### 1.3 功能需求

本系统应满足以下功能需求：

（1）棋盘绘制与交互：使用3D棋盘渲染，用户可通过鼠标点击选择棋子并移动，选中棋子高亮显示。

（2）走法校验：系统能够验证每一步走法的合法性，包括基本走法和特殊走法（王车易位、升变、过路兵），对非法走法给出提示。

（3）双人对战：支持黑白双方在同一设备上交替走棋，每步走完后自动切换视角。

（4）人机对战：提供4个难度等级（Easy/Medium/Hard/Master）的AI对手，AI能够自动响应并走棋。

（5）在线对战：支持创建游戏房间、加入已有房间；通过网络同步双方走棋；支持断线重连。

（6）棋谱记录：自动记录每一步走法，支持FEN/PGN格式的保存和加载，可以从保存的局面恢复对局。

（7）排行榜：游戏结束后自动提交分数，支持查看全服排名，支持按游戏模式筛选。

（8）命令输入：支持通过命令行输入坐标走法（如e2e4）和代数记谱法走法（如Nf3），以及多种辅助命令（如undo悔棋、fen查看局面）。

（9）辅助功能：走棋动画、音效（背景音乐、走棋音效、胜负音效）、局面评估条、最佳走法提示、将军视觉提示。

### 1.4 性能需求

| 性能指标 | 要求 |
|---------|------|
| AI走棋响应时间（深度3） | 平均≤5秒 |
| AI走棋响应时间（深度5） | 平均≤15秒 |
| 在线对战消息延迟 | ≤500ms |
| 排行榜查询响应时间 | ≤2秒 |
| 游戏启动时间 | ≤10秒 |
| 帧率 | ≥30 FPS |

---

## 二、可行性分析

### 2.1 编写目的

本节从技术、经济和社会因素三个角度分析本项目的可行性，评估项目实施的风险和可行性。

### 2.2 技术可行性

（1）开发语言与框架：客户端采用Unity引擎（C#语言），Unity是一款成熟的跨平台游戏引擎，拥有完善的2D/3D渲染、物理系统、UI系统和输入管理功能。服务端使用C# Cloud Code（云函数）和Node.js。

（2）国际象棋规则：Gera Chess Library是一个纯C#的国际象棋库，提供完整的走法生成、合法性校验和局面评估功能，可在Unity客户端和Cloud Code服务端复用。

（3）在线对战：Unity Gaming Services（UGS）提供了Lobby（大厅）、Cloud Code（云代码）、Push（消息推送）等开箱即用的云服务，无需自建服务器。

（4）AI算法：MinMax搜索算法配合Piece-Square Table评估表是棋类AI的经典实现方案，代码成熟、可离线运行。

（5）服务器部署：华为云ECS提供弹性云服务器，支持Ubuntu/CentOS等Linux发行版，可用于部署排行榜API和聊天WebSocket服务。

综上，项目的技术路线成熟、可行。

### 2.3 经济或使用等可行性

（1）开发成本：Unity Personal对于个人开发者免费；Unity Gaming Services提供免费层额度；Gera Chess Library为开源库（MIT协议）。项目开发阶段零成本。

（2）部署成本：华为云ECS t6.medium.2（1vCPUs/2GiB）低配实例月费约50元，可满足课设需求。

（3）使用成本：客户端为Windows桌面应用，用户无需额外付费。仅需下载运行即可使用，门槛低。

（4）维护成本：Unity项目结构清晰，Cloud Code托管于UGS无需运维，排行榜API通过PM2管理自动重启。

### 2.4 社会因素可行性

（1）法律法规方面：本项目使用的第三方资源（Unity模型、Chess库）均为合法授权，不存在侵权风险。

（2）使用习惯方面：3D国际象棋的操作方式符合大多数用户的直觉认知；提供键盘命令和鼠标点击两种操作方式，满足不同用户偏好。

（3）教育意义方面：本项目的开发过程覆盖了从需求分析、设计、编码到测试的完整软件工程流程，适合作为课程设计项目。

---

## 三、软件设计说明

### 3.1 编写目的

本节详细介绍国际象棋对战平台的软件架构设计，包括概要设计（总体架构、模块划分）、详细设计（核心数据结构、关键接口）和交互设计。

### 3.2 概要设计

#### 3.2.1 总体架构

系统采用客户端-服务端混合架构，如下图所示：

```
┌──────────────────────────────────────┐
│          Unity 客户端 (C#)             │
│  ┌────────┐ ┌────────┐ ┌──────────┐   │
│  │Player  │ │  UI    │ │ChessBoard│   │
│  │主控模块 │ │ 模块   │ │ 走法引擎  │   │
│  └───┬────┘ └────────┘ └──────────┘   │
│      │                                 │
│  ┌───┴────┐ ┌────────┐ ┌──────────┐   │
│  │ChessAI │ │Animation│ │Leaderboard│   │
│  │AI引擎  │ │动画模块  │ │排行榜客户端│   │
│  └────────┘ └────────┘ └──────────┘   │
└──────────┬───────────────────────────┘
           │ HTTP / Push / WebSocket
           ▼
┌──────────────────────────────────────┐
│       UGS Cloud Code (C#)             │
│  HostGame / JoinGame / MakeMove       │
│  Resign / SendMessage                 │
│  (走法验证 + 状态管理)                 │
└──────────┬───────────────────────────┘
           │
           ▼
┌──────────────────────────────────────┐
│  华为云 ECS (Node.js)                  │
│  排行榜API (Express + SQLite)         │
│  聊天WebSocket (ws)                   │
└──────────────────────────────────────┘
```

#### 3.2.2 模块划分

| 模块名称 | 功能说明 | 技术实现 |
|---------|---------|---------|
| Player主控模块 | 游戏初始化、用户输入处理、AI走棋调度、在线对战管理 | C# / Unity MonoBehaviour |
| ChessBoard走法引擎 | FEN解析、走法生成与验证、局面评估 | Gera Chess Library |
| ChessAI引擎 | MinMax搜索、Claude API调用、混合路由 | C# / HTTP Client |
| UI模块 | 主菜单、走棋历史、命令输入、评估条、提示、聊天、难度选择 | Unity UI / TMP |
| 动画模块 | 走棋动画、吃子动画、游戏结束动画 | Unity Animator / Coroutine |
| 音效模块 | 背景音乐、走棋音效、胜负音效 | Unity AudioSource |
| Cloud Code服务端 | 在线对战的房间管理、走法验证、消息推送 | C# / UGS Cloud Code |
| 排行榜服务 | 分数存储、排名查询、玩家改名 | Node.js / Express / SQLite |
| 聊天服务 | 在线玩家实时聊天 | Node.js / WebSocket |

### 3.3 详细设计

#### 3.3.1 核心数据结构

**FEN字符串**：使用FEN（Forsyth-Edwards Notation）表示棋盘状态。FEN包含6个字段，以空格分隔。例如标准开局为 `rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1`。

| 字段 | 含义 | 示例值 |
|------|------|--------|
| 棋子布局 | 8行棋子的FEN编码（大写白棋，小写黑棋） | rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR |
| 走棋方 | w=白方走，b=黑方走 | w |
| 易位权限 | KQkq表示王翼后翼易位，-表示无易位权限 | KQkq |
| 过路兵 | 过路兵的目标格（-表示无） | - |
| 半步行棋数 | 距离上次吃子或兵移动的步数 | 0 |
| 回合数 | 当前回合数（从1开始） | 1 |

**ChessBoard类**：核心类，封装棋盘状态和操作。主要接口如下：

```csharp
public class ChessBoard
{
    // 创建新棋局
    public static ChessBoard LoadFromFen(string fen);
    public string ToFen();
    
    // 走法相关
    public List<Move> Moves();          // 获取所有合法走法
    public List<Move> Moves(PieceType pieceType); // 获取指定棋子的走法
    public void Move(Move move);        // 执行走棋
    
    // 局面判断
    public bool IsCheckmate { get; }
    public bool IsStalemate { get; }
    public bool IsDraw { get; }
    public bool IsCheck { get; }
    
    // 游戏控制
    public void Resign(Color color);
    public bool IsResigned { get; }
    
    // 属性
    public Color TurnToMove { get; }    // 当前走棋方
    public int PlyCount { get; }        // 半步行棋数
}
```

#### 3.3.2 关键接口设计

**Player主控类**：

```csharp
public class Player : MonoBehaviour
{
    // 游戏模式
    public void StartLocalGame();
    public void StartRobotGame(int aiDepth = 3);
    public async Task CreateGame();
    public async Task JoinLobbyByCode(string lobbyCode);
    
    // 走棋处理
    public void OnSquareClicked(Vector3 position);
    public void ExecuteMove(string moveStr);  // 坐标走法
    public void ExecuteSanMove(string san);    // 代数走法
    
    // AI走棋
    private async Task DoRobotMoveAsync();
    
    // 在线对战
    private async Task SubscribeToPlayerMessages();
    private void OnBoardUpdate(string fenJson);
    
    // 游戏结束
    public void Resign();
    public async void SubmitGameScore();
}
```

**ChessAI类**：

```csharp
public class ChessAI
{
    public ChessAI(int maxDepth);
    public Move GetBestMove(ChessBoard board);
    
    // 混合路由（深度4/5走云端）
    private Move GetCloudMove(ChessBoard board, int depth);
}
```

**LeaderboardAPI类**：

```csharp
public class LeaderboardAPI
{
    public async Task<bool> SubmitScore(string playerName, int score, string gameMode);
    public async Task<List<LeaderboardEntry>> GetLeaderboard(string gameMode, int limit = 50);
    public async Task<PlayerRank> GetPlayerRank(string playerName, string gameMode);
    public async Task<bool> RenamePlayer(string oldName, string newName);
}
```

#### 3.3.3 方案对比——多个维度的选型过程

##### (1) 游戏引擎选型：Unity vs Godot vs Unreal vs 纯C#

| 维度 | Unity | Godot | Unreal Engine | 纯C#控制台 |
|------|-------|-------|---------------|------------|
| 学习曲线 | 中等 | 较平缓 | 陡峭 | 低 |
| 2D/3D支持 | 都强 | 2D强、3D较弱 | 3D极强 | 无 |
| 跨平台能力 | Windows/Mac/Linux/移动端 | 同上 | 同上 | 平台无关 |
| 国际象棋资源 | 有免费3D模型 | 较少 | 少 | 需自制 |
| 云服务集成 | UGS原生支持 | 需第三方 | 需第三方 | 无 |
| 社区规模 | 极大 | 中等 | 大 | — |
| 包体大小 | ~50MB | ~30MB | ~1GB+ | ~1MB |
| 中文资料 | 丰富 | 较少 | 较少 | 丰富 |

**分析**：Godot虽然开源免费且包体小，但3D能力相对薄弱，且缺乏开箱即用的云服务。Unreal Engine更适合大型3A项目，对课设来说过重。纯C#控制台虽然可以完整实现功能，但缺少图形界面和交互体验。

**结论**：Unity在资源丰富度、学习成本和云服务集成三个方面取得最佳平衡，选择Unity。

##### (2) AI算法选型：MinMax vs Claude API vs 混合路由

| 维度 | 本地MinMax+评估表 | 云端Claude API | 混合路由（最终方案） |
|------|-------------------|----------------|--------------------|
| 实现复杂度 | 中 | 低（AI编写） | 中 |
| 响应速度 | 快（深度3≤5秒） | 中等（3~15秒） | 可配置 |
| 棋力 | 中等 | 强（大模型推理） | 深度1/3弱、4/5强 |
| 运行成本 | 免费 | API按量计费 | 深度4/5才走云端 |
| 可离线 | 完全在线 | 需联网 | 部分离线 |
| 可控性 | 完全可控 | API变化可能影响 | 分层可控 |

**分析**：如果全部使用云端API，简单局面（深度1/3）的走法延迟比本地高3~10倍，而且调用大模型处理简单局面是资源的浪费。反之，如果全部使用本地MinMax，棋力上限受限于搜索深度。

**结论**：采用混合路由方案。深度1（Easy）和深度3（Medium）使用本地MinMax搜索；深度4（Hard）和深度5（Master）调用Claude API。当深度4/5不可用（如断网）时自动降级到深度3。

##### (3) 在线对战架构：UGS Cloud Code vs 自建服务器

| 维度 | UGS Cloud Code+Push（最终方案） | 自建ECS+WebSocket |
|------|-------------------------------|-------------------|
| 开发成本 | 低（SDK集成+C#云端代码） | 高（全栈自建） |
| 运维成本 | 0（托管） | 需维护服务器 |
| 实时性 | 中等（Push推送） | 高（WebSocket长连接） |
| 走法防作弊 | 服务端C#验证 | 需自建验证 |
| 扩展性 | 自动扩缩 | 手动管理 |
| 成本 | 免费层足够 | ECS月费约50元 |

**分析**：走法验证必须在服务端执行（防止客户端作弊），UGS Cloud Code提供原生C#运行环境，可以直接复用Gera Chess Library做校验。游戏状态同步通过UGS Push消息推送实现，无需维护长连接。

**结论**：核心游戏逻辑走UGS Cloud Code+Push，聊天功能自建WebSocket。

##### (4) 云服务平台：华为云ECS部署

**服务器配置**：

| 配置项 | 规格 |
|--------|------|
| 实例类型 | t6.medium.2 |
| vCPUs | 1 |
| 内存 | 2GiB |
| 操作系统 | Ubuntu 24.04 server 64bit |
| 弹性公网IP | 121.36.101.82 |
| 开放端口 | 3000（排行榜API）、3001（WebSocket聊天） |

**部署服务**：

| 服务 | 技术栈 | 部署方式 |
|------|-------|---------|
| 排行榜API | Node.js + Express + better-sqlite3 | PM2进程守护 |
| 聊天服务 | Node.js + ws | PM2进程守护 |

**方案选择理由**：自建排行榜API相比UGS Leaderboard更加灵活——支持多模式合并查询、玩家改名、积分更新策略自定义等特性。华为云ECS的部署本身就是课程评分中的加分项。

### 3.4 交互/界面设计

本系统采用3D图形界面，用户通过鼠标与棋盘交互。

**主菜单界面**：全屏半透明面板，包含游戏标题"Chess"、四个主按钮（Local Game / vs AI / Online Game / Leaderboard）、难度选择弹窗、在线选项界面。

**游戏内界面**：
- 顶部信息栏：显示当前回合提示、房间码（在线模式）、对手名称
- 右侧走棋历史面板：Toggle显示/隐藏，记录每一步走法
- 左侧评估条：实时显示局面评估值（仅本地/人机模式）
- 左下角提示按钮：AI推荐最佳走法
- 命令输入面板：支持键盘输入命令走棋
- 认输按钮：结束当前对局

**棋盘交互**：用户点击选中己方棋子（高亮蓝色），点击目标位置执行走棋。支持视角自动切换（本地模式每步切换黑白方视角）。

**状态转换图**：

```
启动 → 主菜单 → 本地游戏 → 游戏进行 → 游戏结束 → 返回主菜单
              → AI游戏 → 难度选择 → 游戏进行 → ...
              → 在线游戏 → 创建/加入 → 等待对手 → 游戏进行 → ...
              → 排行榜 →（可随时切换显示/隐藏）
```

---

## 四、测试计划

### 4.1 编写目的

本节制定国际象棋对战平台的测试计划，包括测试内容、测试方法与预期结果，确保软件功能正确、性能达标、运行稳定。

### 4.2 计划安排

| 测试阶段 | 测试内容 | 预计时间 | 测试工具 |
|---------|---------|---------|---------|
| 单元测试 | 各模块功能验证 | 3天 | Unity Editor / curl |
| 集成测试 | 模块间协同工作 | 2天 | Unity Editor |
| 系统测试 | 完整流程验证 | 2天 | 打包exe + 双客户端 |
| 性能测试 | AI响应时间、在线延迟 | 1天 | 日志计时 / 秒表 |

### 4.3 单元测试设计

#### 模块1：走法校验（ChessBoard）

| 测试用例ID | 测试内容 | 测试步骤 | 输入数据 | 预期结果 | 实际结果 |
|-----------|---------|---------|---------|---------|---------|
| TC-CHESS-01 | 兵初始走两步 | 白方e2e4 | 标准开局 | e4格出现白兵 | 通过 |
| TC-CHESS-02 | 兵吃子 | exd5（d5有黑子） | 特定局面 | 黑子消失，白兵到d5 | 通过 |
| TC-CHESS-03 | 王车易位（王翼） | e1g1 | 王和车未移动 | 王到g1，车到f1 | 通过 |
| TC-CHESS-04 | 王车易位（后翼） | e1c1 | 王和车未移动 | 王到c1，车到d1 | 通过 |
| TC-CHESS-05 | 兵升变为后 | e7e8q | e7有白兵 | 兵消失，e8出现白后 | 通过 |
| TC-CHESS-06 | 非法走法 | e2e5 | 标准开局 | 报错"Illegal move" | 通过 |
| TC-CHESS-07 | 将军检测 | 制造将军局面 | 特定FEN | IsCheck==true，王高亮 | 通过 |
| TC-CHESS-08 | 将杀检测 | 制造将杀局面 | 特定FEN | IsCheckmate==true | 通过 |
| TC-CHESS-09 | 逼和检测 | 制造逼和局面 | 特定FEN | IsStalemate==true | 通过 |
| TC-CHESS-10 | FEN恢复 | load fen后走棋 | 任意FEN | 走法与原始局面一致 | 通过 |

#### 模块2：AI走棋（ChessAI）

| 测试用例ID | 测试内容 | 测试步骤 | 预期结果 | 实际结果 |
|-----------|---------|---------|---------|---------|
| TC-AI-01 | 深度1走棋 | Easy模式，白方走d4 | 1秒内AI响应，日志含depth=1 | 通过 |
| TC-AI-02 | 深度3走棋 | Medium模式，白方走d4 | 5秒内AI响应，日志含depth=3 | 通过 |
| TC-AI-03 | 深度4走棋 | Hard模式 | AI走棋成功，日志含depth=4 | 通过 |
| TC-AI-04 | 深度5走棋 | Master模式 | AI走棋成功，日志含depth=5 | 通过 |
| TC-AI-05 | 连续多局 | Robot模式连续5局 | 无卡死，每局正常结束 | 通过 |
| TC-AI-06 | 吃子选择 | 有吃子机会 | AI选择最优吃子 | 通过 |

#### 模块3：在线对战（Cloud Code）

| 测试用例ID | 测试内容 | 测试步骤 | 预期结果 | 实际结果 |
|-----------|---------|---------|---------|---------|
| TC-ONLINE-01 | 创建房间 | 客户端A点Create Room | 显示6位房间码 | 通过 |
| TC-ONLINE-02 | 加入房间 | 客户端B输入房间码点Join | B加入成功，A进入游戏 | 通过 |
| TC-ONLINE-03 | 走棋同步 | A走e2e4 | B棋盘自动更新 | 通过 |
| TC-ONLINE-04 | 将军推送 | A将杀B | B收到结果 | 通过 |
| TC-ONLINE-05 | 断线重连 | 刷新后重新Join | 恢复局面继续对弈 | 通过 |

#### 模块4：排行榜（Leaderboard API）

| 测试用例ID | 测试内容 | 测试步骤 | 预期结果 | 实际结果 |
|-----------|---------|---------|---------|---------|
| TC-LB-01 | 提交分数 | POST /score模拟游戏结束 | 服务器返回200 | 通过 |
| TC-LB-02 | 查询排行 | GET /leaderboard | 返回排序列表 | 通过 |
| TC-LB-03 | 模式筛选 | GET /leaderboard?game_mode=robot | 仅返回robot模式 | 通过 |
| TC-LB-04 | 高分覆盖 | 同玩家同模式第二次提交更高分 | 分数更新 | 通过 |
| TC-LB-05 | 玩家改名 | PUT /player/OldName | 所有记录更新 | 通过 |

### 4.4 总体测试设计

#### 4.4.1 评价准则

功能完整性：全部需求功能均已实现并能正常运行；性能指标：AI响应时间、在线延迟、查询响应时间均满足需求定义；稳定性：连续运行无崩溃、无卡死；代码规范：命名规范、注释完整、可读性好。

#### 4.4.2 环境与数据准备

| 测试环境 | 配置 |
|---------|------|
| 客户端硬件 | Intel i5 / 16GB RAM / Windows 10/11 |
| 开发环境 | Unity 2022.3 + Visual Studio 2022 |
| 服务端环境 | 华为云ECS / Ubuntu 24.04 |
| 网络环境 | 校园网（测试在线对战和排行榜） |
| 测试数据 | 标准开局FEN、各种将杀局面FEN、多组玩家名和分数 |

#### 4.4.3 测试大纲

| 测试项目 | 测试内容 | 测试方法 | 测试结论 |
|---------|---------|---------|---------|
| 功能测试 | 逐一验证11项功能需求 | 按需求列表手工测试 | 全部通过 |
| AI性能测试 | 深度3响应时间 | 重复10次取平均 | 平均3.2秒（≤5秒，通过） |
| 在线延迟测试 | 局域网和公网场景 | 双客户端联机 | <200ms（通过） |
| 压力测试 | 排行榜100次并发提交 | curl并发脚本 | 数据库稳定（通过） |
| 兼容性测试 | Windows 10 / 11 | 打包exe运行 | 正常（通过） |

#### 4.4.4 结果分析

测试结果表明，系统功能完整，各项性能指标达到预期要求。AI走棋响应时间优化后性能良好（深度3平均3.2秒），在线对战延迟低于200ms，排行榜API运行稳定。部分扩展功能（如网络观棋、AI形势分析）可作为后续优化方向。

---

## 五、用户操作手册

### 5.1 编写目的

本手册帮助用户快速上手使用国际象棋对战平台，包括软件的安装运行、基本操作和功能使用说明。

### 5.2 运行环境

| 项目 | 要求 |
|------|------|
| 操作系统 | Windows 10 或 Windows 11 |
| 处理器 | Intel Core i3或同等级别及以上 |
| 内存 | 4GB及以上 |
| 硬盘空间 | 500MB及以上 |
| 运行方式 | Unity Editor 2022.3+打开项目运行，或运行打包exe |
| 网络 | 在线对战和排行榜功能需要联网 |

### 5.3 典型用例

#### 用例1：本地双人对战

1. 启动游戏，进入主菜单
2. 点击"Local Game"按钮
3. 白方点击己方棋子（高亮蓝色），再点击目标格走棋
4. 视角自动切换，黑方走棋
5. 交替进行，直到将杀/和棋/认输结束
6. 游戏结束显示结果，分数自动提交，返回主菜单

#### 用例2：人机对战

1. 启动游戏，点击"vs AI"按钮
2. 在弹窗中选择难度（Easy/Medium/Hard/Master）
3. 白方走第一步，AI自动响应（显示"AI Thinking..."）
4. 交替走棋直到游戏结束
5. 可点击Hint按钮获取AI推荐走法提示

#### 用例3：在线对战（房主）

1. 启动游戏，点击"Online Game"按钮
2. 点击"Create Room"按钮
3. 显示6位房间码，将房间码告知对手
4. 等待对手加入，自动进入游戏
5. 走棋后通过UGS Push推送，对手棋盘自动更新

#### 用例4：在线对战（加入者）

1. 启动游戏，点击"Online Game"按钮
2. 在输入框中输入房间码，点击"Join"按钮
3. 成功加入后自动进入游戏

#### 用例5：查看排行榜

1. 主菜单中点击Leaderboard按钮
2. 查看排名列表（前三名特殊颜色，当前玩家高亮）
3. 可通过下拉框筛选游戏模式
4. 可输入玩家名并点击刷新
5. 按Tab键可快速切换排行榜显示/隐藏

### 5.4 使用条件

本软件为Windows桌面应用，建议在联网环境下使用以体验在线对战和排行榜功能。人机对战模式可在离线状态下使用（深度1/3完全离线，深度4/5需联网调用云端AI）。

---

## 六、体会与收获

通过本课程设计的完整开发流程，我深刻体会到了软件工程思想与实践相结合的重要性。从需求分析到设计、实现、测试的每一个环节，我都获得了宝贵的实践经验。

**关于技术能力的提升**。本项目涵盖了Unity游戏开发、C#服务端编程、Node.js后端开发、Linux服务器部署等多个技术领域。虽然每个领域的学习都有AI辅助，但通过实际项目把这些技术串联起来，让我对全栈开发有了更清晰的认识。

**关于AI协作的思考**。本项目的开发过程是一次"人类+AI"协作的深度实践。我学会了如何更准确地向AI描述需求——从"帮我写一个功能"进阶到"给出完整上下文、技术栈约束和预期结果"。我也学会了如何验收AI的代码——加日志验证逻辑、编译检查找错误、功能走查保完整。这种"提需求→审计划→验结果"的工作流，让我认识到AI工具的核心价值不是替代开发者，而是加速执行、辅助决策。

**关于工程规范的实践**。项目中我坚持使用Git进行版本管理，每次提交都包含独立的功能改动，方便追踪和回退。每项较大的改动都先由AI出计划文档，我审核后再执行，确保了开发方向不会偏离。这种Plan先行的习惯是我在本次课设中最大的收获之一。

**关于云服务的认识**。通过部署华为云ECS服务器，我了解了从云主机申请、环境配置到服务部署和运维的完整流程。自建排行榜API让我对后端架构设计有了实际体验。

总之，这次课程设计不仅让我完成了一个功能完整的国际象棋平台，更让我学会了如何利用AI工具提升开发效率、如何规范管理项目进度。这些经验将在我今后的学习和工作中持续发挥作用。

---

## 七、附录

### 附录A：AI辅助开发实践报告

#### A.1 背景与动机

课程要求使用"软件设计思想与方法"，而AI编程助手的发展让我们有机会探索一种新的开发模式——人类负责决策和验收，AI负责具体编码实现。本项目全程以AI编程助手为核心工具完成。

使用的AI工具：
- **Claude Code（Anthropic）**：主要编码工具，负责架构设计、代码生成、Bug修复、场景配置
- **Trae（ByteDance）**：IDE集成AI，支持编译自动检查、MCP工具链

工作量分配说明：Unity官方提供了ChessDemo的3D模型资源（Free Low Poly Chess Set）和Cloud Code联机示例框架，但以下内容全部由我+AI协作完成：
- 完整的UI系统（主菜单、难度选择、走棋历史、评估条、提示、聊天）
- AI引擎（MinMax搜索算法+云端Claude API混合路由）
- 独立排行榜服务器（Node.js + SQLite，部署于华为云ECS）
- 在线聊天WebSocket服务器
- 动画系统、音效系统、视觉提示系统
- 所有Bug修复和性能优化

#### A.2 怎么提需求——与AI的交流过程

##### A.2.1 基本模式：现象+上下文+期望

经过多次实践磨合，我总结出了有效的提问模式：

> **推荐提法**：
> ```
> 我：我需要一个排行榜功能，具体要求：
> - 游戏结束后自动提交分数到服务器
> - 支持查看全服排名
> - 分模式显示（本地双人/人机对战/在线对战）
> - 已有一个Node.js服务器在121.36.101.82:3000，需要用Express+SQLite
> - 参考lichess的排行榜风格，显示排名/玩家名/分数/模式/日期
> ```
>
> AI：理解了，先出设计文档，你审核后再编码。

核心要点：给AI提供完整的上下文（现有技术栈+具体期望+参考对象），产出越接近目标。同时要求AI先出计划再编码，避免盲目生成。

##### A.2.2 具体案例：AI卡死修复

**背景**：人机对战模式中，白方走棋后AI一直显示"AI Thinking..."再无响应，控制台无任何报错。

**我的提法**：
```
我在Robot Game里走了一步，AI显示"AI Thinking..."一直不返回。
控制台无报错。请分析根因并修复，不要只加try-catch表面处理。
```

**AI的发现**：
1. `DoRobotMoveAsync`没有try-catch，一旦异常`_aiThinking`永远不会重置为false
2. 嵌套`Task.Run`导致线程池饥饿——MinMax递归中每次调用`board.Moves()`都创建约16个Task，深度3总共可能创建千万级Task
3. FEN序列化/反序列化克隆棋盘极其低效
4. `boardSnapshot`是引用而非深拷贝

**改动**：AI发现全部4个问题后给出了完整的修复方案（加try-catch、重构并行生成、改用深拷贝快照），并在修复后添加了`[AI move: e2e4 (depth=3, eval=0.5)]`这样的结构化日志用于验证。

##### A.2.3 具体案例：在线对战功能

**背景**：Unity Gaming Services（UGS）提供了Cloud Code示例框架，但只是一个最基本框架——创房/加入/走棋。我需要在此基础上实现完整的在线对战流程。

**我的提法**：
```
参考Unity官方联机项目示例，我需要：
1. 创建房间后显示6位房间码，等待对手加入
2. 对手从另一个客户端输入房间码加入
3. 加入后自动分配黑白方
4. 每步走棋通过Cloud Code验证，通过UGS Push推送给对手
5. 对手客户端收到推送后自动更新棋盘
6. 支持重新连接（对手刷新页面后能恢复局面）
每个步骤请先出计划，我审核后再执行。
```

**AI的处理**：先画出了完整的数据流图，给出了6步分阶段的实现计划，每阶段一个git commit。每阶段我都在两个客户端上跑通后进入下一阶段。

##### A.2.4 具体案例：排行榜+华为云部署

**背景**：需求一个独立的排行榜系统，需要部署到云服务器上。

**我的提法**：
```
我需要自建一个排行榜系统，不依赖UGS Leaderboard，因为：
1. 部署到华为云ECS（121.36.101.82）会加分
2. 要有自己完整的CRUD API
3. 支持多模式筛选
4. 技术栈建议用Node.js + Express + SQLite

请先出设计，重点考虑API端点设计、数据库schema、PM2进程管理。
```

**AI的输出**：设计了完整的RESTful API（GET /leaderboard、POST /score、PUT /player/:old_name等）、数据库schema（唯一约束(player_name, game_mode)、只更新更高分）、部署脚本。

#### A.3 怎么测试AI完成的结果

##### A.3.1 日志驱动

我的测试核心方法是"让AI在代码里加日志，通过日志而非肉眼判断逻辑正确性"。

**实际案例**：修复积分显示为0时，日志直接定位根因：
```
[Leaderboard] Score submitted: Player_xxx -> 60 (mode: local, rank: #1)
[Leaderboard] Current scores: robot=0, local=60, online=0
```
发现是模式映射错误（"all"被强制替换为"robot"），而非积分未提交。

**常用日志格式**：
- `[AI move: e2e4 (depth=3, eval=0.5)]` — AI走棋日志
- `[MoveAnimator] AnimateSyncBoard: found 32 pieces, 1 toCapture, 1 toMove` — 动画处理
- `[Check] White king checked at e1, highlighting...` — 将军检测
- `[Leaderboard] Score submitted: Player_xxx -> 60 (mode: local, rank: #1)` — 排行榜提交

[图1：Unity Console面板，显示结构化日志]

##### A.3.2 编译验证+功能走查

1. **编译验证**：AI生成代码后，Trae IDE自动触发编译检查，发现缺少using语句、类型不匹配等问题
2. **功能走查**：维护一份功能验收清单（PROJECT_UI_GUIDE.md），逐项测试：

| 测试功能 | 测试方法 | 验证标准 |
|---------|---------|---------|
| 本地游戏 | 走几步棋，切换视角 | 棋盘正确更新，视角自动切换 |
| AI走棋 | 深度1~4各测试 | AI正常响应，不卡死 |
| 在线对战 | 双客户端创房+加入+走棋 | 同步无延迟，推送正常 |
| 排行榜 | 游戏结束→查看排行榜 | 分数正确显示 |
| 将军提示 | 制造将军局面 | 王高亮，走法过滤 |
| 悔棋 | 走几步后undo | 棋盘回退正确 |
| 加载FEN | 命令输入FEN | 棋盘恢复到指定局面 |

##### A.3.3 远程服务器验证

排行榜部署在华为云ECS上，用curl+SSH验证：

```bash
curl -X POST http://121.36.101.82:3000/score \
  -H "Content-Type: application/json" \
  -d '{"player_name":"test","score":100,"game_mode":"local"}'
curl http://121.36.101.82:3000/leaderboard?game_mode=local
ssh root@121.36.101.82 "pm2 status"
```

#### A.4 怎么要求调整优化

##### A.4.1 精准反馈，提供证据

✅ 正确反馈：
```
Leaderboard中条目左侧rank/name/score列全部空白，
右侧mode和date正常显示。
[截图]
我排查了entryPrefab的子对象，发现左侧使用了中文名。
```

AI通过截图直观发现问题→推断出中文字段名问题→给出中英文双重匹配修复方案。

##### A.4.2 要求分析根因

> 我的要求：不要只修MoveAnimator的表面Bug，分析增量更新逻辑为什么失败，给出彻底方案。

AI分析后发现：棋子A吃掉棋子B时，B的位置在新状态中被A占据，所以B不会被检测为"消失"。最终给出"三步逻辑"——识别差异→执行吃子→执行移动的彻底修复方案。

##### A.4.3 Plan先行+Git分步管理

每个较大改动都采用"先出Plan→我审核→分步执行→每步git commit→我验证"的模式：

```
1. AI写计划文档（根因分析、修改步骤、文件清单、验证方法）
2. 我审核计划（修正方向、补充约束）
3. AI按步骤执行，每完成一个步骤就git add+git commit
4. 我测试验证通过后进入下一步
```

**为什么用Git分步管理**：每步独立commit，出了问题可以git revert回退单个步骤；git历史就是完整的开发日志，方便追踪AI每一步改动；老师审查代码时能看到清晰的迭代过程。

##### A.4.4 多模态LLM截图辅助

排行榜UI修复时，直接截取Unity运行截图发给AI，AI通过截图推断中文命名问题，给出双重匹配修复。一张截图胜过千言万语，多模态能力让AI能"看"到和开发者一样的画面，诊断效率大幅提升。

##### A.4.5 通过Unity MCP让AI直接理解场景对象

Unity MCP（Model Context Protocol）是一个关键的桥梁——它让AI能够直接通过HTTP接口与Unity Editor交互，读取和操作场景中的GameObject。

**MCP配置**（`.mcp.json`）：
```json
{
  "unity-mcp": {
    "type": "http",
    "url": "http://localhost:8080/"
  }
}
```

**具体应用场景**：

| 场景 | MCP操作 | 效果 |
|------|---------|------|
| 场景诊断 | diagnose_scene列出所有GameObject | AI知道场景中有哪些对象和层次结构 |
| 组件绑定 | manage_component设置属性 | AI直接在场景中绑定Inspector引用 |
| 创建UI | 动态创建GameObject+挂组件 | AI在Canvas下创建UI元素 |
| 布局调整 | 修改RectTransform属性 | AI调整UI的位置、大小、对齐 |
| 绑定按钮事件 | 设置Button.onClick | AI连接按钮点击到对应方法 |

**为什么要通过MCP让AI理解场景对象**：Unity场景中的GameObject和组件引用需要Inspector手动绑定，如果只写代码不配场景，程序无法运行。以前需要我在Unity里手动拖拽配置，现在AI可以直接操作。MCP让AI"看到"了和开发者一样的场景结构。

[图2：MCP场景诊断输出，展示GameObject层次结构]

#### A.5 AI工作流总结

```
1. 提需求：现象+上下文+约束+参考资料（AI搜索，我审核）
2. 出计划：AI先写计划文档，我审核确认方向
3. Git分步管理：按计划分步实施，每步git commit，可回退
4. 日志驱动：关键位置加结构化日志，以日志判断正确性
5. 编译验证：自动编译检查，发现缺失引用和类型错误
6. 功能走查：按验收清单逐项测试
7. MCP辅助：AI通过Unity MCP直接读取和操作场景对象
8. 截图反馈：多模态LLM用截图直观描述UI问题
9. 云服务验证：curl+SSH验证远程部署

核心原则：Plan先行、日志驱动、分步提交、闭环反馈。
```

#### A.6 心得体会

使用AI做课设的过程，对我来说最大的收获不是完成了功能，而是学会了"如何管理AI"：

1. **从"让AI写代码"到"让AI为我所用"**：一开始我只是让AI直接生成代码，但很快就发现——AI能做80%，剩下20%需要我精确地告诉它"你要改什么、按什么改、不要改什么"。这个过程训练了我的需求描述能力。

2. **学会验收AI的输出**：AI写出来的代码不一定正确，甚至可能看起来正确但有隐蔽Bug。我学会了"加日志→运行验证→看日志→比预期"这条链路，而不是盲目相信AI的输出。

3. **学会分治和迭代**：AI一次只能处理一个合理大小的任务。把一个复杂功能拆成5~10个小步骤，每步让AI完成一个可控的改动，然后git commit+验证——比让AI一次性做全部更高效、更可靠。

4. **MCP改变了AI的能力边界**：通过Unity MCP，AI不再是"只写代码但看不到场景"的开发者，而是可以像人类一样诊断场景结构、创建GameObject、绑定Inspector引用。这让AI从"代码生成器"变成了真正的"开发助手"。

5. **技术栈广度**：通过本项目我接触了Unity C#开发、UGS云服务集成、Node.js后端、Linux服务器部署等。虽然每个领域都是AI辅助下完成的，但我对整个全栈开发流程有了清晰的认识。

6. **关于AI的思考**：AI编程助手不是取代程序员，而是改变了程序员的工作方式——从"自己实现每个细节"变成"制定架构、审核产出、集成测试"。AI是最智能的搜索引擎，但最终的判断力和决策权仍在我手中。

### 附录B：程序代码简要说明

#### B.1 Unity客户端（C#）

| 文件路径 | 功能说明 |
|---------|---------|
| Chess/Assets/Player.cs | 主控类，管理游戏初始化、用户交互、AI走棋、在线对战 |
| Chess/Assets/Piece.cs | 棋子组件，记录初始位置 |
| Chess/Assets/Chess/AI/ChessAI.cs | MinMax搜索+评估表实现 |
| Chess/Assets/Chess/AI/ClaudeApiProvider.cs | Claude API云端AI调用 |
| Chess/Assets/Chess/AI/ClaudeConfig.cs | Claude API配置（密钥、基地址） |
| Chess/Assets/Chess/UI/MainMenuUI.cs | 主菜单界面控制 |
| Chess/Assets/Chess/UI/MoveHistoryUI.cs | 走棋历史面板 |
| Chess/Assets/Chess/UI/CommandInputUI.cs | 命令输入面板 |
| Chess/Assets/Chess/UI/DifficultySelector.cs | AI难度选择界面 |
| Chess/Assets/Chess/UI/EvaluationBar.cs | 局面评估条 |
| Chess/Assets/Chess/UI/HintSystem.cs | 最佳走法提示 |
| Chess/Assets/Chess/UI/ChatUI.cs | 在线聊天界面 |
| Chess/Assets/Chess/Animation/MoveAnimator.cs | 走棋动画 |
| Chess/Assets/Chess/Animation/GameEndAnimator.cs | 游戏结束动画 |
| Chess/Assets/Chess/Audio/AudioManager.cs | 音效管理 |
| Chess/Assets/Leaderboard/LeaderboardAPI.cs | 排行榜API封装 |
| Chess/Assets/Leaderboard/LeaderboardUI.cs | 排行榜界面 |
| Chess/Assets/Chess/ChessBoard/ChessBoard.cs | 棋盘状态核心类 |

#### B.2 Cloud Code服务端（C#）

| 文件路径 | 功能说明 |
|---------|---------|
| ChessCloudCode/Chess.cs | HostGame、JoinGame、MakeMove、Resign端点 |
| ChessCloudCode/ModuleConfig.cs | 依赖注入配置 |

#### B.3 排行榜API（Node.js，部署于华为云ECS）

| 文件路径 | 功能说明 |
|---------|---------|
| leaderboard-server/server.js | Express REST API，better-sqlite3存储，PM2管理 |
| leaderboard-server/package.json | 项目依赖配置 |

---

*报告完*
