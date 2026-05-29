# 国际象棋项目完善计划

## 项目现状分析

### 已有功能 ✅
| 功能 | 状态 | 说明 |
|---|---|---|
| 3D 棋盘与棋子渲染 | ✅ 完成 | 8x8 棋盘，12种棋子预制体（Light/Dark），FEN 驱动 |
| 鼠标交互走棋 | ✅ 完成 | InputSystem + Raycast 选择/移动棋子 |
| 走法验证 | ✅ 完成 | Chess.NET 库完整验证（含王车易位、吃过路兵、升变） |
| 双人对战（本地） | ✅ 完成 | Local 模式，交替走棋，自动翻转视角 |
| 人机对战 | ✅ 完成 | Minimax + Alpha-Beta，深度3，子力+位置评估 |
| 在线对战 | ✅ 完成 | UGS CloudCode + Lobby + Push 消息 |
| 胜负判断 | ✅ 完成 | 将杀/逼和/投降/超时/和棋规则全部覆盖 |
| 排行榜 | ✅ 完成 | 独立 Node.js 服务器 + LeaderboardUI 面板 |
| 分数计算与提交 | ✅ 完成 | 三种模式分别计分 |

### 缺失功能 ❌（对照 temp 需求文档）

#### 基本要求（80分）
| 编号 | 需求 | 状态 | 差距 |
|---|---|---|---|
| 1 | 合适的数据结构描述棋盘、棋子、棋谱 | ⚠️ 部分 | 棋盘/棋子有，但**棋谱展示 UI 不存在** |
| 2 | 命令行清晰显示棋盘、棋子、坐标 | ❌ 缺失 | **无命令行模式**，仅有3D图形界面 |
| 3 | 命令控制棋子移动，错误走法报错 | ❌ 缺失 | **无命令行输入**，仅鼠标操作 |
| 4 | 双人对战，交替行棋，判断胜负 | ✅ 完成 | Local 模式已实现 |
| 5 | 记录棋谱，显示最近几步，从棋谱恢复棋局 | ❌ 缺失 | Chess 库有 PGN/FEN 支持，但**无 UI 展示和恢复功能** |
| 6 | 跨平台 | ⏭️ 暂缓 | Unity 本身跨平台，Android 构建暂不做 |

#### 扩展功能（20分）
| 编号 | 需求 | 状态 | 差距 |
|---|---|---|---|
| 扩1 | 图形界面鼠标操作 | ✅ 完成 | 已有 |
| 扩1b | 棋盘棋子动画效果 | ❌ 缺失 | **无任何动画**，棋子瞬移 |
| 扩1c | 背景音乐和音效 | ⏭️ 暂缓 | 暂不做 |
| 扩2 | 网络对局 | ✅ 完成 | UGS 在线模式 |
| 扩2c | 聊天功能 | ❌ 缺失 | 无聊天 |
| 扩2d | 积分榜 | ✅ 完成 | 排行榜系统 |
| 扩3 | 人机对弈 | ✅ 完成 | AI 模式 |
| 扩3b | 难度调整 | ❌ 缺失 | AI 深度固定为3 |
| 扩3c | 形势分析 | ❌ 缺失 | 无评估展示 |
| 扩3d | 下一步推荐 | ❌ 缺失 | 无走法提示 |

---

## Git 提交策略

**每完成一个小功能立即提交**，确保可随时回滚。提交格式：
```
feat: 功能描述
```

每个 Step 完成后执行：
```bash
git add -A
git commit -m "feat: xxx"
```

---

## 实施步骤（按执行顺序）

### Step 1: 棋谱记录与展示 UI
**目标**：实现走法历史面板，显示每步走法的 SAN 记谱

1. 创建 `Assets/Chess/UI/MoveHistoryUI.cs` 脚本
2. 在 Canvas 下创建 MoveHistoryPanel（ScrollView + 走法列表）
3. 修改 Player.cs：走棋后调用 MoveHistoryUI 更新
4. **Git 提交**: `feat: 棋谱记录与展示UI`

### Step 2: 命令行输入模式
**目标**：支持通过文本输入走法命令

1. 创建 `Assets/Chess/UI/CommandInputUI.cs` 脚本
2. 在 Canvas 下创建 CommandInputPanel（输入框 + 输出区域 + ASCII棋盘）
3. 修改 Player.cs：添加 MakeCommandMove() 方法
4. 实现命令解析（SAN 格式如 "e4"、"Nf3"，坐标格式如 "e2e4"）
5. 输入验证：非法走法显示错误提示
6. **Git 提交**: `feat: 命令行输入模式`

### Step 3: 棋盘坐标显示
**目标**：在 3D 棋盘边缘显示 a-h 和 1-8 坐标

1. 创建 `Assets/Chess/UI/BoardCoordinateLabels.cs` 脚本
2. 在 BoardPivot 下动态生成坐标标签（TextMeshPro 世界空间文字）
3. **Git 提交**: `feat: 棋盘坐标显示`

### Step 4: 棋谱恢复功能
**目标**：支持从 PGN/FEN 恢复棋局

1. 在 MoveHistoryUI 中添加"从 PGN 恢复"和"从 FEN 恢复"功能
2. 在 UIPanel 中添加恢复入口按钮和输入框
3. 调用 ChessBoard.LoadFromPgn() / LoadFromFen() 恢复棋局
4. **Git 提交**: `feat: 棋谱恢复功能`

### Step 5: 棋子移动动画
**目标**：棋子平滑移动而非瞬移

1. 创建 `Assets/Chess/Animation/MoveAnimator.cs`
2. 使用 Coroutine + Lerp 实现平滑移动
3. 吃子动画：被吃棋子缩小消失
4. 修改 SyncBoard() 逻辑为增量更新（不再每次销毁重建所有棋子）
5. **Git 提交**: `feat: 棋子移动动画`

### Step 6: AI 难度调整
**目标**：支持选择 AI 难度

1. 创建 `Assets/Chess/UI/DifficultySelector.cs`
2. 修改 StartRobotGame() 接受难度参数
3. 难度等级：简单(深度1)、中等(深度3)、困难(深度4)、大师(深度5)
4. 在 UI 中添加难度选择下拉菜单
5. **Git 提交**: `feat: AI难度调整`

### Step 7: 形势分析展示
**目标**：显示当前局面评估

1. 创建 `Assets/Chess/UI/EvaluationBar.cs`
2. 复用 ChessAI 的评估逻辑
3. 在 PlayerUIPanel 中添加竖向评估条（白优偏白/黑优偏黑）
4. 每次走棋后更新评估值
5. **Git 提交**: `feat: 形势分析展示`

### Step 8: 下一步推荐（走法提示）
**目标**：点击按钮显示推荐走法

1. 创建 `Assets/Chess/UI/HintSystem.cs`
2. 在 PlayerUIPanel 中添加"提示"按钮
3. 点击后使用 ChessAI.GetBestMove() 获取推荐走法
4. 在棋盘上高亮显示推荐走法的起点和终点
5. 3秒后自动取消高亮
6. **Git 提交**: `feat: 下一步推荐走法提示`

### Step 9: 聊天功能（在线模式）
**目标**：在线对局中可以与对手聊天

1. 创建 `Assets/Chess/UI/ChatUI.cs`
2. 在 Canvas 下创建 ChatPanel（消息列表 + 输入框 + 发送按钮）
3. 利用 UGS Push 消息传递聊天内容
4. **Git 提交**: `feat: 在线聊天功能`

### Step 10: UI 美化与中文化
**目标**：整体 UI 美化和中文本地化

1. 创建 `Assets/Chess/UI/MainMenuUI.cs` 重构主菜单
2. 所有 UI 文本中文化
3. 被吃棋子展示区
4. 整体 UI 调整优化
5. **Git 提交**: `feat: UI美化与中文化`

---

## 文件变更清单

### 新建文件
1. `Assets/Chess/UI/MoveHistoryUI.cs` — 棋谱历史面板
2. `Assets/Chess/UI/CommandInputUI.cs` — 命令行输入
3. `Assets/Chess/UI/BoardCoordinateLabels.cs` — 棋盘坐标标签
4. `Assets/Chess/Animation/MoveAnimator.cs` — 移动动画
5. `Assets/Chess/UI/DifficultySelector.cs` — 难度选择
6. `Assets/Chess/UI/EvaluationBar.cs` — 形势评估条
7. `Assets/Chess/UI/HintSystem.cs` — 走法提示
8. `Assets/Chess/UI/ChatUI.cs` — 聊天界面
9. `Assets/Chess/UI/MainMenuUI.cs` — 主菜单

### 修改文件
1. `Assets/Player.cs` — 集成棋谱、命令行、动画、提示等功能
2. `Assets/Chess/AI/ChessAI.cs` — 添加公共 Evaluate 方法供形势分析使用
3. Scene: `ChessDemo.unity` — 添加新 UI 面板、坐标标签等
