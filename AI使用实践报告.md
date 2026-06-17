# 国际象棋项目中高效使用 AI 的实践报告

## 一、项目概述

本项目是基于 Unity 的国际象棋游戏，支持本地对战、人机对战和在线对战。开发过程中，我以 AI 编程助手（Claude / Trae）为核心工具，完成了架构设计、Bug 修复、功能开发和性能优化等全流程工作。

---

## 二、与 AI 的交流过程

### 2.1 怎么提要求

#### （1）从问题现象出发，提供完整上下文

不直接说"帮我修个 Bug"，而是提供复现路径和错误信息。例如修复 AI 卡死：

> **我的输入**：Robot Game 中白方走棋后，AI 一直显示 "AI Thinking..." 无响应。控制台无报错。请分析根因并修复。

AI 据此发现了 4 个关键问题：缺少异常处理、嵌套 Task.Run 线程池饥饿、FEN 克隆低效、boardSnapshot 非深拷贝。若只说"AI 卡住了"，AI 可能只会加 try-catch 草草了事。

#### （2）明确约束和范围

UI 重构时要求：

> **我的输入**：不要一次性重写，采用"Inspector 优先，代码兜底"策略。Inspector 中指定了场景对象引用就复用，没指定再代码创建。

产出渐进式方案，降低风险。

#### （3）资料让 AI 从网上找，我来审核

音效系统设计时：

> **我的输入**：推荐无版权的古典钢琴背景音乐来源，给出具体网址和推荐理由，我来审核。

AI 搜索推荐了 Musopen（非营利 CC0 机构）、Pixabay 等平台，并给出了 Erik Satie 的 Gnossienne No.1 等具体曲目建议。我审核后采纳了 Musopen 方案。

#### （4）多模态 LLM 用截图辅助

排行榜 UI 修复时，我直接截取 Unity 运行截图发给 AI：

> **我的输入**：[截图] 排行榜条目左侧 rank/name/score 列全部空白，右侧 mode 和 date 正常显示。

AI 通过截图直观发现问题，推断出 entryPrefab 中左侧子对象可能使用了中文名称（如"排名"而非"rank"），给出中英文双重匹配的修复方案。

---

### 2.2 交流中的迭代优化

#### （1）Plan + Git 控制大型修改

UI 重构采用"先出计划，分步实施，每步提交"的模式：

1. **让 AI 先输出详细计划**（存到 `.trae/documents/`），包含根因分析、修改步骤、文件清单、验证方法
2. **我审核计划**确认方向正确
3. **AI 按步骤执行**，每完成一个步骤就 `git add` + `git commit`
4. **我测试验证**通过后再进行下一步

例如在 MoveHistoryUI 修复中，经历了 3 轮迭代（childControlHeight → viewport 设置 → anchor 匹配），每轮都有独立的 commit，随时可以回退。

#### （2）从 Bug 修复到功能增强

修复"兵不能走"时，AI 分析后指出这不是 Bug（白王被将军，兵无法解将），而是缺少将军提示功能。这帮助我找到了正确的解决方向——添加将军视觉提示和走法过滤，而非修改走法验证逻辑。

---

## 三、怎么测试 AI 完成的结果

### 3.1 核心方法：添加日志，通过日志判断

我让 AI 在关键位置添加结构化日志，运行时通过 Console 面板判断逻辑是否正确执行。

**示例日志**：
```
[Leaderboard] Score submitted: Player_xxx -> 60 (mode: local, rank: #1)
[MoveAnimator] AnimateSyncBoard: found 32 pieces, 1 toCapture, 1 toMove
[Player] AI move: e2e4 (depth=3, eval=0.5)
[Check] White king checked at e1, highlighting...
```

**实际案例**：修复积分显示为 0 时，日志显示 `_currentScores["robot"]=0` 但 `_currentScores["local"]=60`，直接定位到是模式映射错误（"all" 被强制替换为 "robot"），而非积分未提交。

### 3.2 编译验证：使用 Subagent

Trae IDE 支持编译验证 Subagent，AI 修改代码后自动触发编译检查，发现缺少 using 语句、类型不匹配等编译错误，无需手动等待 Unity 编译。

### 3.3 功能场景走查

| 功能 | 测试方法 |
|------|---------|
| AI 走棋 | Robot Game 白方走 d3/d4，观察 AI 是否正常响应 |
| 吃子动画 | 走棋到吃子局面，观察被吃棋子是否正确消失 |
| 排行榜提交 | 完成一局游戏，打开排行榜检查分数是否正确显示 |
| 在线对战 | 两个客户端创建/加入房间，走棋同步 |
| 悔棋 | Local 模式走几步后点击 Undo，检查棋盘是否回退 |
| 将军提示 | 制造将军局面，检查王是否高亮、走法是否过滤 |

### 3.4 远程服务器验证

排行榜涉及服务端修改，用 curl/ssh 验证：

```bash
# 测试改名接口
curl -X PUT http://121.36.101.82:3000/player/OldName \
  -H "Content-Type: application/json" -d '{"new_name":"NewName"}'

# 检查服务器日志
ssh root@121.36.101.82 "pm2 logs leaderboard --lines 20"
```

---

## 四、怎么要求调整优化

### 4.1 精准反馈，提供证据

测试发现问题时，描述具体现象 + 日志/截图，而非笼统说"不对"。

### 4.2 要求分析根因

> **我的要求**：不要只修 MoveAnimator 表面 Bug，分析增量更新逻辑为什么失败，给出彻底方案。

AI 分析后发现：棋子 A 吃掉棋子 B 时，B 的位置在新状态中存在（被 A 占据），所以 B 不会被检测为"消失"。最终给出三步逻辑：识别差异 → 执行吃子 → 执行移动。

### 4.3 指定设计参考

> **我的要求**：参考 lichess 和 chess.com 风格，只改视觉外观（颜色、间距、字号），不改功能逻辑。

AI 据此给出 lichess 棕金色 `#cda06e`、sigmoid 评估条映射等具体方案。

### 4.4 利用计划文档审查

让 AI 先出计划存到 `.trae/documents/`，我审查后再执行。例如在积分系统修复中，计划文档暴露了 `last_insert_rowid` 用法错误、`SubmitOnlineScore` 缺少缓存更新，我在审查阶段就纠正了方向。

---

## 五、工具链与工作流

### 5.1 Unity MCP：AI 直接操作 Unity Editor

`.trae/mcp.json` 配置了 Unity MCP：

```json
{
  "unity-mcp": {
    "type": "http",
    "url": "http://localhost:8080/"
  }
}
```

AI 通过 MCP 直接操作 Unity Editor：创建 GameObject、修改组件属性、绑定 Inspector 引用、调整 RectTransform 布局。实现"AI 写代码 + AI 配场景"的闭环，无需我在 Unity 中手动拖拽配置。

### 5.2 权限白名单

`.claude/settings.local.json` 配置 AI 可执行的操作白名单（git、文件复制、Python 脚本等），在安全可控的前提下赋予 AI 自主性。

### 5.3 文档体系

- `.trae/documents/`：20+ 份计划文档，记录每个功能/修复的完整方案
- `.trae/specs/`：功能规格说明和任务清单

---

## 六、总结：我的工作流 Skill

我将以上经验总结为一个可复用的 AI 协作工作流：

1. **提需求**：现象 + 上下文 + 约束 + 参考资料（AI 搜索，我审核）
2. **出计划**：AI 先写详细计划文档，我审查确认方向
3. **分步实施**：按 Plan 分步骤执行，每步 git commit
4. **加日志测试**：关键位置加结构化日志，通过日志判断逻辑正确性
5. **编译验证**：Trae Subagent 自动编译检查
6. **截图反馈**：多模态 LLM 用截图直观描述 UI 问题
7. **MCP 操作**：AI 直接通过 MCP 操作 Unity Editor 配置场景
8. **迭代优化**：发现问题 → 精准反馈（日志/截图）→ AI 分析根因 → 修复 → 验证

这套工作流的核心是：**Plan 先行、日志驱动、分步验证、MCP 闭环**。
