# 重构方案：MainMenuUI 直接替换原始 UIPanel

## 核心思路

**不再创建新的独立面板，而是直接复用场景中已有的 UIPanel 及其子对象**。原始 UIPanel 已经有 Local Game Button、Robot Game Button、Create Button、Join Button、Lobby Code Input、Result Text、LeaderboardButton 这些按钮，它们已经绑定了 Player 的方法引用。新方案让 MainMenuUI 接管这些已有对象，只补充缺失的布局和功能。

## 当前问题分析

1. **NullRef at CreateSubButton (line 273)**：`ShowOnlineOptions()` 动态创建子面板时，`AddComponent<TextMeshProUGUI>()` 在同一 GameObject 上与 Image/Button 共存导致 NullRef（与之前 MoveHistoryUI 同样的 TMP 初始化问题）
2. **两套 UI 并存**：旧 UIPanel（场景预设）和新 MainMenuPanel（代码动态创建）同时存在，互相干扰
3. **布局比例不对**：新面板是代码创建的，没有参考原始 UIPanel 的布局
4. **Online 子面板每次重新创建/销毁**：不稳定，容易出错

## 重构方案

### 核心改变
**MainMenuUI 不再自己创建面板，而是接管场景中已有的 UIPanel**，重新组织其子对象的布局和可见性。

### Step 1: 重写 MainMenuUI - 接管 UIPanel

**MainMenuUI.cs** 完全重写：
- `Awake()` 不再创建新面板，而是找到场景中的 UIPanel
- 重新组织 UIPanel 的子对象布局：
  - 主菜单状态：显示 Local Game / vs AI / Online Game / Leaderboard 四个按钮
  - Online 子菜单状态：显示 Create Room / Join (含输入框) / Back
  - 游戏结果状态：显示 Result Text
- 使用状态机管理 UI 状态：`MainMenu` / `OnlineOptions` / `Hidden`

具体实现：
```
Awake():
  - 找到 UIPanel (player.uiPanel)
  - 获取所有子对象引用 (Local Game Button, Robot Game Button, Create Button, Join Button, Lobby Code Input, Result Text, LeaderboardButton)
  - 重新排列布局：添加 VerticalLayoutGroup 到 UIPanel
  - 初始状态 = MainMenu
  - 隐藏 UIPanel (等 Player.Start 调用 Show)
```

### Step 2: 重新组织 UIPanel 布局

UIPanel 已有的子对象：
1. `Local Game Button` - 已有 Button + TMP 文字
2. `Robot Game Button` - 已有 Button + TMP 文字（改名为 vs AI）
3. `Create Button` - 已有 Button + TMP 文字
4. `Join Button` - 已有 Button + TMP 文字
5. `Lobby Code Input` - 已有 TMP_InputField
6. `Result Text` - 已有 TMP
7. `LeaderboardButton` - 已有 Button + Text (旧 Text，非 TMP)

重新组织：
- **主菜单视图**：显示 Local Game Button, Robot Game Button, Online Game Button (新创建), LeaderboardButton
- **Online 子菜单视图**：显示 Create Button, Join Button + Lobby Code Input, Back Button (新创建)
- **结果视图**：显示 Result Text + 返回主菜单按钮

实际上，更好的方式是：不改变已有对象的层级，只控制它们的可见性和位置。

### Step 3: 简化方案 - 直接控制已有对象

最简方案：MainMenuUI 只做可见性切换，不重新布局。

**主菜单状态**：
- 显示：Local Game Button, Robot Game Button, LeaderboardButton
- 隐藏：Create Button, Join Button, Lobby Code Input
- 新增一个 "Online Game" 按钮（或复用某个按钮）
- Result Text 默认隐藏

**Online 子菜单状态**：
- 隐藏：Local Game Button, Robot Game Button, LeaderboardButton
- 显示：Create Button, Join Button, Lobby Code Input
- 新增一个 "Back" 按钮

**游戏结果状态**：
- 显示：Result Text
- 其他全部隐藏

### Step 4: 具体实现

```csharp
public class MainMenuUI : MonoBehaviour
{
    public Player player;
    
    // 引用场景中已有的对象
    private GameObject _panel;           // UIPanel
    private GameObject _localGameBtn;    // Local Game Button
    private GameObject _robotGameBtn;    // Robot Game Button  
    private GameObject _createBtn;       // Create Button
    private GameObject _joinBtn;         // Join Button
    private GameObject _lobbyCodeInput;  // Lobby Code Input
    private GameObject _resultText;      // Result Text
    private GameObject _leaderboardBtn;  // LeaderboardButton
    
    // 动态创建的对象
    private GameObject _onlineGameBtn;   // Online Game 按钮
    private GameObject _backBtn;         // Back 按钮
    
    private enum MenuState { MainMenu, OnlineOptions, Hidden }
    private MenuState _state = MenuState.Hidden;
    private string _pendingResult;
    
    void Awake() {
        // 不做任何事，等 Player.Start 设置引用后调用 Show
    }
    
    public void Initialize(GameObject panel) {
        _panel = panel;
        // 遍历子对象获取引用
        foreach (Transform child in panel.transform) {
            switch (child.name) {
                case "Local Game Button": _localGameBtn = child.gameObject; break;
                case "Robot Game Button": _robotGameBtn = child.gameObject; break;
                case "Create Button": _createBtn = child.gameObject; break;
                case "Join Button": _joinBtn = child.gameObject; break;
                case "Lobby Code Input": _lobbyCodeInput = child.gameObject; break;
                case "Result Text": _resultText = child.gameObject; break;
                case "LeaderboardButton": _leaderboardBtn = child.gameObject; break;
            }
        }
        
        // 修改 Robot Game Button 文字为 "vs AI"
        // 创建 Online Game 按钮
        // 创建 Back 按钮
        
        // 重新组织布局
        SetupLayout();
        
        _panel.SetActive(false);
    }
}
```

### Step 5: Player.cs 修改

- `Start()` 中：将 `uiPanel` 传给 `mainMenuUI.Initialize(uiPanel)`
- 移除所有 `uiPanel.SetActive(true/false)` 调用（由 MainMenuUI 管理）
- `CreateGame()` 中移除 `uiPanel.SetActive(false)`
- `StartLocalGame()` 中移除 `uiPanel.SetActive(false)`
- `StartRobotGameWithDifficulty()` 中移除 `uiPanel.SetActive(false)`
- `OnGameStart()` 中移除 `uiPanel.SetActive(false)`
- `LoadFromFen()` / `LoadFromPgn()` 中移除 `uiPanel.SetActive(false)`

### Step 6: 布局设计

UIPanel 使用 VerticalLayoutGroup，居中排列按钮：
- padding: 40/40/40/40
- spacing: 12
- childAlignment: MiddleCenter
- 每个按钮 preferredHeight: 48, preferredWidth: 280

按钮排列顺序（主菜单）：
1. Title "Chess" (新建 TMP)
2. Result Text (已有，默认隐藏)
3. Local Game Button (已有)
4. Robot Game Button / vs AI (已有，改文字)
5. Online Game Button (新建)
6. LeaderboardButton (已有)

按钮排列顺序（Online 子菜单）：
1. Title "Online Game" (新建 TMP)
2. Create Button (已有)
3. Lobby Code Input + Join Button (已有，水平排列)
4. Back Button (新建)

## 修改文件清单

1. **MainMenuUI.cs** - 完全重写，接管 UIPanel
2. **Player.cs** - 传递 uiPanel 给 MainMenuUI，移除 uiPanel.SetActive 调用
