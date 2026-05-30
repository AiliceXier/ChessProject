# UI System Fix Plan

## Problem Summary

1. **NullReferenceException** in all UI components during Awake() - panels partially created
2. **Black boxes covering UI** - all panels visible by default (Awake creates active panels, Start hides them 1 frame later)
3. **Wrong panels shown** - clicking "Local Game" or "Create Room" shows DifficultySelector
4. **raycastTarget blocking** - panel backgrounds block clicks on elements behind them
5. **Missing leaderboard button** in MainMenuUI
6. **Toggle buttons overlap with panels** - no consistent layout

## Root Cause Analysis

### Core Issue: Panel Initialization Pattern
All 7 UI components use this pattern:
```csharp
Awake() { BuildUI(); }    // Panel created as ACTIVE (visible)
Start() { Hide(); }        // Hidden 1 frame later
```
Problems:
- Panel is visible for 1 frame before Start() runs
- If BuildUI() throws NullReferenceException, panel is partially created but visible
- Start() may not properly Hide() if _panel is in inconsistent state
- Multiple panels visible simultaneously = "black boxes"

### Why DifficultySelector Appears for Local/Online
DifficultySelector.Awake() creates its panel (visible). If BuildUI() throws at line 91,
the panel GameObject already exists (created at line 45) with a dark background Image.
Start() → Hide() should work, but the panel may flash visible or remain visible if
the exception corrupts component state.

### Why raycastTarget Blocks Clicks
All panel backgrounds have `raycastTarget = true` on their Image component.
When panels overlap, the topmost panel's background intercepts all mouse events,
preventing clicks on buttons and other interactive elements behind it.

## Fix Steps

### Step 1: Fix initialization pattern for all 7 UI components

**Change**: Create panels as INACTIVE from the start. Remove `Start() { Hide(); }`.

For each component, after creating the panel GameObject, immediately set it inactive:
```csharp
_panel = new GameObject("PanelName");
_panel.transform.SetParent(canvas.transform, false);
_panel.layer = 5;
_panel.SetActive(false);  // <-- KEY FIX: Start inactive!
// ... rest of setup ...
```

Remove `Start() { Hide(); }` from all components since panels are already inactive.

For toggle buttons (MoveHistoryUI, CommandInputUI, ChatUI), keep them in Awake()
since they should always be visible during gameplay. But add null-safety:
```csharp
private void Awake()
{
    var canvas = FindObjectOfType<Canvas>();
    if (canvas == null) return;
    BuildToggleButton(canvas.transform);
    // Panel created lazily on first Show()
}
```

**Files to modify**:
- `Assets/Chess/UI/MoveHistoryUI.cs`
- `Assets/Chess/UI/CommandInputUI.cs`
- `Assets/Chess/UI/DifficultySelector.cs`
- `Assets/Chess/UI/EvaluationBar.cs`
- `Assets/Chess/UI/HintSystem.cs`
- `Assets/Chess/UI/ChatUI.cs`
- `Assets/Chess/UI/MainMenuUI.cs` (keep active since it should show at startup)

### Step 2: Fix raycastTarget on all panel backgrounds

Set `raycastTarget = false` on background Images that should not block clicks.
Only keep `raycastTarget = true` on:
- Button images
- Input field backgrounds
- ScrollRect viewports (needed for scroll detection)

**Rule**: If the Image is just visual (background, decoration), set `raycastTarget = false`.

### Step 3: Fix panel positioning to prevent overlap

Current positions:
- MoveHistoryPanel: right side, full height (anchor 1,0 to 1,1, offset -270 to -10)
- CommandPanel: bottom-left, 40% width, 55% height (anchor 0,0 to 0.4,0.55)
- DifficultyPanel: center, 300x240 modal
- ChatPanel: bottom-left, 35% width, 50% height (anchor 0,0 to 0.35,0.5) — OVERLAPS with CommandPanel!
- MainMenuPanel: full screen
- EvalBar: left edge, thin bar
- HintBtn: bottom-left (10,10 to 110,44)
- ChatToggleBtn: bottom-left (10,50 to 110,84)
- CmdToggleBtn: right middle (1,0.5, offset -100,-50 to -10,-10)
- MoveHistoryBtn: right middle (1,0.5, offset -100,-20 to -10,20)

**Fix**: Reposition panels to avoid overlap:
- CommandPanel: left side, bottom 50% (anchor 0,0 to 0.38,0.5)
- ChatPanel: left side, bottom 50% (anchor 0,0 to 0.38,0.5) — same position, mutually exclusive with CommandPanel
- MoveHistoryPanel: right side, full height (keep current)
- DifficultyPanel: center modal (keep current)
- MainMenuPanel: full screen (keep current)

**Mutual exclusion**: When CommandInputUI.Show() is called, auto-hide ChatUI, and vice versa.

### Step 4: Add leaderboard button to MainMenuUI

Add a "Leaderboard" card after the "Online Game" card:
```csharp
CreateModeCard("Leaderboard", "View global rankings", cardColor, () =>
{
    player?.ShowLeaderboard();
});
```

Add `ShowLeaderboard()` method to Player.cs that toggles the existing LeaderboardPanel.

### Step 5: Fix game flow - hide all panels when starting a game

Add a helper method to Player.cs:
```csharp
private void HideAllUIPanels()
{
    if (moveHistoryUI != null) moveHistoryUI.Hide();
    if (commandInputUI != null) commandInputUI.Hide();
    if (chatUI != null) chatUI.Hide();
    if (difficultySelector != null) difficultySelector.Hide();
    if (hintSystem != null) { /* hint highlights auto-clear */ }
}
```

Call this in StartLocalGame(), StartRobotGameWithDifficulty(), OnGameStart().

Also ensure DifficultySelector.Show() is ONLY called from StartRobotGame(), not from any other path.

### Step 6: Fix toggle button layout - consistent toolbar

Create a consistent bottom toolbar with all toggle buttons:
- Position: bottom of screen, centered or aligned
- Buttons: [Hint] [Chat] [Cmd] [Moves]
- Each button toggles its respective panel

Current toggle buttons are scattered:
- MoveHistoryBtn: right middle
- CmdToggleBtn: right middle (below MoveHistoryBtn)
- ChatToggleBtn: bottom-left
- HintBtn: bottom-left (below ChatToggleBtn)

**Fix**: Reposition all toggle buttons to a consistent toolbar at the bottom:
- Hint: bottom-left (10, 10, 100, 34)
- Chat: bottom-left (120, 10, 100, 34)
- Cmd: bottom-left (230, 10, 80, 34)
- Moves: bottom-right (right-100, 10, 90, 34)

### Step 7: Fix EvaluationBar visibility

EvaluationBar should only be visible during gameplay (not on main menu).
Add Show()/Hide() methods and call them appropriately.

### Step 8: Fix MainMenuUI ShowWithResult after online game

When ShowWithResult is called after an online game ends, the _resultText
reference may be lost if ShowOnlineOptions() destroyed the panel.
Fix: Store result text and re-apply when rebuilding main menu.

## Implementation Order

1. Step 1 (initialization fix) - Most critical, fixes black boxes and wrong panels
2. Step 2 (raycastTarget) - Fixes click blocking
3. Step 5 (game flow) - Fixes DifficultySelector appearing for wrong modes
4. Step 3 (panel positioning) - Fixes overlap
5. Step 6 (toolbar layout) - Consistent button positions
6. Step 4 (leaderboard button) - Feature addition
7. Step 7 (EvalBar visibility) - Polish
8. Step 8 (ShowWithResult fix) - Edge case fix

## Files Modified

- `Assets/Chess/UI/MoveHistoryUI.cs` - Steps 1,2,3,6
- `Assets/Chess/UI/CommandInputUI.cs` - Steps 1,2,3,6
- `Assets/Chess/UI/DifficultySelector.cs` - Steps 1,2
- `Assets/Chess/UI/EvaluationBar.cs` - Steps 1,2,7
- `Assets/Chess/UI/HintSystem.cs` - Steps 1,2,6
- `Assets/Chess/UI/ChatUI.cs` - Steps 1,2,3,6
- `Assets/Chess/UI/MainMenuUI.cs` - Steps 1,2,4,8
- `Assets/Player.cs` - Steps 4,5,7
