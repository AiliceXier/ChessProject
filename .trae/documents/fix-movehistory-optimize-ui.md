# MoveHistoryUI 修复 + UI 视觉优化计划

## 一、MoveHistoryUI 一直为空的 Bug 修复

### 根本原因
`MoveHistoryUI.BuildPanel()` 中 Content 的 `VerticalLayoutGroup.childControlHeight = false`，导致 `ContentSizeFitter` 无法从子元素收集 `preferredHeight`，计算出的 content 总高度为 0，所有走棋记录行被压缩到不可见。

### 修复步骤

#### 步骤1：修复 Content 的 VerticalLayoutGroup
- 文件：`MoveHistoryUI.cs` 第178-185行
- 将 `contentVlg.childControlHeight` 改为 `true`
- 这样 VLG 才能从子元素的 LayoutElement.preferredHeight 收集高度信息

#### 步骤2：确保每行有 LayoutElement
- 文件：`MoveHistoryUI.cs` `AddMoveRow()` 方法
- 当 `childControlHeight = true` 时，VLG 会控制子元素高度，`SetSizeWithCurrentAnchors` 不再有效
- 需要给每行添加 `LayoutElement` 并设置 `preferredHeight = 28`（参考 lichess 的 28-32px 行高）

#### 步骤3：修复 RefreshDisplay 在面板未创建时的调用
- 文件：`MoveHistoryUI.cs` `RefreshDisplay()` 方法
- 添加 `_contentParent == null` 的早期返回检查
- 避免在面板未构建时尝试操作 null 的 contentParent

#### 步骤4：修复面板背景 raycastTarget
- 文件：`MoveHistoryUI.cs` 第91行
- 将面板背景 `raycastTarget` 改为 `true`，防止点击穿透

---

## 二、UI 视觉优化（参考 lichess/chess.com 设计）

> 原则：只修改视觉外观（颜色、间距、字号、布局尺寸），不改变功能逻辑

### 2.1 MoveHistoryUI 视觉优化

| 属性 | 当前值 | 优化后值 | 参考来源 |
|------|-------|---------|---------|
| 走法文字颜色 | `rgba(0.95,0.95,0.6)` 淡黄 | `rgba(0.80,0.63,0.43)` 棕金色 | lichess `#cda06e` |
| 序号颜色 | `rgba(0.55,0.55,0.55)` | `rgba(0.47,0.47,0.47)` | lichess `#777` |
| 行高 | 26px | 30px | lichess 28-32px |
| 面板宽度 | 260px (offsetMin -270) | 260px 不变 | — |
| 面板背景色 | `rgba(0.12,0.12,0.12,0.95)` | `rgba(0.10,0.10,0.12,0.95)` | 略深 |
| Header背景色 | `rgba(0.18,0.18,0.18)` | `rgba(0.15,0.15,0.17)` | 略深 |
| 偶数行背景 | `rgba(0.14,0.14,0.14)` | `rgba(0.13,0.13,0.15)` | — |
| 奇数行背景 | `rgba(0.2,0.2,0.2)` | `rgba(0.18,0.18,0.20)` | — |
| 走法字号 | 15px | 14px | lichess 13-14px |
| 序号字号 | 15px | 12px | lichess 12px |
| 标题字号 | 18px | 16px | 更紧凑 |
| 添加最后一行高亮 | 无 | 淡黄色背景 `rgba(255,255,100,0.15)` | lichess |

### 2.2 MainMenuUI 视觉优化

| 属性 | 当前值 | 优化后值 | 参考来源 |
|------|-------|---------|---------|
| 按钮高度 | 48px | 56px | chess.com 大卡片 |
| 按钮间距 | 10px | 12px | — |
| 面板内边距 | 40px | 50px | 更宽松 |
| Local Game 颜色 | `rgba(0.2,0.2,0.26)` | `rgba(0.18,0.32,0.52)` 蓝色 | chess.com |
| vs AI 颜色 | `rgba(0.2,0.2,0.26)` | `rgba(0.18,0.48,0.28)` 绿色 | chess.com |
| Online Game 颜色 | `rgba(0.15,0.25,0.35)` | `rgba(0.38,0.22,0.52)` 紫色 | chess.com |
| Leaderboard 颜色 | `rgba(0.2,0.3,0.2)` | `rgba(0.48,0.38,0.18)` 金色 | — |
| Create Room 颜色 | `rgba(0.2,0.5,0.3)` | `rgba(0.18,0.55,0.30)` 亮绿 | — |
| Join 颜色 | `rgba(0.2,0.3,0.6)` | `rgba(0.20,0.38,0.65)` 亮蓝 | — |
| 按钮文字字号 | 18px | 17px | 略紧凑 |
| 标题字号 | 28px | 32px | 更醒目 |

### 2.3 EvaluationBar 视觉优化

| 属性 | 当前值 | 优化后值 | 参考来源 |
|------|-------|---------|---------|
| 白色颜色 | `Color.white` | `new Color(0.94f,0.94f,0.94f)` | lichess `#f0f0f0` |
| 黑色颜色 | `Color.black` | `new Color(0.19f,0.19f,0.19f)` | lichess `#303030` |
| 映射函数 | 线性 `0.5+eval*0.05` | sigmoid `1/(1+exp(-eval*0.4))` | lichess |
| 数值颜色 | `Color.gray` | 动态：白方优势→黑字，黑方优势→白字 | lichess |
| 数值字号 | 10px | 11px | 略大 |
| 宽度 | 20px | 22px | chess.com |

### 2.4 DifficultySelector 视觉优化

| 属性 | 当前值 | 优化后值 |
|------|-------|---------|
| 面板宽度 | 300px | 320px |
| 面板高度 | 280px | 300px |
| 按钮高度 | 40px | 44px |
| 选中颜色 | `rgba(0.3,0.55,0.8)` | `rgba(0.29,0.48,0.71)` lichess蓝 |
| 未选中颜色 | `rgba(0.2,0.2,0.25)` | `rgba(0.22,0.22,0.28)` |
| 标题字号 | 22px | 20px |

### 2.5 CommandInputUI 视觉优化

| 属性 | 当前值 | 优化后值 |
|------|-------|---------|
| 面板背景色 | `rgba(0.12,0.12,0.12,0.95)` | `rgba(0.08,0.08,0.10,0.95)` 更深 |
| Header背景色 | `rgba(0.18,0.18,0.18)` | `rgba(0.14,0.14,0.16)` |
| 输出区文字颜色 | `rgba(0.8,0.8,0.8)` | `rgba(0.75,0.75,0.75)` |
| 成功颜色 | `rgba(0.4,0.9,0.4)` | `rgba(0.38,0.60,0.14)` lichess绿 |
| 错误颜色 | `rgba(0.9,0.3,0.3)` | `rgba(0.80,0.20,0.20)` |
| 标题字号 | 18px | 16px |

### 2.6 ChatUI 视觉优化

| 属性 | 当前值 | 优化后值 |
|------|-------|---------|
| 自己消息背景 | `rgba(0.2,0.4,0.7,0.5)` | `rgba(0.29,0.48,0.71,0.6)` lichess蓝 |
| 对手消息背景 | `rgba(0.3,0.3,0.3,0.5)` | `rgba(0.22,0.22,0.24,0.6)` |
| 标题字号 | 18px | 16px |

### 2.7 HintSystem 按钮视觉优化

| 属性 | 当前值 | 优化后值 |
|------|-------|---------|
| 按钮背景色 | `rgba(0.25,0.25,0.3,0.9)` | `rgba(0.29,0.48,0.71,0.9)` lichess蓝 |
| 高亮颜色 | 绿色半透明 | `rgba(0.38,0.60,0.14,0.5)` lichess绿 |

### 2.8 Toggle 按钮统一风格

| 按钮 | 当前颜色 | 优化后颜色 |
|------|---------|---------|
| Moves | `rgba(0.25,0.25,0.3,0.9)` | `rgba(0.22,0.22,0.28,0.9)` |
| Cmd | `rgba(0.25,0.25,0.3,0.9)` | `rgba(0.22,0.22,0.28,0.9)` |
| Chat | `rgba(0.25,0.25,0.3,0.9)` | `rgba(0.22,0.22,0.28,0.9)` |

---

## 三、实施顺序

1. **修复 MoveHistoryUI Bug**（步骤1-4）
2. **优化 MoveHistoryUI 视觉**
3. **优化 MainMenuUI 视觉**
4. **优化 EvaluationBar 视觉 + sigmoid 映射**
5. **优化 DifficultySelector 视觉**
6. **优化 CommandInputUI 视觉**
7. **优化 ChatUI 视觉**
8. **优化 HintSystem 视觉**
9. **统一 Toggle 按钮风格**
10. **编译验证 + 运行测试**
