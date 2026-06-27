"""
Generate professional SVG diagrams for the chess platform course design report.
No external dependencies needed — raw SVG.
"""

import os

def write_svg(filename, svg_content):
    path = os.path.join(r'D:\unity\my_chess\diagrams', filename)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, 'w', encoding='utf-8') as f:
        f.write(svg_content)
    print(f"Saved: {path}")

def system_architecture():
    """Overall system architecture diagram"""
    svg = '''<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 900 650" width="900" height="650">
  <defs>
    <marker id="arrow" markerWidth="8" markerHeight="6" refX="8" refY="3" orient="auto">
      <path d="M0,0 L8,3 L0,6 Z" fill="#555"/>
    </marker>
    <linearGradient id="headerGrad" x1="0" y1="0" x2="1" y2="0">
      <stop offset="0%" stop-color="#2c3e50"/>
      <stop offset="100%" stop-color="#34495e"/>
    </linearGradient>
  </defs>

  <!-- Title -->
  <rect x="0" y="0" width="900" height="38" fill="url(#headerGrad)" rx="3"/>
  <text x="450" y="25" text-anchor="middle" fill="white" font-family="Microsoft YaHei, sans-serif" font-size="16" font-weight="bold">图3-1  系统总体架构图</text>

  <!-- Client Layer -->
  <rect x="40" y="55" width="820" height="200" fill="#e8f4fd" stroke="#2196F3" stroke-width="1.5" rx="6"/>
  <text x="55" y="75" font-family="Microsoft YaHei, sans-serif" font-size="13" font-weight="bold" fill="#1565C0">Unity 客户端 (C#)</text>

  <!-- Client modules -->
  <rect x="60" y="88" width="180" height="42" fill="white" stroke="#42A5F5" stroke-width="1" rx="4"/>
  <text x="150" y="106" text-anchor="middle" font-family="Consolas, sans-serif" font-size="11" font-weight="bold" fill="#333">Player.cs</text>
  <text x="150" y="121" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#666">游戏主控 · 输入处理 · 状态管理</text>

  <rect x="260" y="88" width="180" height="42" fill="white" stroke="#42A5F5" stroke-width="1" rx="4"/>
  <text x="350" y="106" text-anchor="middle" font-family="Consolas, sans-serif" font-size="11" font-weight="bold" fill="#333">ChessAI.cs</text>
  <text x="350" y="121" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#666">MinMax搜索 · 评估表 · 难度分级</text>

  <rect x="460" y="88" width="180" height="42" fill="white" stroke="#42A5F5" stroke-width="1" rx="4"/>
  <text x="550" y="106" text-anchor="middle" font-family="Consolas, sans-serif" font-size="11" font-weight="bold" fill="#333">ClaudeApiProvider.cs</text>
  <text x="550" y="121" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#666">Volces Ark 网关 · 云端AI推理</text>

  <rect x="660" y="88" width="180" height="42" fill="white" stroke="#42A5F5" stroke-width="1" rx="4"/>
  <text x="750" y="106" text-anchor="middle" font-family="Consolas, sans-serif" font-size="11" font-weight="bold" fill="#333">LeaderboardAPI.cs</text>
  <text x="750" y="121" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#666">排行榜客户端 · HTTP请求封装</text>

  <!-- UI modules row 2 -->
  <rect x="60" y="145" width="130" height="32" fill="#f0f8ff" stroke="#90CAF9" stroke-width="1" rx="3"/>
  <text x="125" y="164" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#555">MainMenuUI</text>

  <rect x="205" y="145" width="130" height="32" fill="#f0f8ff" stroke="#90CAF9" stroke-width="1" rx="3"/>
  <text x="270" y="164" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#555">MoveHistoryUI</text>

  <rect x="350" y="145" width="130" height="32" fill="#f0f8ff" stroke="#90CAF9" stroke-width="1" rx="3"/>
  <text x="415" y="164" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#555">EvaluationBar</text>

  <rect x="495" y="145" width="130" height="32" fill="#f0f8ff" stroke="#90CAF9" stroke-width="1" rx="3"/>
  <text x="560" y="164" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#555">HintSystem</text>

  <rect x="640" y="145" width="130" height="32" fill="#f0f8ff" stroke="#90CAF9" stroke-width="1" rx="3"/>
  <text x="705" y="164" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#555">ChatUI + ChatWS</text>

  <!-- Animation & Audio row 3 -->
  <rect x="60" y="190" width="180" height="28" fill="#f0f8ff" stroke="#90CAF9" stroke-width="1" rx="3"/>
  <text x="150" y="208" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#555">MoveAnimator · GameEndAnimator</text>

  <rect x="260" y="190" width="180" height="28" fill="#f0f8ff" stroke="#90CAF9" stroke-width="1" rx="3"/>
  <text x="350" y="208" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#555">AudioManager · BoardCoordinateLabels</text>

  <rect x="460" y="190" width="380" height="28" fill="#f0f8ff" stroke="#90CAF9" stroke-width="1" rx="3"/>
  <text x="650" y="208" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#555">CommandInputUI · DifficultySelector · LeaderboardUI</text>

  <!-- Chess Engine inset -->
  <rect x="90" y="228" width="740" height="20" fill="#FFE0B2" stroke="#FF9800" stroke-width="0.8" rx="3"/>
  <text x="460" y="242" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#E65100">Gera Chess Library（内嵌）: ChessBoard.cs · ChessGenerations · ChessValidations · FEN/PGN/SAN转换 · endgame检测</text>

  <!-- Cloud Code Layer -->
  <rect x="40" y="270" width="820" height="120" fill="#fff3e0" stroke="#FF9800" stroke-width="1.5" rx="6"/>
  <text x="55" y="290" font-family="Microsoft YaHei, sans-serif" font-size="13" font-weight="bold" fill="#E65100">Unity Gaming Services — Cloud Code (C#)</text>

  <rect x="60" y="305" width="185" height="36" fill="white" stroke="#FFB74D" stroke-width="1" rx="4"/>
  <text x="152" y="319" text-anchor="middle" font-family="Consolas, sans-serif" font-size="10" font-weight="bold" fill="#333">HostGame</text>
  <text x="152" y="333" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="8" fill="#888">CreateLobby · 初始化 Cloud Save</text>

  <rect x="260" y="305" width="185" height="36" fill="white" stroke="#FFB74D" stroke-width="1" rx="4"/>
  <text x="352" y="319" text-anchor="middle" font-family="Consolas, sans-serif" font-size="10" font-weight="bold" fill="#333">JoinGame</text>
  <text x="352" y="333" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="8" fill="#888">JoinLobby · 随机分配黑白 · Rejoin</text>

  <rect x="460" y="305" width="185" height="36" fill="white" stroke="#FFB74D" stroke-width="1" rx="4"/>
  <text x="552" y="319" text-anchor="middle" font-family="Consolas, sans-serif" font-size="10" font-weight="bold" fill="#333">MakeMove</text>
  <text x="552" y="333" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="8" fill="#888">走法校验 · 更新FEN · Push推送</text>

  <rect x="660" y="305" width="185" height="36" fill="white" stroke="#FFB74D" stroke-width="1" rx="4"/>
  <text x="752" y="319" text-anchor="middle" font-family="Consolas, sans-serif" font-size="10" font-weight="bold" fill="#333">Resign</text>
  <text x="752" y="333" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="8" fill="#888">Resign() · 保存FEN · Push推送</text>

  <!-- UGS services row -->
  <text x="460" y="380" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="10" fill="#E65100">↓ 依赖 ↓</text>

  <!-- UGS Services -->
  <rect x="40" y="395" width="820" height="55" fill="#fce4ec" stroke="#E91E63" stroke-width="1.5" rx="6"/>
  <text x="55" y="415" font-family="Microsoft YaHei, sans-serif" font-size="13" font-weight="bold" fill="#C62828">UGS 云服务</text>

  <rect x="60" y="425" width="185" height="20" fill="white" stroke="#F48FB1" stroke-width="0.8" rx="3"/>
  <text x="152" y="439" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#555">Lobby Service（房间管理）</text>

  <rect x="260" y="425" width="185" height="20" fill="white" stroke="#F48FB1" stroke-width="0.8" rx="3"/>
  <text x="352" y="439" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#555">Cloud Save（FEN持久化）</text>

  <rect x="460" y="425" width="185" height="20" fill="white" stroke="#F48FB1" stroke-width="0.8" rx="3"/>
  <text x="552" y="439" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#555">Player Messages（Push推送）</text>

  <rect x="660" y="425" width="185" height="20" fill="white" stroke="#F48FB1" stroke-width="0.8" rx="3"/>
  <text x="752" y="439" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#555">Authentication（匿名登录）</text>

  <!-- External Services Layer -->
  <rect x="40" y="465" width="820" height="175" fill="#e8f5e9" stroke="#4CAF50" stroke-width="1.5" rx="6"/>
  <text x="55" y="485" font-family="Microsoft YaHei, sans-serif" font-size="13" font-weight="bold" fill="#2E7D32">华为云 ECS (121.36.101.82 · Ubuntu 24.04 · t6.medium.2)</text>

  <rect x="60" y="500" width="370" height="60" fill="white" stroke="#81C784" stroke-width="1" rx="4"/>
  <text x="245" y="518" text-anchor="middle" font-family="Consolas, sans-serif" font-size="11" font-weight="bold" fill="#333">Node.js + Express + SQLite (Port 3000)</text>
  <text x="245" y="535" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#666">POST /score · GET /leaderboard · GET /rank/:name</text>
  <text x="245" y="550" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#666">PUT /player/:old · DELETE /score/:name（管理员）</text>

  <rect x="470" y="500" width="370" height="60" fill="white" stroke="#81C784" stroke-width="1" rx="4"/>
  <text x="655" y="518" text-anchor="middle" font-family="Consolas, sans-serif" font-size="11" font-weight="bold" fill="#333">WebSocket Chat Server (Port 3001)</text>
  <text x="655" y="535" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#666">ws://121.36.101.82:3001 · 房间隔离 · 系统广播</text>
  <text x="655" y="550" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#666">PM2 进程守护（ecosystem.config.js）</text>

  <!-- External AI -->
  <rect x="60" y="575" width="780" height="28" fill="#E8EAF6" stroke="#3F51B5" stroke-width="1" rx="4"/>
  <text x="75" y="594" font-family="Microsoft YaHei, sans-serif" font-size="10" fill="#283593">Volces Ark API Gateway: https://ark.cn-beijing.volces.com/api/coding/v1/messages · model=ark-code-latest → MiniMax-M3</text>

  <!-- Arrows between layers -->
  <line x1="450" y1="255" x2="450" y2="270" stroke="#1565C0" stroke-width="2" marker-end="url(#arrow)"/>
  <line x1="450" y1="390" x2="450" y2="395" stroke="#E65100" stroke-width="2" marker-end="url(#arrow)"/>
  <line x1="450" y1="450" x2="450" y2="465" stroke="#C62828" stroke-width="2" marker-end="url(#arrow)"/>

  <!-- Client-to-Cloud arrows -->
  <line x1="170" y1="230" x2="170" y2="305" stroke="#42A5F5" stroke-width="1.5" marker-end="url(#arrow)" stroke-dasharray="4,3"/>
  <line x1="730" y1="130" x2="730" y2="305" stroke="#42A5F5" stroke-width="1.5" marker-end="url(#arrow)" stroke-dasharray="4,3"/>

  <!-- Port labels -->
  <text x="245" y="565" text-anchor="middle" font-family="Consolas, sans-serif" font-size="8" fill="#999">HTTP</text>
  <text x="655" y="565" text-anchor="middle" font-family="Consolas, sans-serif" font-size="8" fill="#999">WebSocket</text>
</svg>'''
    write_svg('1_system_architecture.svg', svg)


def ai_routing_diagram():
    """AI routing flow diagram"""
    svg = '''<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 850 500" width="850" height="500">
  <defs>
    <marker id="arrow" markerWidth="8" markerHeight="6" refX="8" refY="3" orient="auto">
      <path d="M0,0 L8,3 L0,6 Z" fill="#333"/>
    </marker>
    <marker id="arrowRed" markerWidth="8" markerHeight="6" refX="8" refY="3" orient="auto">
      <path d="M0,0 L8,3 L0,6 Z" fill="#C62828"/>
    </marker>
    <linearGradient id="headerGrad" x1="0" y1="0" x2="1" y2="0">
      <stop offset="0%" stop-color="#2c3e50"/>
      <stop offset="100%" stop-color="#34495e"/>
    </linearGradient>
  </defs>

  <rect x="0" y="0" width="850" height="38" fill="url(#headerGrad)" rx="3"/>
  <text x="425" y="25" text-anchor="middle" fill="white" font-family="Microsoft YaHei, sans-serif" font-size="16" font-weight="bold">图3-2  AI 混合路由决策流程</text>

  <!-- Start -->
  <rect x="350" y="55" width="150" height="36" fill="#2c3e50" rx="18"/>
  <text x="425" y="78" text-anchor="middle" fill="white" font-family="Microsoft YaHei, sans-serif" font-size="13" font-weight="bold">AI 走棋请求</text>
  <line x1="425" y1="91" x2="425" y2="110" stroke="#333" stroke-width="2" marker-end="url(#arrow)"/>

  <!-- Decision diamond: depth check -->
  <polygon points="425,115 290,155 425,195 560,155" fill="#FFF9C4" stroke="#F9A825" stroke-width="1.5"/>
  <text x="425" y="152" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="12" font-weight="bold" fill="#333">AI 难度?</text>
  <text x="425" y="168" text-anchor="middle" font-family="Consolas, sans-serif" font-size="10" fill="#666">depth ≤ 3 or depth ≥ 4?</text>

  <!-- Left branch: Local -->
  <text x="310" y="140" font-family="Microsoft YaHei, sans-serif" font-size="11" font-weight="bold" fill="#2E7D32">depth ≤ 3</text>
  <line x1="290" y1="155" x2="160" y2="155" stroke="#2E7D32" stroke-width="2" marker-end="url(#arrow)"/>

  <rect x="50" y="135" width="200" height="40" fill="#C8E6C9" stroke="#43A047" stroke-width="1.5" rx="5"/>
  <text x="150" y="153" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="11" font-weight="bold" fill="#2E7D32">本地 MinMax 引擎</text>
  <text x="150" y="168" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#555">ChessAI.cs · Piece-Square Table</text>

  <line x1="150" y1="175" x2="150" y2="260" stroke="#2E7D32" stroke-width="1.5" marker-end="url(#arrow)"/>

  <!-- Left detail box -->
  <rect x="30" y="265" width="240" height="100" fill="#f1f8e9" stroke="#81C784" stroke-width="1" rx="5"/>
  <text x="150" y="285" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="11" font-weight="bold" fill="#2E7D32">MinMax + α-β 剪枝</text>
  <text x="45" y="304" font-family="Consolas, sans-serif" font-size="9" fill="#555">depth=1 (Easy):</text>
  <text x="160" y="304" font-family="Consolas, sans-serif" font-size="9" fill="#666">≈ 30 节点, &lt;1s</text>
  <text x="45" y="320" font-family="Consolas, sans-serif" font-size="9" fill="#555">depth=3 (Medium):</text>
  <text x="160" y="320" font-family="Consolas, sans-serif" font-size="9" fill="#666">≈ 8000 节点, 3~5s</text>
  <text x="45" y="336" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#555">评估函数: Piece-Square Table</text>
  <text x="45" y="352" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#555">优势: 完全离线 · 结果确定 · 速度快</text>

  <!-- Right branch: Cloud -->
  <text x="540" y="140" font-family="Microsoft YaHei, sans-serif" font-size="11" font-weight="bold" fill="#1565C0">depth ≥ 4</text>
  <line x1="560" y1="155" x2="690" y2="155" stroke="#1565C0" stroke-width="2" marker-end="url(#arrow)"/>

  <rect x="600" y="135" width="220" height="40" fill="#BBDEFB" stroke="#1976D2" stroke-width="1.5" rx="5"/>
  <text x="710" y="153" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="11" font-weight="bold" fill="#1565C0">云端 Claude API</text>
  <text x="710" y="168" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#555">ClaudeApiProvider.cs · Volces Ark</text>

  <line x1="710" y1="175" x2="710" y2="210" stroke="#1565C0" stroke-width="1.5" marker-end="url(#arrow)"/>

  <!-- Right decision: thinking? -->
  <polygon points="710,215 620,245 710,275 800,245" fill="#E3F2FD" stroke="#1976D2" stroke-width="1.2"/>
  <text x="710" y="242" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="11" font-weight="bold" fill="#333">thinking?</text>
  <text x="710" y="258" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#666">depth=4 vs depth=5</text>

  <text x="630" y="230" font-family="Microsoft YaHei, sans-serif" font-size="10" fill="#1565C0">depth=4</text>
  <text x="780" y="230" font-family="Microsoft YaHei, sans-serif" font-size="10" fill="#0D47A1">depth=5</text>

  <line x1="620" y1="245" x2="580" y2="245" stroke="#1565C0" stroke-width="1.5"/>
  <line x1="580" y1="245" x2="580" y2="310" stroke="#1565C0" stroke-width="1.5" marker-end="url(#arrow)"/>

  <line x1="800" y1="245" x2="830" y2="245" stroke="#0D47A1" stroke-width="1.5"/>
  <line x1="830" y1="245" x2="830" y2="310" stroke="#0D47A1" stroke-width="1.5" marker-end="url(#arrow)"/>

  <rect x="490" y="314" width="180" height="50" fill="#E3F2FD" stroke="#1976D2" stroke-width="1" rx="5"/>
  <text x="580" y="334" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="11" font-weight="bold" fill="#1565C0">无思考模式</text>
  <text x="580" y="350" text-anchor="middle" font-family="Consolas, sans-serif" font-size="9" fill="#555">thinking: disabled · max_tokens: 64</text>

  <rect x="720" y="314" width="110" height="50" fill="#E8EAF6" stroke="#3F51B5" stroke-width="1" rx="5"/>
  <text x="775" y="334" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="11" font-weight="bold" fill="#283593">思考模式</text>
  <text x="775" y="350" text-anchor="middle" font-family="Consolas, sans-serif" font-size="9" fill="#555">budget: 6K tokens</text>

  <!-- API call details -->
  <rect x="490" y="380" width="340" height="55" fill="#F5F5F5" stroke="#9E9E9E" stroke-width="1" rx="5"/>
  <text x="660" y="398" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="10" font-weight="bold" fill="#333">Volces Ark Anthropic API</text>
  <text x="660" y="415" text-anchor="middle" font-family="Consolas, sans-serif" font-size="8" fill="#666">POST /api/coding/v1/messages · model=ark-code-latest</text>
  <text x="660" y="430" text-anchor="middle" font-family="Consolas, sans-serif" font-size="8" fill="#666">UCI格式提取 → SAN回退 → 合法性校验 → Random Fallback</text>

  <!-- Result merge -->
  <line x1="150" y1="365" x2="150" y2="455" stroke="#2E7D32" stroke-width="1.5"/>
  <line x1="150" y1="455" x2="425" y2="455" stroke="#333" stroke-width="1.5"/>
  <line x1="660" y1="435" x2="660" y2="455" stroke="#333" stroke-width="1.5"/>
  <line x1="660" y1="455" x2="425" y2="455" stroke="#333" stroke-width="1.5"/>
  <line x1="425" y1="455" x2="425" y2="475" stroke="#333" stroke-width="2" marker-end="url(#arrow)"/>

  <!-- Return result -->
  <rect x="350" y="478" width="150" height="20" fill="#2c3e50" rx="10"/>
  <text x="425" y="492" text-anchor="middle" fill="white" font-family="Microsoft YaHei, sans-serif" font-size="11">返回最佳走法 (UCI)</text>
</svg>'''
    write_svg('2_ai_routing.svg', svg)


def state_transition_diagram():
    """Game state transition diagram"""
    svg = '''<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 850 480" width="850" height="480">
  <defs>
    <marker id="arrow" markerWidth="8" markerHeight="6" refX="8" refY="3" orient="auto">
      <path d="M0,0 L8,3 L0,6 Z" fill="#333"/>
    </marker>
    <marker id="arrowBlue" markerWidth="8" markerHeight="6" refX="8" refY="3" orient="auto">
      <path d="M0,0 L8,3 L0,6 Z" fill="#1976D2"/>
    </marker>
  </defs>

  <rect x="0" y="0" width="850" height="38" fill="#2c3e50" rx="3"/>
  <text x="425" y="25" text-anchor="middle" fill="white" font-family="Microsoft YaHei, sans-serif" font-size="16" font-weight="bold">图3-3  游戏状态转换图</text>

  <!-- States as rounded rectangles -->
  <!-- Init -->
  <rect x="350" y="55" width="150" height="40" fill="#2c3e50" rx="8"/>
  <text x="425" y="80" text-anchor="middle" fill="white" font-family="Microsoft YaHei, sans-serif" font-size="13" font-weight="bold">程序启动</text>

  <line x1="425" y1="95" x2="425" y2="115" stroke="#333" stroke-width="2" marker-end="url(#arrow)"/>

  <!-- Main Menu -->
  <rect x="350" y="120" width="150" height="45" fill="#E3F2FD" stroke="#1976D2" stroke-width="2" rx="8"/>
  <text x="425" y="140" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="13" font-weight="bold" fill="#0D47A1">主菜单</text>
  <text x="425" y="157" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="8" fill="#555">选择游戏模式</text>

  <!-- Branching from Main Menu -->
  <!-- Local Game branch -->
  <line x1="350" y1="142" x2="230" y2="225" stroke="#1976D2" stroke-width="1.5" marker-end="url(#arrowBlue)"/>
  <text x="280" y="175" font-family="Microsoft YaHei, sans-serif" font-size="10" fill="#1565C0">Local Game</text>

  <rect x="140" y="230" width="180" height="45" fill="#C8E6C9" stroke="#43A047" stroke-width="1.5" rx="8"/>
  <text x="230" y="250" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="12" font-weight="bold" fill="#2E7D32">本地双人对战</text>
  <text x="230" y="267" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="8" fill="#555">每步切换视角</text>

  <!-- AI branch -->
  <line x1="385" y1="165" x2="385" y2="230" stroke="#1976D2" stroke-width="1.5" marker-end="url(#arrowBlue)"/>
  <text x="395" y="200" font-family="Microsoft YaHei, sans-serif" font-size="10" fill="#1565C0">vs AI</text>

  <rect x="315" y="235" width="140" height="45" fill="#C8E6C9" stroke="#43A047" stroke-width="1.5" rx="8"/>
  <text x="385" y="255" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="12" font-weight="bold" fill="#2E7D32">人机对战</text>
  <text x="385" y="272" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="8" fill="#555">玩家白方 · AI黑方</text>

  <!-- Online branch -->
  <line x1="500" y1="142" x2="620" y2="225" stroke="#1976D2" stroke-width="1.5" marker-end="url(#arrowBlue)"/>
  <text x="570" y="175" font-family="Microsoft YaHei, sans-serif" font-size="10" fill="#1565C0">Online Game</text>

  <rect x="525" y="230" width="200" height="45" fill="#C8E6C9" stroke="#43A047" stroke-width="1.5" rx="8"/>
  <text x="625" y="250" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="12" font-weight="bold" fill="#2E7D32">在线对战</text>
  <text x="625" y="267" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="8" fill="#555">创房 · 加入 · Push同步</text>

  <!-- Leaderboard -->
  <line x1="500" y1="155" x2="750" y2="85" stroke="#1976D2" stroke-width="1.5"/>
  <rect x="700" y="60" width="130" height="40" fill="#FFECB3" stroke="#FFA000" stroke-width="1.5" rx="6"/>
  <text x="765" y="84" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="11" font-weight="bold" fill="#E65100">排行榜</text>

  <!-- Game in progress states -->
  <rect x="220" y="295" width="200" height="42" fill="#FFF3E0" stroke="#FF9800" stroke-width="1.5" rx="8"/>
  <text x="320" y="313" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="12" font-weight="bold" fill="#E65100">游戏进行中</text>
  <text x="320" y="330" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="8" fill="#555">走棋 → Push → 对手响应 → 循环</text>

  <line x1="230" y1="275" x2="320" y2="293" stroke="#43A047" stroke-width="1.5" marker-end="url(#arrow)"/>
  <line x1="385" y1="280" x2="370" y2="295" stroke="#43A047" stroke-width="1.5" marker-end="url(#arrow)"/>
  <line x1="625" y1="275" x2="450" y2="293" stroke="#43A047" stroke-width="1.5" marker-end="url(#arrow)"/>

  <!-- Game over -->
  <line x1="320" y1="337" x2="320" y2="370" stroke="#333" stroke-width="2" marker-end="url(#arrow)"/>

  <rect x="220" y="375" width="200" height="42" fill="#FFCDD2" stroke="#F44336" stroke-width="1.5" rx="8"/>
  <text x="320" y="395" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="12" font-weight="bold" fill="#C62828">游戏结束</text>
  <text x="320" y="412" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="8" fill="#555">将杀 · 认输 · 和棋 · 超时</text>

  <!-- Score submit -->
  <line x1="320" y1="417" x2="320" y2="445" stroke="#333" stroke-width="1.5"/>
  <rect x="270" y="448" width="100" height="20" fill="#E8EAF6" stroke="#3F51B5" stroke-width="0.8" rx="3"/>
  <text x="320" y="462" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#283593">提交分数 → 排行榜</text>

  <!-- Return to menu -->
  <line x1="420" y1="395" x2="470" y2="300" stroke="#999" stroke-width="1.5" stroke-dasharray="5,3"/>
  <rect x="475" y="320" width="150" height="20" fill="#F5F5F5" stroke="#9E9E9E" stroke-width="0.8" rx="3"/>
  <text x="550" y="334" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#999">返回主菜单</text>

  <!-- Legend -->
  <rect x="40" y="230" width="100" height="120" fill="#FAFAFA" stroke="#E0E0E0" stroke-width="1" rx="4"/>
  <text x="90" y="250" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="10" font-weight="bold" fill="#333">图例</text>
  <line x1="55" y1="275" x2="85" y2="275" stroke="#1976D2" stroke-width="2"/>
  <text x="90" y="280" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#555">选择分支</text>
  <line x1="55" y1="300" x2="85" y2="300" stroke="#333" stroke-width="2"/>
  <text x="90" y="305" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#555">状态转换</text>
  <line x1="55" y1="325" x2="85" y2="325" stroke="#999" stroke-width="1.5" stroke-dasharray="5,3"/>
  <text x="90" y="330" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#555">返回路径</text>
</svg>'''
    write_svg('3_state_transition.svg', svg)


def online_dataflow_diagram():
    """Online game data flow sequence"""
    svg = '''<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 900 750" width="900" height="750">
  <defs>
    <marker id="arrowF" markerWidth="8" markerHeight="6" refX="8" refY="3" orient="auto">
      <path d="M0,0 L8,3 L0,6 Z" fill="#1976D2"/>
    </marker>
    <marker id="arrowG" markerWidth="8" markerHeight="6" refX="8" refY="3" orient="auto">
      <path d="M0,0 L8,3 L0,6 Z" fill="#2E7D32"/>
    </marker>
    <marker id="arrowR" markerWidth="8" markerHeight="6" refX="8" refY="3" orient="auto">
      <path d="M0,0 L8,3 L0,6 Z" fill="#C62828"/>
    </marker>
  </defs>

  <rect x="0" y="0" width="900" height="38" fill="#2c3e50" rx="3"/>
  <text x="450" y="25" text-anchor="middle" fill="white" font-family="Microsoft YaHei, sans-serif" font-size="16" font-weight="bold">图3-4  在线对战数据流时序图</text>

  <!-- Column headers -->
  <rect x="20" y="52" width="140" height="28" fill="#1976D2" rx="4"/>
  <text x="90" y="71" text-anchor="middle" fill="white" font-family="Microsoft YaHei, sans-serif" font-size="10" font-weight="bold">客户端A（房主）</text>

  <rect x="230" y="52" width="120" height="28" fill="#1976D2" rx="4"/>
  <text x="290" y="71" text-anchor="middle" fill="white" font-family="Microsoft YaHei, sans-serif" font-size="10" font-weight="bold">客户端B（加入者）</text>

  <rect x="420" y="52" width="130" height="28" fill="#FF9800" rx="4"/>
  <text x="485" y="71" text-anchor="middle" fill="white" font-family="Microsoft YaHei, sans-serif" font-size="10" font-weight="bold">Cloud Code (C#)</text>

  <rect x="610" y="52" width="130" height="28" fill="#4CAF50" rx="4"/>
  <text x="675" y="71" text-anchor="middle" fill="white" font-family="Microsoft YaHei, sans-serif" font-size="10" font-weight="bold">UGS Cloud Save</text>

  <rect x="790" y="52" width="90" height="28" fill="#E91E63" rx="4"/>
  <text x="835" y="71" text-anchor="middle" fill="white" font-family="Microsoft YaHei, sans-serif" font-size="10" font-weight="bold">UGS Push</text>

  <!-- Lifelines -->
  <line x1="90" y1="80" x2="90" y2="730" stroke="#BBDEFB" stroke-width="1.5" stroke-dasharray="4,3"/>
  <line x1="290" y1="80" x2="290" y2="730" stroke="#BBDEFB" stroke-width="1.5" stroke-dasharray="4,3"/>
  <line x1="485" y1="80" x2="485" y2="730" stroke="#FFE0B2" stroke-width="1.5" stroke-dasharray="4,3"/>
  <line x1="675" y1="80" x2="675" y2="730" stroke="#C8E6C9" stroke-width="1.5" stroke-dasharray="4,3"/>
  <line x1="835" y1="80" x2="835" y2="730" stroke="#F8BBD0" stroke-width="1.5" stroke-dasharray="4,3"/>

  <!-- Phase 1: HostGame -->
  <text x="10" y="105" font-family="Microsoft YaHei, sans-serif" font-size="10" font-weight="bold" fill="#1976D2">阶段1</text>
  <line x1="90" y1="105" x2="90" y2="115" stroke="#1976D2" stroke-width="1.5"/>
  <text x="95" y="113" font-family="Microsoft YaHei, sans-serif" font-size="10" fill="#333">Click "Create Room"</text>

  <line x1="90" y1="120" x2="480" y2="140" stroke="#1976D2" stroke-width="1.5" marker-end="url(#arrowF)"/>
  <text x="200" y="135" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#1565C0">CloudCode.HostGame()</text>

  <line x1="485" y1="145" x2="670" y2="165" stroke="#FF9800" stroke-width="1.5" marker-end="url(#arrowF)"/>
  <text x="560" y="160" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#E65100">CreateLobby(2 players)</text>

  <line x1="485" y1="172" x2="670" y2="192" stroke="#FF9800" stroke-width="1.5" marker-end="url(#arrowF)"/>
  <text x="560" y="187" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#E65100">SetCustomItem(board=FEN, whitePlayerId)</text>

  <line x1="485" y1="198" x2="100" y2="220" stroke="#FF9800" stroke-width="1.5" stroke-dasharray="6,3" marker-end="url(#arrowF)"/>
  <text x="200" y="215" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#E65100">return LobbyCode (6 digits)</text>

  <text x="95" y="235" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#333">显示房间码, 等待对手...</text>

  <!-- Phase 2: JoinGame -->
  <text x="10" y="260" font-family="Microsoft YaHei, sans-serif" font-size="10" font-weight="bold" fill="#2E7D32">阶段2</text>
  <line x1="290" y1="260" x2="290" y2="270" stroke="#2E7D32" stroke-width="1.5"/>
  <text x="295" y="270" font-family="Microsoft YaHei, sans-serif" font-size="10" fill="#333">输入房间码, Click "Join"</text>

  <line x1="290" y1="275" x2="480" y2="295" stroke="#2E7D32" stroke-width="1.5" marker-end="url(#arrowG)"/>
  <text x="350" y="292" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#2E7D32">CloudCode.JoinGame(lobbyCode)</text>

  <line x1="485" y1="300" x2="670" y2="320" stroke="#FF9800" stroke-width="1.5" marker-end="url(#arrowF)"/>
  <text x="560" y="315" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#E65100">JoinLobbyByCode() → GetCustomItems()</text>

  <text x="485" y="335" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#333">rng ≥ 0.5? → 分配黑白方</text>

  <line x1="485" y1="342" x2="670" y2="362" stroke="#FF9800" stroke-width="1.5" marker-end="url(#arrowF)"/>
  <text x="560" y="357" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#E65100">SetCustomItem(blackPlayerId + FEN)</text>

  <!-- Push to A -->
  <line x1="485" y1="368" x2="830" y2="388" stroke="#FF9800" stroke-width="1.5" marker-end="url(#arrowF)"/>
  <text x="650" y="383" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#C62828">SendPlayerMessage("opponentJoined", B_info) → A</text>

  <line x1="835" y1="393" x2="100" y2="415" stroke="#C62828" stroke-width="1.5" stroke-dasharray="6,3" marker-end="url(#arrowR)"/>
  <text x="400" y="410" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#C62828">Push: "opponentJoined" → A.OnGameStart()</text>

  <text x="95" y="430" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#333">初始化UI, 进游戏</text>
  <text x="295" y="430" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#333">初始化UI, 进游戏</text>

  <!-- Phase 3: MakeMove -->
  <text x="10" y="455" font-family="Microsoft YaHei, sans-serif" font-size="10" font-weight="bold" fill="#C62828">阶段3</text>
  <text x="95" y="455" font-family="Microsoft YaHei, sans-serif" font-size="10" font-weight="bold" fill="#333">A 走棋 e2→e4</text>

  <line x1="90" y1="462" x2="480" y2="482" stroke="#C62828" stroke-width="1.5" marker-end="url(#arrowR)"/>
  <text x="200" y="478" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#C62828">CloudCode.MakeMove(session="lobby_id", "e2", "e4")</text>

  <text x="485" y="497" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#333">1. GetCustomItems(board, whiteId, blackId)</text>
  <text x="485" y="512" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#333">2. 校验 turn == white (A 是白方)</text>
  <text x="485" y="527" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#333">3. chessBoard.IsValidMove("e2", "e4") → true</text>
  <text x="485" y="542" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#333">4. chessBoard.Move(new Move("e2", "e4"))</text>

  <line x1="485" y1="548" x2="670" y2="568" stroke="#FF9800" stroke-width="1.5" marker-end="url(#arrowF)"/>
  <text x="560" y="563" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#E65100">SetCustomItem(board=new_FEN)</text>

  <line x1="485" y1="574" x2="830" y2="594" stroke="#FF9800" stroke-width="1.5" marker-end="url(#arrowF)"/>
  <text x="650" y="590" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#C62828">SendPlayerMessage("boardUpdated", new_FEN) → B</text>

  <line x1="835" y1="599" x2="300" y2="620" stroke="#C62828" stroke-width="1.5" stroke-dasharray="6,3" marker-end="url(#arrowR)"/>
  <text x="550" y="615" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#C62828">Push: "boardUpdated" (FEN + GameOver flag)</text>

  <text x="295" y="635" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#333">OnBoardUpdate() → SyncBoard(FEN)</text>
  <text x="295" y="650" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#333">FindSanBetween() → 显示走棋历史</text>
  <text x="295" y="665" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#333">切换到B的回合 → "Your Turn"</text>

  <!-- Phase 4: Resign -->
  <text x="10" y="690" font-family="Microsoft YaHei, sans-serif" font-size="10" font-weight="bold" fill="#999">阶段4·可选</text>
  <text x="295" y="690" font-family="Microsoft YaHei, sans-serif" font-size="10" font-weight="bold" fill="#333">B 认输</text>

  <line x1="290" y1="697" x2="480" y2="717" stroke="#999" stroke-width="1.2" marker-end="url(#arrowF)"/>
  <text x="370" y="713" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#999">CloudCode.Resign()</text>

  <text x="485" y="730" text-anchor="middle" font-family="Microsoft YaHei, sans-serif" font-size="9" fill="#999">chessBoard.Resign(Black) → Save + Push</text>
</svg>'''
    write_svg('4_online_dataflow.svg', svg)


if __name__ == '__main__':
    system_architecture()
    ai_routing_diagram()
    state_transition_diagram()
    online_dataflow_diagram()
    print("All 4 diagrams generated.")
