# Bug 修复计划：棋子重复生成 & 坐标标签对齐

## Bug 1：棋子移动时旧模型未删除，生成重复棋子

### 根因分析
`MoveAnimator.AnimateSyncBoard` 的增量更新逻辑存在严重缺陷：

1. **`_piecesByPos` 字典从未被初始化**：首次调用 `AnimateSyncBoard` 时，字典为空，所有棋子都被视为"新增"并实例化。但旧的棋子（由 `SyncBoard` 的 else 分支或场景初始状态创建的）仍然存在于 `board.transform` 下，没有被清理。
2. **没有在动画开始前清理旧的棋子**：原逻辑假设 `_piecesByPos` 能跟踪所有现有棋子，但实际上场景中已有 32 个初始棋子不在字典中。
3. **匹配逻辑不可靠**：通过名称匹配旧棋子和新位置，但首次调用时字典为空，无法匹配。

### 修复方案
简化 `AnimateSyncBoard` 逻辑：
- **在动画开始前，先收集 `board.transform` 下所有现有棋子**，建立位置→棋子的映射
- 对比新旧状态，分为三类：移动（位置变化）、吃子（消失）、新增（升变等）
- 移动的棋子执行动画，吃子的棋子缩小消失，新增的棋子缩放出现
- 动画结束后更新 `_piecesByPos` 字典

### 具体修改

**MoveAnimator.cs**：
1. 重写 `AnimateSyncBoard` 方法：
   - 首先扫描 `board.transform` 下所有子对象，通过位置建立 `_piecesByPos` 映射
   - 对比新旧棋盘状态，计算差异
   - 位置不变且类型相同的棋子 → 不动
   - 位置变化且类型相同的棋子 → 移动动画
   - 新状态中不存在的棋子 → 吃子动画（缩小消失）
   - 新状态中有但旧状态没有的棋子 → 新增动画（缩放出现）
   - 更新 `_piecesByPos` 字典

## Bug 2：坐标标签未与棋盘对齐且字体太大

### 根因分析
1. **坐标偏移**：`Board` 在 `BoardPivot` 下的 `localPosition` 是 `(-3.5, 0, -3.5)`，而棋子位置是相对于 `Board` 的（0-7 范围）。标签生成在 `BoardPivot` 下，但使用了 0-7 的坐标，没有考虑 `Board` 的偏移。
2. **字体太大**：`fontSize = 24` 对于世界空间 TextMeshPro 来说太大，需要缩小。

### 修复方案

**BoardCoordinateLabels.cs**：
1. 将标签父对象改为 `Board`（而不是 `BoardPivot`），这样坐标 0-7 就自然对齐
2. 减小字体大小从 24 改为 8
3. 减小 labelOffset 从 0.5 改为 0.4
4. 调整 labelY 从 0.05 改为 0.01

## 实施步骤

### Step 1: 修复 MoveAnimator.cs
1. 重写 `AnimateSyncBoard` 方法，先扫描现有棋子建立映射
2. Git 提交

### Step 2: 修复 BoardCoordinateLabels.cs
1. 修改标签父对象为 Board
2. 调整字体大小和偏移
3. Git 提交
