module.exports = {
  apps: [{
    name: 'leaderboard',
    script: './server.js',
    cwd: '/root/leaderboard-api',
    instances: 1,
    autorestart: true,
    max_restarts: 10,
    error_file: './logs/err.log',
    out_file: './logs/out.log',
    log_date_format: 'YYYY-MM-DD HH:mm:ss',
    env: {
      NODE_ENV: 'production',
      PORT: 3000
    }
  }]
};
