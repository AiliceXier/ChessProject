const express = require('express');
const Database = require('better-sqlite3');
const cors = require('cors');
const helmet = require('helmet');
const rateLimit = require('express-rate-limit');
const Joi = require('joi');

const app = express();
const PORT = process.env.PORT || 3000;
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
    if (score > existing.score) {
      db.prepare('UPDATE scores SET score = ?, created_at = CURRENT_TIMESTAMP WHERE id = ?')
        .run(score, existing.id);
    }
  } else {
    db.prepare('INSERT INTO scores (player_name, score, game_mode) VALUES (?, ?, ?)')
      .run(player_name, score, game_mode);
  }

  const rank = getPlayerRank(player_name, game_mode);
  res.json({ success: true, message: '分数已提交', data: { rank } });
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
    return res.json({ success: false, message: '玩家不存在' });
  }

  const rank = getPlayerRank(player_name, game_mode);
  res.json({
    success: true,
    data: { rank, player_name: player.player_name, score: player.score }
  });
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

// ── Start ────────────────────────────────────────────────────
app.listen(PORT, '0.0.0.0', () => {
  console.log(`Leaderboard API running on http://0.0.0.0:${PORT}`);
});
