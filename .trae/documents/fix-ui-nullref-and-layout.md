# UI 修复计划 - 游戏中显示主菜单、NullRef、Cmd无法输入

## 问题诊断

### 问题 1: 游戏开始后主菜单面板仍然显示 + 卡片超出屏幕
**根因**: `MainMenuUI.Awake()` 构建面板时，面板默认是 active 的。`MainMenuUI.BuildUI()` 没有在末尾调用 `SetActive(false)`。虽然 `StartLocalGame()` 等方法调用了 `mainMenuUI.Hide()`，但面板在创建时就是可见的。同时布局总高度过大（Spacer 70px + Title 50px + ResultText 36px + 4 Cards 256px + padding 80px = ~492px），在低分辨率下卡片超出屏幕。

**修复**:
- BuildUI() 末尾添加 `_panel.SetActive(false)`
- 减小 Spacer、padding、字号等，确保所有卡片在屏幕内

### 问题 2: MoveHistoryUI.BuildPanel() NullReferenceException (line 105)
**根因**: `_panel.SetActive(false)` 在第 81 行执行，之后在 inactive 的父对象上创建子对象并添加 `TextMeshProUGUI` 组件。TMP 在 `OnEnable()` 中初始化字体材质等内部引用，当 GameObject 是 inactive 时 `OnEnable()` 不会被调用，导致后续访问 `.text` 时内部引用为 null 产生 NullRef。

**修复**: 将 `_panel.SetActive(false)` 移到 BuildPanel() 方法的最后，先完成所有组件添加，再隐藏面板。

### 问题 3: CommandInputUI.BuildHeader() NullReferenceException (line 145)
**根因**: 与问题 2 相同 - `CommandInputUI.BuildPanel()` 第 80 行 `_panel.SetActive(false)`，之后在 inactive 面板上添加 TMP 组件导致 NullRef。

**修复**: 将 `_panel.SetActive(false)` 移到 BuildPanel() 末尾。

### 问题 4: Cmd 输入框无法输入
**根因**: CommandInputUI 的 `BuildOutputArea()` 中 ScrollRect 没有设置 `viewport` 属性，导致滚动区域布局异常，可能挤压输入框。同时 NullRef 导致整个面板构建中断，输入区域可能根本没有被创建。

**修复**:
1. 修复 NullRef 后面板可以完整构建
2. 为 CommandInputUI 的 ScrollRect 设置 viewport
3. 将 TMP_InputField 的 textComponent 和 placeholder 分离到不同子对象（标准 TMP_InputField 结构）

### 问题 5: ChatUI 同样的 NullRef 风险
**根因**: ChatUI.BuildPanel() 也在第 78 行设置了 `_panel.SetActive(false)`，但 ChatUI 的面板目前可能还没被触发打开所以没报错。

**修复**: 同样将 SetActive(false) 移到末尾。

### 问题 6: DifficultySelector 同样的 NullRef 风险
**根因**: DifficultySelector.BuildUI() 在第 42 行设置了 `_panel.SetActive(false)`。

**修复**: 同样将 SetActive(false) 移到末尾。

## 修复步骤

### Step 1: 修复 MoveHistoryUI - NullRef
- 将 `_panel.SetActive(false)` 从第 81 行移到 BuildPanel() 末尾（第 165 行之后）
- 将 Title 区域从手动锚点定位改为 LayoutElement 方式（与面板布局兼容）
- CloseBtn 改为在 HorizontalLayoutGroup 内使用 LayoutElement

### Step 2: 修复 CommandInputUI - NullRef + 输入框
- 将 `_panel.SetActive(false)` 从第 80 行移到 BuildPanel() 末尾
- 为 ScrollRect 设置 viewport
- 将 TMP_InputField 的 textComponent 和 placeholder 分离到不同子对象

### Step 3: 修复 ChatUI - NullRef 预防
- 将 `_panel.SetActive(false)` 从第 78 行移到 BuildPanel() 末尾

### Step 4: 修复 DifficultySelector - NullRef 预防
- 将 `_panel.SetActive(false)` 从第 42 行移到 BuildUI() 末尾

### Step 5: 修复 MainMenuUI - 面板初始隐藏 + 布局优化
- BuildUI() 末尾添加 `_panel.SetActive(false)`
- 减小 Spacer 高度：40→20, 10→5, 20→10
- 减小 padding：60→40, 40→20
- 减小 Title 字号：42→32, preferredHeight 50→40
- 减小 Card 高度：64→56

### Step 6: 修复 EvaluationBar - NullRef 预防
- 将 `_barObj.SetActive(false)` 移到 BuildBar() 末尾

### Step 7: 验证
- 进入 Play Mode 测试所有 UI 流程
- 检查控制台无 NullRef 错误
- 测试 Cmd 输入框可正常输入
- 测试游戏开始后主菜单正确隐藏
