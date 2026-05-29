---
name: workflow
description: Unity 项目开发工作流和常用操作
metadata: 
  node_type: memory
  type: project
  originSessionId: e6fbecdf-60db-43bb-b501-49cee55329c4
---

## 项目结构

- `Chess/` — Unity 客户端项目（用 Unity Editor 打开此目录）
- `ChessCloudCode/` — Cloud Code 服务端模块 (C# .NET)
- `leaderboard-server/` — 排行榜独立 API 服务器 (Node.js)

## Unity MCP

Unity MCP 运行在 localhost:8080，通过 JSON-RPC HTTP 调用。

### 常用 MCP 工具

```bash
# 刷新/编译
curl -s -X POST http://localhost:8080/tools -H "Content-Type: application/json" \
  -d '{"method":"refresh_unity","params":{"compile":"request"}}'

# 查看控制台日志
curl -s -X POST http://localhost:8080/tools -H "Content-Type: application/json" \
  -d '{"method":"read_console","params":{"lines":50}}'

# 诊断场景引用
curl -s -X POST http://localhost:8080/tools -H "Content-Type: application/json" \
  -d '{"method":"diagnose_scene","params":{}}'

# 管理组件属性
curl -s -X POST http://localhost:8080/tools -H "Content-Type: application/json" \
  -d '{"method":"manage_component","params":{"action":"set_property","target":"<fileID>","property":"fieldName","value":"<refFileID>"}}'
```

### 关键注意

- TMP 组件类型为 `TMP_Dropdown`、`TMP_InputField`、`TMP_Text`，不能用 Unity UI 的 `Dropdown`、`InputField`、`Text`
- 场景 YAML 编辑后会被 Unity reload 覆盖，优先改代码而非场景文件
- MCP `Accept: application/json` 头必须设置，否则返回 tools/list
- 包含非 ASCII 字符的 JSON 请求要写文件再 `-d @file` 传入 curl

## LeaderboardUI 组件引用

Scene: `ChessDemo.unity`，GameObject 上的 LeaderboardUI 组件需要 8 个 Inspector 引用：
- panel, contentParent, entryPrefab
- openButton, refreshButton, closeButton
- modeDropdown (TMP_Dropdown), playerNameInput (TMP_InputField)
- player, loadingIndicator, myRankText

Dropdown 选项为英文："all", "robot", "local", "online", "default"（带 "Option X: " 前缀，代码用正则去除）。
