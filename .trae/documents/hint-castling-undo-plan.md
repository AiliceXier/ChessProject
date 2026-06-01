# 实现计划：Hint修复 + 王车易位提示 + 悔棋功能

## 一、修复Hint按钮点击无提示

### 问题分析
`HintSystem.EnsureButton()` 中，当 `hintBtnRef` 存在且已有 `Button` 组件时，**不会注册 `ShowHint` 监听器**（只在 `btn == null` 时才添加），导致点击按钮无反应。

### 修复步骤
1. **修改 `HintSystem.cs`**：在 `EnsureButton()` 中，无论 Button 组件是否已存在，都先 `RemoveAllListeners()` 再 `AddListener(ShowHint)`

---

## 二、王车易位颜色提示

### 问题分析
当前玩家选中王后，无法直观看出哪些位置可以王车易位。需要在选中王时，对可以易位的目标格子（g1/c1 或 g8/c8）显示特殊颜色提示。

### 实现步骤
1. **修改 `Player.cs` 的 `SelectPiece()` 方法**：
   - 选中棋子后，检查该棋子是否为王
   - 如果是王，调用 `_localBoard` 获取该王的所有合法走法
   - 对每个走法，检查是否为王车易位（`move.Parameter is MoveCastle`）
   - 对易位目标位置，在棋盘上显示**黄色半透明高亮**
   - 对普通合法走法，显示**蓝色半透明高亮**（可选，增强体验）

2. **在 `Player.cs` 中添加高亮方法**：
   - `ShowMoveHighlights(List<Move> moves)` — 显示所有合法走法的高亮
   - `ClearMoveHighlights()` — 清除所有高亮

3. **高亮对象**：使用场景中已有的 Board 子对象，动态创建 Quad 作为高亮标记
   - 普通走法：蓝色半透明 `(0.3, 0.5, 1.0, 0.35)`
   - 王车易位：黄色半透明 `(1.0, 0.85, 0.0, 0.45)`
   - 吃子走法：红色半透明 `(1.0, 0.3, 0.3, 0.35)`

4. **修改 `SelectPiece()` 和 `MakeMove()`**：
   - 选中棋子时显示高亮
   - 取消选择/走棋后清除高亮

---

## 三、悔棋功能（Local 和 Robot 模式）

### 问题分析
`Player.UndoLastLocalMove()` 方法已存在且逻辑完整，但**没有UI按钮触发它**。

### 实现步骤
1. **创建 `UndoButton.cs` 脚本**：
   - 挂载在场景中的 UndoBtn GameObject 上
   - 引用 Player，点击时调用 `Player.UndoLastLocalMove()`
   - 仅在 Local/Robot 模式下显示

2. **通过 MCP 在场景中创建 UndoBtn GameObject**：
   - 在 Canvas 下创建按钮，位于 Hint 按钮旁边
   - 使用场景引用方式（public 字段），方便在 Unity Inspector 中编辑

3. **修改 `Player.cs`**：
   - 添加 `public GameObject undoButton;` 字段
   - 在 `StartLocalGame()` / `StartRobotGameWithDifficulty()` 中 `undoButton.SetActive(true)`
   - 在 `ShowGameOver()` / `HideInGameUI()` 中 `undoButton.SetActive(false)`
   - 添加 `public void OnUndoClicked()` 方法调用 `UndoLastLocalMove()`

4. **同样修复 Hint 按钮**：确保 HintBtn 场景引用的 Button 组件也注册了监听器

---

## 四、Git 提交

1. 所有修改完成后，执行 `git add` + `git commit`

---

## 文件修改清单

| 文件 | 修改内容 |
|------|----------|
| `Chess/Assets/Chess/UI/HintSystem.cs` | 修复 EnsureButton 中 Button 监听器注册问题 |
| `Chess/Assets/Player.cs` | 添加 undoButton 字段、OnUndoClicked()、走法高亮逻辑、修改 SelectPiece() |
| `Chess/Assets/Chess/UI/UndoButton.cs` | 新建：悔棋按钮脚本（可选，也可直接在 Player 中处理） |

## MCP 场景操作

| 操作 | 说明 |
|------|------|
| 创建 UndoBtn | 在 Canvas 下创建按钮 UI，位于 Hint 按钮旁 |
| 绑定引用 | 将 UndoBtn 拖到 Player.undoButton 字段 |
