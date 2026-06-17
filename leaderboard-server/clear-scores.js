const Database = require('better-sqlite3');
const db = new Database('./data/leaderboard.db');
db.prepare('DELETE FROM scores').run();
console.log('All scores cleared');
db.close();
