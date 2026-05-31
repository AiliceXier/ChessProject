# UI 场景化重构方案：让 Editor 修改持久化

## 问题根因

6 个 UI 脚本（CommandInputUI、HintSystem、MoveHistoryUI、DifficultySelector、ChatUI、EvaluationBar）的所有 UI 元素都通过 `new GameObject()` + `AddComponent<>()` 在运行时动态创建。每次游戏启动时，代码从零重建整个 UI，导致在 Unity Editor 中做的任何修改都会丢失。

## 解决方案：双模式架构

采用 **"Inspector 优先，代码兜底"** 的策略：
- 如果在 Inspector 中指定了场景对象引用 → 直接使用场景对象
- 如果没有指定 → 回退到代码创建（保持向后兼容）

这样用户可以逐步将 UI 迁移到场景中编辑，不需要一次性改完。

## 重构步骤

### 第 1 步：在场景中创建 UI 预制体结构

在 Canvas 下为每个 UI 模块创建空的父级 GameObject，层级结构如下：

```
Canvas
├── ... (已有场景对象)
├── CommandInputUI
│   ├── ToggleBtn
│   │   └── Text
│   └── Panel
│       └── InputRow
│           ├── InputField
│           │   └── TextArea
│           │       ├── Text
│           │       └── Placeholder
│           └── SendBtn
│               └── Text
├── MoveHistoryUI
│   ├── ToggleBtn
│   │   └── Text
│   └── Panel
│       ├── Header
│       │   ├── Title
│       │   └── CloseBtn
│       │       └── X
│       └── ScrollView (Mask)
│           └── Content
├── HintSystem
│   └── HintBtn
│       └── Text
├── EvaluationBar
│   ├── WhiteBar
│   └── EvalText
├── DifficultySelector
│   └── Panel
│       ├── Title
│       ├── Btn_Easy
│       ├── Btn_Medium
│       ├── Btn_Hard
│       ├── Btn_Master
│       └── BackBtn
└── ChatUI
    ├── ToggleBtn
    │   └── Text
    └── Panel
        ├── Header
        │   ├── Title
        │   └── CloseBtn
        │       └── Text
        ├── MsgScroll (Mask)
        │   └── Content
        └── InputRow
            ├── InputField
            │   └── TextArea
            │       ├── Text
            │       └── Placeholder
            └── SendBtn
                └── Text
```

### 第 2 步：修改每个 UI 脚本，添加 Inspector 引用字段

为每个脚本添加 `[SerializeField]` 引用字段，并修改 `EnsureXxx()` 方法：

**模式示例（以 HintSystem 为例）：**

```csharp
// 新增字段
[SerializeField] private GameObject hintBtnRef;

private void EnsureButton()
{
    if (_hintBtn != null) return;

    // 优先使用 Inspector 引用
    if (hintBtnRef != null)
    {
        _hintBtn = hintBtnRef;
        var btn = _hintBtn.GetComponent<Button>();
        if (btn == null)
        {
            btn = _hintBtn.AddComponent<Button>();
            btn.onClick.AddListener(ShowHint);
        }
        return;
    }

    // 回退：代码创建
    var canvas = FindObjectOfType<Canvas>();
    if (canvas == null) return;
    BuildButton(canvas.transform);
}
```

### 第 3 步：逐个脚本重构

#### 3.1 HintSystem.cs（最简单，3 个对象）

新增字段：
- `[SerializeField] private GameObject hintBtnRef;`

修改：
- `EnsureButton()`: 如果 `hintBtnRef` 不为 null，直接使用；否则走 `BuildButton()`
- `BuildButton()` 不变（作为兜底）

#### 3.2 EvaluationBar.cs（3 个对象）

新增字段：
- `[SerializeField] private GameObject evalBarRef;`

修改：
- `EnsureBar()`: 如果 `evalBarRef` 不为 null，从中获取 `_whiteImg`、`_evalText`；否则走 `BuildBar()`
- `BuildBar()` 不变

#### 3.3 CommandInputUI.cs（2 个顶层对象）

新增字段：
- `[SerializeField] private GameObject toggleBtnRef;`
- `[SerializeField] private GameObject panelRef;`
- `[SerializeField] private TMP_InputField inputFieldRef;`

修改：
- `Awake()`: 如果 `toggleBtnRef` 不为 null，直接使用并绑定事件；否则走 `BuildToggleButton()`
- `EnsurePanel()`: 如果 `panelRef` 不为 null，从中获取 `_inputField`；否则走 `BuildPanel()`

#### 3.4 MoveHistoryUI.cs（2 个顶层对象 + 动态内容）

新增字段：
- `[SerializeField] private GameObject toggleBtnRef;`
- `[SerializeField] private GameObject panelRef;`
- `[SerializeField] private ScrollRect scrollRectRef;`
- `[SerializeField] private RectTransform contentRtRef;`

修改：
- `Awake()`: 如果 `toggleBtnRef` 不为 null，直接使用；否则走 `BuildToggleButton()`
- `EnsurePanel()`: 如果 `panelRef` + `scrollRectRef` + `contentRtRef` 不为 null，直接使用；否则走 `BuildTestPanel()`
- `AddMoveRow()` / `AddTextCell()` / `AddLabelEntry()` 不变（动态内容仍由代码创建）

#### 3.5 DifficultySelector.cs（1 个面板 + 动态按钮）

新增字段：
- `[SerializeField] private GameObject panelRef;`

修改：
- `EnsurePanel()`: 如果 `panelRef` 不为 null，从中获取所有按钮并绑定事件；否则走 `BuildUI()`
- `BuildUI()` 不变

注意：DifficultySelector 的按钮是固定的 4 个难度 + 1 个返回按钮，适合完全场景化。

#### 3.6 ChatUI.cs（2 个顶层对象 + 动态消息）

新增字段：
- `[SerializeField] private GameObject toggleBtnRef;`
- `[SerializeField] private GameObject panelRef;`
- `[SerializeField] private TMP_InputField inputFieldRef;`
- `[SerializeField] private ScrollRect scrollRectRef;`
- `[SerializeField] private Transform contentParentRef;`

修改：
- `Awake()`: 如果 `toggleBtnRef` 不为 null，直接使用；否则走 `BuildToggleButton()`
- `EnsurePanel()`: 如果 `panelRef` 不为 null，从中获取 `_inputField`、`_scrollRect`、`_contentParent`；否则走 `BuildPanel()`
- `AddMessage()` 不变（动态消息仍由代码创建）

### 第 4 步：在场景中绑定引用

通过 MCP 或手动在 Unity Editor 中：
1. 在 Canvas 下创建上述层级结构的 GameObject
2. 为每个 GameObject 添加必要的组件（Image、Button、TMP_InputField、ScrollRect、Mask 等）
3. 设置好位置、颜色、字体等属性
4. 在 Player 脚本的 Inspector 中，将对应的 UI 脚本字段拖入场景对象
5. 在每个 UI 脚本的 Inspector 中，将场景对象拖入新增的 `[SerializeField]` 字段

### 第 5 步：验证

1. 在 Editor 中修改 UI 的位置/颜色/大小
2. 运行游戏，确认修改被保留
3. 确认所有功能正常（按钮点击、输入、滚动等）
4. 确认没有指定 Inspector 引用时，代码兜底创建仍然正常

## 优先级建议

按复杂度从低到高重构：
1. **HintSystem** — 最简单，只有 1 个按钮
2. **EvaluationBar** — 简单，3 个静态对象
3. **CommandInputUI** — 中等，2 个顶层对象 + InputField
4. **DifficultySelector** — 中等，但按钮全部固定
5. **MoveHistoryUI** — 复杂，有动态内容行
6. **ChatUI** — 最复杂，有动态消息 + InputField

## 关键注意事项

1. **事件绑定**：场景对象的 Button 组件需要在代码中绑定 `onClick.AddListener()`，不能只在 Inspector 中设置
2. **TMP_InputField 引用**：InputField 的 `textComponent` 和 `placeholder` 必须正确引用，否则无法输入
3. **ScrollRect 引用**：`content` 和 `viewport` 必须正确设置
4. **Mask 组件**：Viewport 的 Image 颜色不能是 `Color.clear`（alpha=0），否则子元素不可见
5. **初始隐藏**：Panel 类对象需要初始 `SetActive(false)`，ToggleBtn 需要初始隐藏（游戏中才显示）
6. **向后兼容**：所有 `[SerializeField]` 字段默认为 null，不指定时走代码创建路径
