# Fix All UI Issues - Implementation Plan

## Problem Summary

1. **MoveHistory panel blank** - 日志确认数据正确（entries=3, contentHeight=100），但面板只显示 Header，行内容不可见
2. **主菜单按钮大小不一致** - 场景预设按钮（Local Game, Robot Game, Leaderboard, Create, Join, Lobby Code Input）sizeDelta=160x30，而代码动态创建的按钮（Online Game, Back）sizeDelta 由 LayoutElement 控制为 56 高度
3. **非 Online 模式未隐藏 Opponent Name 和 Lobby Code** - Player.cs 中 ShowInGameUI/HideInGameUI 已添加控制逻辑，但场景中的 GameObject 初始状态为 active
4. **AI 难度选择器尺寸** - 代码已设置 sizeDelta=280x280，anchoredPosition=Vector2.zero，但用户说没看到变化
5. **Cmd 面板尺寸** - 代码已设置 offsetMin/offsetMax，但用户说没看到变化

## Root Cause Analysis

### 核心问题：场景预设 vs 代码动态创建

- **UIPanel 上的按钮**（Local Game, Robot Game, Create, Join, Leaderboard, Lobby Code Input）是**场景预设（Scene Prefab）**，它们的 RectTransform 和 LayoutElement 在场景中已有固定值。代码中的 `SetupButtonLayout` 虽然设置了 LayoutElement.preferredHeight=56，但**没有修改 RectTransform.sizeDelta**。由于这些按钮的 anchorMin/anchorMax 都是 (0.5, 0.5)（绝对定位），LayoutElement 对它们不起作用，实际大小由 sizeDelta 决定。

- **动态创建的按钮**（Online Game, Back, WaitingPanel 等）是在 `Initialize()` 中通过代码创建的，完全受 LayoutElement 和 VerticalLayoutGroup 控制。

- **PlayerUIPanel 上的文本**（Opponent Name Text, Lobby Code Text）初始 activeSelf=true，Player.cs 的 ShowInGameUI 会在游戏开始时设置 active 状态，但用户可能在非游戏状态也看到它们。

- **DifficultySelector 和 CommandInputUI** 是运行时动态创建的，代码已设置正确尺寸，但用户说没看到变化。可能原因：
  - 用户测试时这些面板还没被创建（需要点击才会 EnsurePanel）
  - 或者这些面板的尺寸设置正确，但用户期望的是不同的视觉效果

### MoveHistory 空白问题

日志显示：
- BuildPanel complete
- Panel built successfully, _contentRt=True
- RefreshDisplay complete, _entries.Count=3, contentHeight=100
- Row 1: f3 | Nc6
- Row 2: f4 | Nf6
- Row 3: f5 | Ne5 (last)

数据完全正确，但面板空白。关键问题：

1. **ScrollRect.viewport 设置**：`scrollRt` 是 scrollObj 的 RectTransform，但 scrollObj 本身没有明确的尺寸。scrollObj 是 VerticalLayoutGroup 的子元素，由 LayoutElement 控制。

2. **Content 的 anchor 设置**：`_contentRt.anchorMin = new Vector2(0f, 1f); _contentRt.anchorMax = new Vector2(1f, 1f);` 这是顶部锚定，pivot=(0.5,1)。当 contentHeight=100 时，_contentRt 的 rect 应该是从顶部向下延伸 100 像素。

3. **Row 的 offset 计算**：
   - rowIndex=0: yPos=4, offsetMin=(4, -34), offsetMax=(-4, -4)
   - 这意味着 Row 的顶部在 y=-4，底部在 y=-34
   - 但 Content 的 pivot 是 (0.5, 1)，anchor 也是 (0,1)-(1,1)，所以 Content 的本地坐标系原点在顶部中心
   - Row 作为 Content 的子物体，其 offset 是相对于 Content 的

4. **关键问题：Content 的 sizeDelta 设置**：
   ```csharp
   _contentRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalHeight);
   ```
   当 anchorMin.y == anchorMax.y == 1f 时，SetSizeWithCurrentAnchors 会设置 sizeDelta.y = totalHeight。这是正确的。

5. **但 ScrollRect 的 viewport 可能没有正确裁剪**：
   - scrollObj 的 LayoutElement 设置了 flexibleHeight=1，minHeight=60
   - 但 scrollObj 本身没有 Image/Mask 的 showMaskGraphic=false
   - 实际上代码有：`scrollObj.AddComponent<Mask>().showMaskGraphic = false;`

6. **最可能的根本原因：Row 的 anchor 设置与 Content 的 pivot 不匹配**
   
   Content: anchorMin=(0,1), anchorMax=(1,1), pivot=(0.5,1)
   Row: anchorMin=(0,1), anchorMax=(1,1), pivot=(0.5,1)
   
   Row 的 offsetMin=(4, -34), offsetMax=(-4, -4)
   
   在 pivot=(0.5,1) 的坐标系中：
   - offsetMax.y = -4 表示 Row 的顶部在 Content 顶部下方 4 像素
   - offsetMin.y = -34 表示 Row 的底部在 Content 顶部下方 34 像素
   
   这是正确的。Row 应该在 Content 内部可见。

7. **另一个可能原因：ScrollRect 的 content 尺寸没有正确更新**
   代码调用了 `LayoutRebuilder.ForceRebuildLayoutImmediate(_contentRt);`，但 ScrollRect 可能需要额外的刷新。

8. **最可能的原因：Row 被创建时，Content 的尺寸还没有更新，导致 Row 的位置计算错误**
   但代码是在 UpdateContentSize 之后创建 Row 的，所以这应该不是问题。

9. **真正的问题可能是：ScrollRect 的 viewport 尺寸为 0 或负数**
   scrollObj 作为 VerticalLayoutGroup 的子元素，其尺寸由 LayoutElement 控制。但 VerticalLayoutGroup 在 UIPanel 上，UIPanel 是 full-screen (anchorMin=(0,0), anchorMax=(1,1))。VerticalLayoutGroup 会分配空间给各个子元素。

   等等，MoveHistoryPanel 不是 UIPanel 的子元素！它是 Canvas 的直接子元素！
   
   看代码：
   ```csharp
   _panel = new GameObject("MoveHistoryPanel");
   _panel.transform.SetParent(canvasTr, false);
   ```
   
   MoveHistoryPanel 是 Canvas 的直接子元素，不是 UIPanel 的子元素。这意味着它的尺寸不受 UIPanel 的 VerticalLayoutGroup 控制。

   MoveHistoryPanel 的 RectTransform：
   - anchorMin=(1,0), anchorMax=(1,1)
   - offsetMin=(-270, 50), offsetMax=(-10, -10)
   
   这意味着面板右侧对齐 Canvas 右侧，底部距离 Canvas 底部 50 像素，顶部距离 Canvas 顶部 10 像素，左侧距离 Canvas 右侧 270 像素，右侧距离 Canvas 右侧 10 像素。

   面板高度 = Canvas 高度 - 10 - 50 = Canvas 高度 - 60
   面板宽度 = 260

   在 800x600 的参考分辨率下，面板高度约为 540 像素。

   面板内部有 VerticalLayoutGroup，子元素是 Header (height=36) 和 ScrollView (flexibleHeight=1)。
   
   ScrollView 的高度 = 540 - 36 = 504 像素。这是正确的。

   但等等，VerticalLayoutGroup 的 padding 是 (0,0,0,0)，spacing=0。所以 ScrollView 确实应该有约 504 像素的高度。

   那么为什么 Row 不可见？

   **可能原因：Row 的 Image 颜色与背景相同，或者 alpha 值太低**
   - evenRowColor = (0.13, 0.13, 0.15, 0.9)
   - oddRowColor = (0.18, 0.18, 0.20, 0.9)
   - 这些颜色与 panelColor = (0.10, 0.10, 0.12, 0.95) 非常接近
   - 虽然有一定对比度，但可能不够明显

   **另一个可能原因：TextMeshProUGUI 的文本没有正确渲染**
   但日志显示 Row 被正确创建，而且 TextMeshProUGUI 在 active 的 GameObject 上添加应该没问题。

   **另一个可能原因：ScrollRect 的 verticalNormalizedPosition = 0f 导致内容被滚动到底部**
   当 contentHeight=100 而 viewportHeight=504 时，content 比 viewport 小，verticalNormalizedPosition=0 应该显示全部内容。

   **另一个可能原因：Row 的 anchor 和 pivot 设置导致它们在 viewport 坐标系中的位置错误**
   
   让我重新计算：
   - Content: anchorMin=(0,1), anchorMax=(1,1), pivot=(0.5,1), sizeDelta=(width, 100)
   - 由于 anchorMin.y == anchorMax.y == 1，Content 的顶部与父物体（ScrollView/Viewport）的顶部对齐
   - Content 的本地坐标：顶部在 y=0，底部在 y=-100
   
   - Row 0: offsetMin=(4, -34), offsetMax=(-4, -4)
   - Row 的顶部在 Content 本地坐标 y=-4，底部在 y=-34
   - 这意味着 Row 0 在 Content 顶部下方 4 到 34 像素处
   
   这是正确的！Row 应该在 viewport 内可见。

   **那么问题到底是什么？**

   考虑到用户说"move history有相关UI但是一直没有显示具体的历史棋盘"，并且截图显示面板是打开的但内容区域是空的。

   我怀疑问题可能是：
   1. **ScrollRect 的 movementType 导致内容被弹回或隐藏**
   2. **Mask 组件没有正确工作**
   3. **CanvasRenderer 没有正确初始化**
   4. **最可能：Content 的 RectTransform 尺寸在创建 Row 时还没有更新**

   等等，还有一个重要线索：用户说"move history有相关UI但是一直没有显示具体的历史棋盘"。这里的"历史棋盘"可能是指用户期望看到类似棋盘的布局，而不是简单的文本列表。但代码确实是创建文本列表。

   让我重新思考。日志显示一切正常，但 UI 不显示。这可能是一个**渲染顺序**或**层级**问题。

   实际上，我突然想到一个可能性：**Row 被创建为 Content 的子元素，但 Content 是 ScrollView 的子元素。ScrollView 有一个 Mask 组件。如果 Mask 的 showMaskGraphic=false 但 Mask 本身没有正确初始化，可能导致所有子元素被裁剪掉。**

   但代码中：`scrollObj.AddComponent<Mask>().showMaskGraphic = false;` 这是正确的。

   **另一个可能性：ScrollRect 的 viewport 引用的是 scrollRt（scrollObj 的 RectTransform），但 scrollObj 同时又是 Mask 的宿主。这可能导致冲突。**

   不，这是标准做法。

   **让我考虑一个更简单的可能性：用户看到的空白是因为 Row 的颜色和背景太接近，肉眼难以分辨。**

   evenRowColor = new Color(0.13f, 0.13f, 0.15f, 0.9f) - 非常深的灰色
   oddRowColor = new Color(0.18f, 0.18f, 0.20f, 0.9f) - 稍浅的深灰色
   panelColor = new Color(0.10f, 0.10f, 0.12f, 0.95f) - 更深的灰色

   对比度确实很小。而且 moveColor = new Color(0.80f, 0.63f, 0.43f) 是金色，应该可见。

   但如果 TextMeshProUGUI 没有正确渲染文本...

   **等等！我突然想到一个关键问题：TextMeshProUGUI 的文本在某些情况下可能不显示，如果字体材质或 shader 有问题。但 TMP 组件在 active 的 GameObject 上添加时，应该会自动创建正确的材质。**

   让我换一个思路。也许问题不是 MoveHistory 本身，而是**用户点击 Toggle 按钮后，面板显示但内容不可见，因为内容被创建在错误的位置**。

   我注意到一个细节：在 console.txt 中，当 Toggle 被调用时：
   1. Show called, _panel exists=False
   2. EnsurePanel - building panel for first time
   3. BuildPanel complete
   4. Panel built successfully, _contentRt=True
   5. RefreshDisplay called, _contentRt=True, _board=True
   6. No moves yet, showing label
   7. UpdateContentSize: entries=1, totalHeight=38

   然后用户关闭面板，再次打开：
   1. Show called, _panel exists=True
   2. RefreshDisplay called, _contentRt=True, _board=True
   3. ClearEntries removed 1 entries
   4. ExecutedMoves: count=6
   5. Rendering 6 moves in 3 rows
   6. Row 1: f3 | Nc6
   7. Row 2: f4 | Nf6
   8. Row 3: f5 | Ne5 (last)
   9. UpdateContentSize: entries=3, totalHeight=100
   10. RefreshDisplay complete, _entries.Count=3, contentHeight=100

   一切看起来都正常。但用户说看不到内容。

   **最后一个可能性：面板的层级（sorting order）问题，或者有其他 UI 元素遮挡了 MoveHistoryPanel。**

   但 MoveHistoryPanel 是 Canvas 的直接子元素，而 UIPanel 也是 Canvas 的直接子元素。如果 UIPanel 在 MoveHistoryPanel 之后渲染，它可能会遮挡 MoveHistoryPanel。

   不过 MoveHistoryPanel 的 anchor 是右侧 (1,0)-(1,1)，而 UIPanel 是全屏。如果 UIPanel 在 Hierarchy 中位于 MoveHistoryPanel 之后，它会覆盖 MoveHistoryPanel。

   但用户说"move history有相关UI"，说明面板是可见的，只是内容区域空白。

   **我决定采用一个更激进的方法：完全重写 MoveHistory 的显示逻辑，使用更简单、更可靠的方式。**

   具体来说：
   1. 不使用 ScrollRect，因为内容通常不会太多（一盘棋最多几百步）
   2. 直接在 Panel 内部使用 VerticalLayoutGroup 放置所有行
   3. 或者，保持 ScrollRect 但使用 ContentSizeFitter 自动调整 Content 尺寸
   4. 增加颜色对比度，确保内容可见
   5. 添加边框或分隔线，使行更明显

## Implementation Plan

### Step 1: Fix MoveHistory Panel - Complete Rewrite with Simpler Layout

**目标**：确保 MoveHistory 内容始终可见

**方案**：
1. 保留 ScrollRect 结构（因为内容可能超出面板）
2. 在 Content 上添加 ContentSizeFitter，自动根据内容调整高度
3. 每个 Row 使用 LayoutElement 控制高度，而不是绝对定位
4. 增加行与背景的颜色对比度
5. 添加调试可视化（如边框）确保 Row 确实被创建

**代码修改**：
- 修改 BuildScrollView：在 Content 上添加 ContentSizeFitter
- 修改 AddMoveRow：使用 LayoutElement + HorizontalLayoutGroup 替代绝对定位
- 修改 AddLabelEntry：同样使用 LayoutElement
- 增加颜色对比度
- 移除 UpdateContentSize 中的手动 SetSizeWithCurrentAnchors，改为依赖 ContentSizeFitter

### Step 2: Fix Main Menu Button Sizes via MCP

**目标**：让所有场景预设按钮的高度统一为 56

**方案**：使用 MCP 直接修改场景中按钮的 RectTransform.sizeDelta

**需要修改的对象**：
- Canvas/UIPanel/Local Game Button: sizeDelta.y = 56
- Canvas/UIPanel/Robot Game Button: sizeDelta.y = 56
- Canvas/UIPanel/Create Button: sizeDelta.y = 56
- Canvas/UIPanel/Join Button: sizeDelta.y = 56
- Canvas/UIPanel/LeaderboardButton: sizeDelta.y = 56
- Canvas/UIPanel/Lobby Code Input: sizeDelta.y = 44 (保持与代码一致)

**注意**：这些按钮的 anchor 是 (0.5, 0.5)，所以需要同时调整 anchoredPosition 以避免重叠。

当前按钮位置（anchoredPosition）：
- Local Game: (3, 42)
- Robot Game: (3, -5)
- Create: (0, 89)
- Join: (0, -92)
- Leaderboard: (0, -140)
- Lobby Code Input: (0, -58)
- Result Text: (2, 141)

如果按钮高度从 30 增加到 56，间距需要调整。但注意这些按钮在 MainMenuUI 中大部分时间是隐藏/显示切换的：
- MainMenu 状态显示：Local, Robot, Online, Leaderboard
- OnlineOptions 状态显示：Create, Join, LobbyCode, Back

由于 MainMenuUI 使用 VerticalLayoutGroup 控制布局（在 Initialize 中 SetupLayout 设置了 VLG），但场景预设按钮的 RectTransform 是 absolute positioning (anchor=0.5,0.5)。这意味着 VerticalLayoutGroup **不会**移动这些按钮的位置！

等等，让我重新检查。在 MainMenuUI.Initialize 中：
1. 获取了 _panel（即 UIPanel）
2. 调用 SetupLayout()
3. SetupLayout 中：`_panel.GetComponent<VerticalLayoutGroup>()` 或添加新的 VLG
4. VLG 设置 childControlWidth=true, childControlHeight=false

但场景预设按钮的 anchorMin/anchorMax 是 (0.5, 0.5)，这意味着它们是**绝对定位**。VerticalLayoutGroup 只对**相对定位**（anchorMin != anchorMax）的子元素有效。

所以 VerticalLayoutGroup 对这些场景预设按钮**完全不起作用**！按钮的位置完全由它们的 anchoredPosition 决定。

这意味着：
1. 按钮高度从 30 增加到 56 后，它们会重叠（因为间距是由 anchoredPosition 决定的，约 47 像素）
2. 需要重新计算并调整每个按钮的 anchoredPosition

**新的布局方案**：
- 主菜单按钮（Local, Robot, Online, Leaderboard）垂直排列，间距 12 像素
- 在线菜单按钮（Create, Join, LobbyCode, Back）垂直排列

计算主菜单布局（以中心为原点）：
- 4 个按钮，每个高度 56，间距 12
- 总高度 = 4*56 + 3*12 = 224 + 36 = 260
- 第一个按钮顶部 = 260/2 - 56 = 74
- Local Game: y = 74 - 28 = 46
- Robot Game: y = 46 - 56 - 12 = -22
- Online Game: y = -22 - 56 - 12 = -90
- Leaderboard: y = -90 - 56 - 12 = -158

但 Online Game 是动态创建的，不受场景预设影响。所以只需要调整场景预设按钮：
- Local Game: y = 46
- Robot Game: y = -22
- Leaderboard: y = -158

在线菜单布局：
- Create: y = 74
- Lobby Code Input: y = 74 - 56 - 12 = 6 (但 Lobby Code Input 高度是 44)
- Join: y = 6 - 44 - 12 = -50
- Back: y = -50 - 56 - 12 = -118

但 Back 是动态创建的，不需要调整场景预设。

**简化方案**：由于 MainMenuUI 使用 SetActive 切换按钮显示，且按钮位置是固定的，我们可以：
1. 保持按钮的 anchoredPosition 不变
2. 只增加 sizeDelta.y 到 56
3. 由于当前间距约 47 像素（42 到 -5 是 47），而 56 > 47，按钮会重叠

所以需要调整位置。让我重新计算：

主菜单状态显示的按钮（按当前 anchoredPosition.y 排序）：
- Result Text: 141 (最上面，但通常隐藏)
- Create: 89 (在线菜单，隐藏)
- Local Game: 42
- Robot Game: -5
- Lobby Code Input: -58 (在线菜单，隐藏)
- Join: -92 (在线菜单，隐藏)
- Leaderboard: -140

如果所有按钮高度变为 56：
- Local Game 底部 = 42 - 28 = 14
- Robot Game 顶部 = -5 + 28 = 23
- 重叠！14 < 23，重叠 9 像素

需要调整：
- Local Game: y = 42 + 9 = 51 (或者 Robot Game: y = -5 - 9 = -14)

更系统的调整：
- Local Game: y = 60
- Robot Game: y = -8 (60 - 28 - 28 = 4，需要间距 12，所以 4 - 12 - 28 = -36)

让我重新计算：
目标：按钮中心间距 = 56 + 12 = 68

主菜单：
- Local Game: y = 60
- Robot Game: y = 60 - 68 = -8
- Leaderboard: y = -8 - 68 = -76

在线菜单：
- Create: y = 60
- Lobby Code Input: y = 60 - 68 = -8 (但 Lobby Code Input 高度是 44，中心间距可以小一点)
- 如果 Lobby Code Input 高度 44，间距 12：60 - 28 - 12 - 22 = -2
- Join: y = -2 - 22 - 12 - 28 = -64

这变得复杂了。让我采用一个更简单的方法：

**方案 A：使用 MCP 修改场景预设按钮的 RectTransform，使其适配新的高度**

主菜单按钮（Local, Robot, Leaderboard）：
- 高度 56，间距 12
- Local: anchoredPosition.y = 60
- Robot: anchoredPosition.y = -8
- Leaderboard: anchoredPosition.y = -76

在线菜单按钮（Create, Join, LobbyCode）：
- Create: anchoredPosition.y = 60
- LobbyCode: anchoredPosition.y = -8 (height=44)
- Join: anchoredPosition.y = -64

Result Text: anchoredPosition.y = 120 (在 Create 上方)

**方案 B：在 MainMenuUI.SetupLayout 中，强制修改场景预设按钮的 RectTransform**

在代码中修改：
```csharp
private void SetupButtonLayout(GameObject btn, Color color)
{
    if (btn == null) return;
    var img = btn.GetComponent<Image>();
    if (img != null) img.color = color;

    var le = btn.GetComponent<LayoutElement>();
    if (le == null) le = btn.AddComponent<LayoutElement>();
    le.preferredHeight = 56;
    le.minHeight = 56;
    le.flexibleHeight = 0;

    // 强制修改 RectTransform
    var rt = btn.GetComponent<RectTransform>();
    rt.sizeDelta = new Vector2(rt.sizeDelta.x, 56);
}
```

但这样只能修改高度，不能修改位置避免重叠。

**最终决定**：采用方案 A + B 结合：
1. 代码中修改 sizeDelta
2. 使用 MCP 修改场景中的 anchoredPosition

### Step 3: Hide Opponent Name and Lobby Code in Non-Online Modes

**目标**：非 Online 模式下不显示 Opponent Name 和 Lobby Code

**当前状态**：
- Player.cs 的 ShowInGameUI 已设置：`lobbyCodeText.gameObject.SetActive(isOnline)` 和 `opponentNameText.gameObject.SetActive(isOnline)`
- Player.cs 的 HideInGameUI 已设置：`lobbyCodeText.gameObject.SetActive(false)` 和 `opponentNameText.gameObject.SetActive(false)`

**问题**：场景中的 GameObject 初始 activeSelf=true。如果游戏启动时这些文本显示，可能是因为 HideInGameUI 没有被及时调用，或者 Start 中的调用顺序有问题。

检查 Player.Start：
```csharp
HideInGameUI();  // 第 114 行
if (mainMenuUI != null) mainMenuUI.Show();  // 第 115 行
```

HideInGameUI 会在 Start 时被调用，应该能正确隐藏这些文本。

但用户说"没有看到变化"，可能是因为：
1. 用户测试时这些文本本来就没有显示（因为 HideInGameUI 已经隐藏了它们）
2. 或者用户期望的是完全不同的行为

**验证**：使用 MCP 检查场景中这些 GameObject 的 activeSelf 状态。

### Step 4: Verify DifficultySelector and CommandInputUI Sizes

**目标**：确认这些面板的尺寸是否正确

**DifficultySelector**：
- 代码中：panelRt.sizeDelta = new Vector2(280, 280)
- 代码中：panelRt.anchoredPosition = Vector2.zero
- 这是动态创建的面板，只有调用 Show() 时才会创建

**CommandInputUI**：
- 代码中：panelRt.offsetMin = new Vector2(10, 10), offsetMax = new Vector2(360, 260)
- 宽度 = 350, 高度 = 250
- 左下角定位

用户说"没有看到变化"，可能是因为：
1. 用户没有触发这些面板的显示
2. 或者这些面板的尺寸确实正确，但用户期望的是不同的尺寸

**验证**：在代码中添加日志，确认面板创建时的尺寸。

### Step 5: Git Commit

每步修改后执行 git commit。

## Detailed Implementation Steps

### Step 1: Fix MoveHistory Panel

**修改 MoveHistoryUI.cs**：

1. 修改 BuildScrollView：
   - 在 Content 上添加 ContentSizeFitter
   - 使用 VerticalLayoutGroup 管理 Row 布局

2. 修改 AddMoveRow：
   - 不使用绝对定位
   - 使用 LayoutElement 设置高度
   - 使用 HorizontalLayoutGroup 管理单元格

3. 修改 AddLabelEntry：
   - 同样使用 LayoutElement

4. 增加颜色对比度

5. 添加更多调试日志

**代码**：

```csharp
private void BuildScrollView(Transform parent)
{
    var scrollObj = CreateUIGameObject("ScrollView", parent);
    var scrollLe = scrollObj.AddComponent<LayoutElement>();
    scrollLe.flexibleHeight = 1;
    scrollLe.minHeight = 60;
    var scrollRt = scrollObj.GetComponent<RectTransform>();
    scrollObj.AddComponent<CanvasRenderer>();
    var vpImg = scrollObj.AddComponent<Image>();
    vpImg.color = Color.clear;
    vpImg.raycastTarget = true;
    scrollObj.AddComponent<Mask>().showMaskGraphic = false;
    _scrollRect = scrollObj.AddComponent<ScrollRect>();
    _scrollRect.horizontal = false;
    _scrollRect.vertical = true;
    _scrollRect.movementType = ScrollRect.MovementType.Elastic;

    var contentObj = CreateUIGameObject("Content", scrollObj.transform);
    _contentRt = contentObj.GetComponent<RectTransform>();
    _contentRt.anchorMin = new Vector2(0f, 0f);
    _contentRt.anchorMax = new Vector2(1f, 1f);
    _contentRt.pivot = new Vector2(0.5f, 0.5f);
    _contentRt.offsetMin = Vector2.zero;
    _contentRt.offsetMax = Vector2.zero;

    var csf = contentObj.AddComponent<ContentSizeFitter>();
    csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

    var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
    vlg.childAlignment = TextAnchor.UpperCenter;
    vlg.childControlWidth = true;
    vlg.childControlHeight = false;
    vlg.childForceExpandWidth = true;
    vlg.childForceExpandHeight = false;
    vlg.spacing = RowSpacing;
    vlg.padding = new RectOffset((int)ContentPadding, (int)ContentPadding, (int)ContentPadding, (int)ContentPadding);

    _scrollRect.content = _contentRt;
    _scrollRect.viewport = scrollRt;
}

private void AddMoveRow(int num, string white, string black, bool isEven, bool isLast)
{
    var row = CreateUIGameObject($"Row_{num}", _contentRt);
    var rowLe = row.AddComponent<LayoutElement>();
    rowLe.preferredHeight = RowHeight;
    rowLe.minHeight = RowHeight;
    rowLe.flexibleHeight = 0;

    row.AddComponent<CanvasRenderer>();
    var rowImg = row.AddComponent<Image>();
    if (isLast)
        rowImg.color = new Color(1f, 1f, 0.4f, 0.3f); // 增加高亮透明度
    else
        rowImg.color = isEven ? new Color(0.13f, 0.13f, 0.15f, 1f) : new Color(0.22f, 0.22f, 0.24f, 1f); // 增加对比度
    rowImg.raycastTarget = false;

    var hlg = row.AddComponent<HorizontalLayoutGroup>();
    hlg.childAlignment = TextAnchor.MiddleLeft;
    hlg.childControlWidth = false;
    hlg.childControlHeight = false;
    hlg.childForceExpandWidth = false;
    hlg.childForceExpandHeight = false;
    hlg.spacing = 4;
    hlg.padding = new RectOffset(6, 6, 0, 0);

    AddTextCell(row.transform, $"{num}.", numColor, FontStyles.Normal, 30, 12);
    AddTextCell(row.transform, white, moveColor, FontStyles.Bold, 90, 14);
    AddTextCell(row.transform, black, moveColor, FontStyles.Bold, 90, 14);

    _entries.Add(row);
}

private void AddTextCell(Transform parent, string text, Color color, FontStyles style, float width, float fontSize)
{
    var cell = CreateUIGameObject("Cell", parent);
    var cellLe = cell.AddComponent<LayoutElement>();
    cellLe.preferredWidth = width;
    cellLe.minWidth = width;
    cellLe.flexibleWidth = 0;

    var cellRt = cell.GetComponent<RectTransform>();
    cellRt.sizeDelta = new Vector2(width, RowHeight);

    var tmp = cell.AddComponent<TextMeshProUGUI>();
    tmp.text = text;
    tmp.fontSize = fontSize;
    tmp.fontStyle = style;
    tmp.color = color;
    tmp.alignment = TextAlignmentOptions.MidlineLeft;
    tmp.raycastTarget = false;
}

private void AddLabelEntry(string text)
{
    var obj = CreateUIGameObject("Label", _contentRt);
    var objLe = obj.AddComponent<LayoutElement>();
    objLe.preferredHeight = RowHeight;
    objLe.minHeight = RowHeight;
    objLe.flexibleHeight = 0;

    var tmp = obj.AddComponent<TextMeshProUGUI>();
    tmp.text = text;
    tmp.fontSize = 14;
    tmp.alignment = TextAlignmentOptions.Center;
    tmp.color = new Color(0.5f, 0.5f, 0.5f);
    tmp.raycastTarget = false;
    _entries.Add(obj);
}

private void UpdateContentSize()
{
    if (_contentRt == null) return;
    // ContentSizeFitter 会自动处理，不需要手动设置
    Debug.Log("[MoveHistoryUI] UpdateContentSize: entries=" + _entries.Count);
}
```

### Step 2: Fix Main Menu Button Sizes

**修改 MainMenuUI.cs**：

在 SetupButtonLayout 中强制设置 RectTransform.sizeDelta：

```csharp
private void SetupButtonLayout(GameObject btn, Color color)
{
    if (btn == null) return;
    var img = btn.GetComponent<Image>();
    if (img != null) img.color = color;

    var le = btn.GetComponent<LayoutElement>();
    if (le == null) le = btn.AddComponent<LayoutElement>();
    le.preferredHeight = 56;
    le.minHeight = 56;
    le.flexibleHeight = 0;

    // 强制设置 RectTransform 尺寸
    var rt = btn.GetComponent<RectTransform>();
    if (rt != null)
    {
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, 56);
    }
}
```

**使用 MCP 修改场景预设按钮的位置**：

由于按钮高度从 30 增加到 56，需要调整 anchoredPosition 避免重叠。

主菜单状态（显示 Local, Robot, Online, Leaderboard）：
- Local Game: anchoredPosition = (3, 60)
- Robot Game: anchoredPosition = (3, -8)
- Leaderboard: anchoredPosition = (0, -76)

在线菜单状态（显示 Create, Join, LobbyCode, Back）：
- Create: anchoredPosition = (0, 60)
- Lobby Code Input: anchoredPosition = (0, -8)
- Join: anchoredPosition = (0, -64)

Result Text: anchoredPosition = (2, 120)

### Step 3: Verify Opponent Name / Lobby Code Visibility

**使用 MCP 检查并设置初始状态**：

如果场景中的 Opponent Name Text 和 Lobby Code Text 初始 activeSelf=true，可以使用 MCP 设置为 false。

但更好的做法是在 Player.Start 中确保它们被正确隐藏。检查 Start 方法：

```csharp
HideInGameUI();  // 第 114 行
```

HideInGameUI 中：
```csharp
if (lobbyCodeText != null) lobbyCodeText.gameObject.SetActive(false);
if (opponentNameText != null) opponentNameText.gameObject.SetActive(false);
```

这应该已经在 Start 时隐藏了它们。如果用户仍然看到它们，可能是因为：
1. Player 脚本没有正确执行
2. 或者这些引用为 null

添加日志确认：
```csharp
private void HideInGameUI()
{
    HideAllUIPanels();
    // ...
    Debug.Log($"[Player] HideInGameUI: lobbyCodeText={(lobbyCodeText!=null)}, opponentNameText={(opponentNameText!=null)}");
    if (lobbyCodeText != null) lobbyCodeText.gameObject.SetActive(false);
    if (opponentNameText != null) opponentNameText.gameObject.SetActive(false);
}
```

### Step 4: Verify DifficultySelector and CommandInputUI

**在代码中添加日志**：

DifficultySelector.BuildUI：
```csharp
Debug.Log($"[DifficultySelector] Panel sizeDelta={panelRt.sizeDelta}, anchoredPosition={panelRt.anchoredPosition}");
```

CommandInputUI.BuildPanel：
```csharp
Debug.Log($"[CommandInputUI] Panel offsetMin={panelRt.offsetMin}, offsetMax={panelRt.offsetMax}");
```

### Step 5: Git Commit

每步修改后执行：
```bash
git add -A
git commit -m "fix: ..."
```

## Execution Order

1. **修改 MoveHistoryUI.cs** - 重写布局逻辑
2. **修改 MainMenuUI.cs** - 强制设置 RectTransform.sizeDelta
3. **使用 MCP 修改场景按钮位置** - 调整 anchoredPosition
4. **使用 MCP 检查/设置 Opponent Name / Lobby Code 初始状态**
5. **在 DifficultySelector 和 CommandInputUI 中添加日志**
6. **测试并验证**
7. **Git commit**
