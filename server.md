---
name: server
description: 排行榜 API 远程服务器连接和管理信息
metadata: 
  node_type: memory
  type: reference
  originSessionId: e6fbecdf-60db-43bb-b501-49cee55329c4
---

## 排行榜服务器

- **IP**: 121.36.101.82
- **用户**: root
- **项目路径**: /root/leaderboard-api
- **入口**: server.js (Express + better-sqlite3)
- **数据库**: /root/leaderboard-api/data/leaderboard.db
- **进程管理**: PM2，进程名 `leaderboard`
- **端口**: 3000

### 常用命令

```bash
# 上传新代码
scp leaderboard-server/server.js root@121.36.101.82:/root/leaderboard-api/

# 重启服务
ssh root@121.36.101.82 "pm2 restart leaderboard"

# 查看日志
ssh root@121.36.101.82 "pm2 logs leaderboard --lines 20"

# 操作数据库 (sqlite3 未安装，通过 node 操作)
ssh root@121.36.101.82 "cd /root/leaderboard-api && node -e \"
const Database = require('better-sqlite3');
const db = new Database('./data/leaderboard.db');
// ... SQL ...
db.close();
\""
```

### API 端点

- `GET /modes` — 获取所有游戏模式列表
- `GET /leaderboard?game_mode=X&limit=N` — 单模式排行榜
- `GET /leaderboard/all?limit=N` — 全模式排行榜
- `GET /rank/:player_name?game_mode=X` — 玩家排名
- `POST /score` — 提交分数 `{player_name, score, game_mode}`
