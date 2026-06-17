const express = require('express');
const http = require('http');
const { WebSocketServer } = require('ws');
const Database = require('better-sqlite3');
const cors = require('cors');
const helmet = require('helmet');
const rateLimit = require('express-rate-limit');
const Joi = require('joi');

const app = express();
const PORT = process.env.PORT || 3000;
const CHAT_PORT = process.env.CHAT_PORT || 3001;
const ADMIN_KEY = 'leaderboard2024';
const DB_PATH = './data/leaderboard.db';

// ── Database ──────────────────────────────────────────────────
const db = new Database(DB_PATH);
db.pragma('journal_mode = WAL');
db.exec(`
  CREATE TABLE IF NOT EXISTS scores (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    player_name TEXT NOT NULL,
    score INTEGER NOT NULL,
    game_mode TEXT NOT NULL DEFAULT 'default',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(player_name, game_mode)
  )
`);

// ── Middleware ────────────────────────────────────────────────
app.use(helmet());
app.use(cors());
app.use(express.json());
const limiter = rateLimit({
  windowMs: 60 * 1000,
  max: 60,
  standardHeaders: true,
  legacyHeaders: false,
  skip: (req) => req.ip === '127.0.0.1' || req.ip === '::1',
  message: { success: false, message: '请求过于频繁，请稍后再试' }
});
app.use(limiter);
app.set('trust proxy', 1);

// ── Validation schemas ───────────────────────────────────────
const scoreSchema = Joi.object({
  player_name: Joi.string().min(1).max(20).required().messages({
    'string.empty': 'player_name 不能为空',
    'string.min': 'player_name 长度至少 1 个字符',
    'string.max': 'player_name 长度不能超过 20 个字符',
    'any.required': '缺少 player_name 字段'
  }),
  score: Joi.number().integer().positive().required().messages({
    'number.base': 'score 必须为数字',
    'number.integer': 'score 必须为整数',
    'number.positive': 'score 必须为正整数',
    'any.required': '缺少 score 字段'
  }),
  game_mode: Joi.string().max(30).optional().default('default')
});

// ── Helper: get player rank ──────────────────────────────────
function getPlayerRank(playerName, gameMode) {
  const all = db.prepare(
    'SELECT player_name, score FROM scores WHERE game_mode = ? ORDER BY score DESC'
  ).all(gameMode);
  const idx = all.findIndex(row => row.player_name === playerName);
  return idx === -1 ? null : idx + 1;
}

// ── Routes ───────────────────────────────────────────────────

// Health check
app.get('/ping', (req, res) => {
  res.json({ status: 'ok', time: new Date().toISOString() });
});

// Submit score
app.post('/score', (req, res) => {
  const { error, value } = scoreSchema.validate(req.body);
  if (error) {
    return res.status(400).json({ success: false, message: error.details[0].message });
  }

  const { player_name, score, game_mode } = value;

  const existing = db.prepare(
    'SELECT id, score FROM scores WHERE player_name = ? AND game_mode = ?'
  ).get(player_name, game_mode);

  if (existing) {
    db.prepare('UPDATE scores SET score = score + ?, created_at = CURRENT_TIMESTAMP WHERE id = ?')
      .run(score, existing.id);
  } else {
    db.prepare('INSERT INTO scores (player_name, score, game_mode) VALUES (?, ?, ?)')
      .run(player_name, score, game_mode);
  }

  const updated = db.prepare('SELECT score FROM scores WHERE player_name = ? AND game_mode = ?').get(player_name, game_mode);
  const rank = getPlayerRank(player_name, game_mode);
  res.json({ success: true, message: '分数已提交', data: { rank, total_score: updated.score } });
});

// Get leaderboard
app.get('/modes', (req, res) => {
  const rows = db.prepare('SELECT DISTINCT game_mode FROM scores ORDER BY game_mode').all();
  const modes = rows.map(r => r.game_mode);
  res.json({ success: true, data: modes });
});

app.get('/leaderboard/all', (req, res) => {
  const limit = Math.min(Math.max(parseInt(req.query.limit) || 10, 1), 100);

  const modes = db.prepare('SELECT DISTINCT game_mode FROM scores ORDER BY game_mode').all();
  const result = [];

  for (const m of modes) {
    const rows = db.prepare(
      'SELECT player_name, score, game_mode, created_at FROM scores WHERE game_mode = ? ORDER BY score DESC LIMIT ?'
    ).all(m.game_mode, limit);

    result.push({
      game_mode: m.game_mode,
      entries: rows.map((row, i) => ({
        rank: i + 1,
        player_name: row.player_name,
        score: row.score,
        game_mode: row.game_mode,
        created_at: row.created_at
      }))
    });
  }

  res.json({ success: true, data: result });
});

app.get('/leaderboard', (req, res) => {
  const limit = Math.min(Math.max(parseInt(req.query.limit) || 10, 1), 100);
  const game_mode = req.query.game_mode || 'default';

  const rows = db.prepare(
    'SELECT player_name, score, game_mode, created_at FROM scores WHERE game_mode = ? ORDER BY score DESC LIMIT ?'
  ).all(game_mode, limit);

  const data = rows.map((row, i) => ({
    rank: i + 1,
    player_name: row.player_name,
    score: row.score,
    game_mode: row.game_mode,
    created_at: row.created_at
  }));

  res.json({ success: true, data });
});

// Get player rank
app.get('/rank/:player_name', (req, res) => {
  const { player_name } = req.params;
  const game_mode = req.query.game_mode || 'default';

  const player = db.prepare(
    'SELECT player_name, score FROM scores WHERE player_name = ? AND game_mode = ?'
  ).get(player_name, game_mode);

  if (!player) {
    return res.json({ success: true, data: { rank: null, player_name, score: 0 } });
  }

  const rank = getPlayerRank(player_name, game_mode);
  res.json({
    success: true,
    data: { rank, player_name: player.player_name, score: player.score }
  });
});

// Update player name (rename all records)
app.put('/player/:old_name', (req, res) => {
  const { old_name } = req.params;
  const { new_name } = req.body;

  if (!new_name || typeof new_name !== 'string') {
    return res.status(400).json({ success: false, message: '缺少 new_name 字段' });
  }

  const trimmedNew = new_name.trim();
  if (trimmedNew.length < 1 || trimmedNew.length > 20) {
    return res.status(400).json({ success: false, message: 'new_name 长度必须在 1-20 个字符之间' });
  }

  if (old_name === trimmedNew) {
    return res.json({ success: true, message: '名称未变更' });
  }

  const oldExists = db.prepare('SELECT 1 FROM scores WHERE player_name = ? LIMIT 1').get(old_name);
  if (!oldExists) {
    return res.status(404).json({ success: false, message: '旧玩家名不存在' });
  }

  const newExists = db.prepare('SELECT 1 FROM scores WHERE player_name = ? LIMIT 1').get(trimmedNew);
  if (newExists) {
    return res.status(409).json({ success: false, message: '新玩家名已被占用' });
  }

  try {
    const update = db.prepare('UPDATE scores SET player_name = ? WHERE player_name = ?');
    const result = update.run(trimmedNew, old_name);
    res.json({ success: true, message: `已将 ${old_name} 更名为 ${trimmedNew}，共更新 ${result.changes} 条记录` });
  } catch (err) {
    console.error(err);
    res.status(500).json({ success: false, message: '重命名失败' });
  }
});

// Delete player (admin only)
app.delete('/score/:player_name', (req, res) => {
  if (req.headers['x-admin-key'] !== ADMIN_KEY) {
    return res.status(403).json({ success: false, message: '无权限，需要管理员密钥' });
  }

  const { player_name } = req.params;
  const result = db.prepare('DELETE FROM scores WHERE player_name = ?').run(player_name);

  if (result.changes === 0) {
    return res.json({ success: false, message: '玩家不存在' });
  }

  res.json({ success: true, message: `已删除玩家 ${player_name} 的记录` });
});

// ── Error handler ────────────────────────────────────────────
app.use((err, req, res, next) => {
  console.error(err.stack);
  res.status(500).json({ success: false, message: '服务器内部错误' });
});

// ── Start HTTP server ────────────────────────────────────────
app.listen(PORT, '0.0.0.0', () => {
  console.log(`Leaderboard API running on http://0.0.0.0:${PORT}`);
});

// ══════════════════════════════════════════════════════════════
// ── WebSocket Chat Server ────────────────────────────────────
// ══════════════════════════════════════════════════════════════

const chatServer = http.createServer();
const wss = new WebSocketServer({ server: chatServer });

const rooms = new Map();

wss.on('connection', (ws) => {
  let currentRoom = null;
  let playerName = null;

  ws.on('message', (raw) => {
    let msg;
    try {
      msg = JSON.parse(raw);
    } catch {
      ws.send(JSON.stringify({ type: 'error', message: 'Invalid JSON' }));
      return;
    }

    switch (msg.type) {
      case 'join': {
        const room = msg.room;
        playerName = String(msg.player || 'Anonymous').slice(0, 20);

        if (!room || room.length < 1) {
          ws.send(JSON.stringify({ type: 'error', message: 'Room ID required' }));
          return;
        }

        if (currentRoom) {
          const oldRoom = rooms.get(currentRoom);
          if (oldRoom) {
            oldRoom.clients.delete(ws);
            if (oldRoom.clients.size === 0) rooms.delete(currentRoom);
          }
        }

        currentRoom = room;
        if (!rooms.has(room)) {
          rooms.set(room, { clients: new Map() });
        }
        rooms.get(room).clients.set(ws, { name: playerName });

        ws.send(JSON.stringify({ type: 'joined', room, player: playerName }));

        const roomData = rooms.get(room);
        const members = [];
        for (const [, info] of roomData.clients) {
          members.push(info.name);
        }
        const joinMsg = JSON.stringify({
          type: 'chat',
          sender: 'System',
          message: `${playerName} joined`,
          members
        });
        for (const [client] of roomData.clients) {
          if (client !== ws && client.readyState === 1) {
            client.send(joinMsg);
          }
        }
        break;
      }

      case 'chat': {
        if (!currentRoom) {
          ws.send(JSON.stringify({ type: 'error', message: 'Not in a room' }));
          return;
        }
        const text = String(msg.message || '').slice(0, 500);
        if (!text) return;

        const roomData = rooms.get(currentRoom);
        if (!roomData) return;

        const chatMsg = JSON.stringify({
          type: 'chat',
          sender: playerName || 'Anonymous',
          message: text,
          timestamp: Date.now()
        });
        for (const [client] of roomData.clients) {
          if (client !== ws && client.readyState === 1) {
            client.send(chatMsg);
          }
        }
        break;
      }

      default:
        ws.send(JSON.stringify({ type: 'error', message: `Unknown type: ${msg.type}` }));
    }
  });

  ws.on('close', () => {
    if (currentRoom) {
      const roomData = rooms.get(currentRoom);
      if (roomData) {
        roomData.clients.delete(ws);
        if (roomData.clients.size > 0 && playerName) {
          const leaveMsg = JSON.stringify({
            type: 'chat',
            sender: 'System',
            message: `${playerName} left`
          });
          const members = [];
          for (const [, info] of roomData.clients) {
            members.push(info.name);
          }
          for (const [client] of roomData.clients) {
            if (client.readyState === 1) {
              client.send(leaveMsg);
            }
          }
        } else {
          rooms.delete(currentRoom);
        }
      }
    }
  });
});

chatServer.listen(CHAT_PORT, '0.0.0.0', () => {
  console.log(`Chat WebSocket server running on ws://0.0.0.0:${CHAT_PORT}`);
});
