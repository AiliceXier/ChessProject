"""Generate architecture diagrams as PNG using PIL."""
from PIL import Image, ImageDraw, ImageFont
import os

os.makedirs(r'D:\unity\my_chess\diagrams\png', exist_ok=True)

WHITE = 'white'
DARK = (44, 62, 80)
BLUE = (25, 118, 210)
GREEN = (67, 160, 71)
RED = (198, 40, 40)
GRAY = (85, 85, 85)
LIGHT_GRAY = (245, 245, 245)
TITLE_FILL = (44, 62, 80)

def title_bar(draw, w, text):
    draw.rectangle([0, 0, w, 36], fill=TITLE_FILL)
    draw.text((w//2-60, 8), text, fill='white')

def box(draw, x, y, w, h, fill, outline, text, tc=(51,51,51)):
    draw.rectangle([x, y, x+w, y+h], fill=fill, outline=outline, width=1)
    ypos = y + 4
    for line in text.split('\n'):
        draw.text((x+8, ypos), line, fill=tc)
        ypos += 16

# === Diagram 1: System Architecture ===
W, H = 1600, 1050
img = Image.new('RGB', (W, H), 'white')
d = ImageDraw.Draw(img)
title_bar(d, W, '图3-1  系统总体架构图')

# Client
d.rectangle([30, 52, 1570, 260], fill=(232,244,253), outline=(33,150,243), width=2)
box(d, 35, 54, 1500, 14, (232,244,253), None, 'Unity 客户端 (C#)', (21,101,192))

boxes = [
    (50,76,270,48, WHITE, BLUE, 'Player.cs\n游戏主控 · 输入处理 · 状态管理', GRAY),
    (335,76,270,48, WHITE, BLUE, 'ChessAI.cs\nMinMax搜索 · 评估表 · 难度分级', GRAY),
    (620,76,270,48, WHITE, BLUE, 'ClaudeApiProvider.cs\nVolces Ark API · 云端推理', GRAY),
    (905,76,270,48, WHITE, BLUE, 'LeaderboardAPI.cs\nHTTP Client · 排行榜', GRAY),
    (50,135,200,30, (240,248,255), (144,202,249), 'MainMenuUI', GRAY),
    (265,135,200,30, (240,248,255), (144,202,249), 'MoveHistoryUI', GRAY),
    (480,135,200,30, (240,248,255), (144,202,249), 'EvaluationBar', GRAY),
    (695,135,200,30, (240,248,255), (144,202,249), 'HintSystem', GRAY),
    (910,135,200,30, (240,248,255), (144,202,249), 'ChatUI + ChatWS', GRAY),
    (50,178,280,30, (240,248,255), (144,202,249), 'MoveAnimator.cs · GameEndAnimator.cs', GRAY),
    (345,178,260,30, (240,248,255), (144,202,249), 'AudioManager.cs · BoardLabels', GRAY),
    (620,178,455,30, (240,248,255), (144,202,249), 'CommandInputUI · DifficultySelector · LeaderboardUI', GRAY),
    (80,220,1440,24, (255,224,178), (255,152,0), 'Gera Chess Library: ChessBoard.cs · ChessGenerations · ChessValidations · FEN/PGN · EndGame', (230,81,0)),
]
for args in boxes:
    box(d, *args)

# Cloud Code
d.rectangle([30, 275, 1570, 400], fill=(255,243,224), outline=(255,152,0), width=2)
box(d, 35, 277, 1500, 14, (255,243,224), None, 'Unity Gaming Services — Cloud Code (C#)', (230,81,0))

cc_box = [
    (50,300,355,40, WHITE, (255,183,77), 'HostGame\nCreateLobby · 初始化 Cloud Save', GRAY),
    (415,300,355,40, WHITE, (255,183,77), 'JoinGame\nJoinLobby · 随机黑白 · Rejoin', GRAY),
    (780,300,355,40, WHITE, (255,183,77), 'MakeMove\n走法校验 · 更新FEN · Push推送', GRAY),
    (1145,300,355,40, WHITE, (255,183,77), 'Resign\nResign() · 保存FEN · Push推送', GRAY),
]
for args in cc_box:
    box(d, *args)

d.text((750, 360), '▼ 依赖 ▼', fill=(230,81,0))

# UGS Services
d.rectangle([30, 373, 1570, 412], fill=(252,228,236), outline=(233,30,99), width=2)
box(d, 35, 375, 1500, 14, (252,228,236), None, 'UGS 云服务', (198,40,40))
ug_boxes = [
    (50,392,355,16, WHITE, (244,143,177), 'Lobby Service（房间管理）', GRAY),
    (415,392,355,16, WHITE, (244,143,177), 'Cloud Save（FEN 持久化存储）', GRAY),
    (780,392,355,16, WHITE, (244,143,177), 'Player Messages（Push 实时推送）', GRAY),
    (1145,392,355,16, WHITE, (244,143,177), 'Authentication（匿名登录）', GRAY),
]
for args in ug_boxes:
    box(d, *args)

# ECS
d.rectangle([30, 426, 1570, 590], fill=(232,245,233), outline=(76,175,80), width=2)
box(d, 35, 428, 1500, 14, (232,245,233), None, '华为云 ECS (121.36.101.82 · Ubuntu 24.04 · t6.medium.2 · 1vCPU/2GiB)', (46,125,50))

ecs_boxes = [
    (50,450,720,70, WHITE, (129,199,132),
     'Node.js + Express + better-sqlite3  ::  Port 3000\n'
     'POST /score  |  GET /leaderboard  |  GET /rank/:name\n'
     'PUT /player/:old_name  |  DELETE /score/:name', GRAY),
    (780,450,720,70, WHITE, (129,199,132),
     'WebSocket Chat Server  ::  Port 3001\n'
     'ws://121.36.101.82:3001  ·  房间隔离  ·  系统广播\n'
     'PM2 进程守护（ecosystem.config.js）', GRAY),
    (50,535,1440,45, (232,234,246), (63,81,181),
     'Volces Ark API: https://ark.cn-beijing.volces.com/api/coding/v1/messages\n'
     'model=ark-code-latest -> MiniMax-M3  |  depth=4:thinking disabled  |  depth=5:thinking enabled,budget=6K', (40,53,147)),
]
for args in ecs_boxes:
    box(d, *args)

d.text((760, 250), '▼ HTTP / Push', fill=BLUE)
d.text((760, 412), '▼ Cloud Save API', fill=RED)
d.text((760, 424), '▼ HTTP / WebSocket', fill=GREEN)

img.save(r'D:\unity\my_chess\diagrams\png\1_system_architecture.png')
print('Diagram 1 saved')

# === Diagram 2: AI Routing ===
W2, H2 = 850, 500
img2 = Image.new('RGB', (W2, H2), 'white')
d2 = ImageDraw.Draw(img2)
title_bar(d2, W2, '图3-2  AI 混合路由决策流程')

# Start
d2.rounded_rectangle([340, 55, 510, 85], radius=15, fill=DARK)
d2.text((370, 63), 'AI 走棋请求', fill='white')
d2.line([(425,85),(425,108)], fill=GRAY, width=2)
# Arrow head
d2.polygon([(425,108),(420,100),(430,100)], fill=GRAY)

# Decision diamond
pts = [(425,112),(295,148),(425,184),(555,148)]
d2.polygon(pts, fill=(255,249,196), outline=(249,168,37))
d2.text((395,137), 'AI 难度?', fill=(51,51,51))
d2.text((370,153), 'depth<=3 vs depth>=4?', fill=GRAY)

# Left: Local
d2.text((280,130), 'depth <= 3', fill=GREEN)
d2.line([(295,148),(155,148)], fill=GREEN, width=2)
d2.rounded_rectangle([50,130,260,172], radius=4, fill=(200,230,201), outline=GREEN)
d2.text((58,140), '本地 MinMax 引擎', fill=(46,125,50))
d2.text((58,156), 'ChessAI.cs · PST 评估', fill=GRAY)

d2.line([(155,172),(155,240)], fill=GREEN, width=1)
d2.rounded_rectangle([30,240,280,360], radius=4, fill=(241,248,233), outline=(129,199,132))
detail_lines = [
    'MinMax + Alpha-Beta 剪枝',
    'depth=1 (Easy):   ~30节点, <1s',
    'depth=3 (Medium): ~8000节点, 3-5s',
    '评估: Piece-Square Table',
    'P=100 N=320 B=330 R=500 Q=900 K=20000',
    '优点: 离线 · 确定 · 快速',
]
y_pos = 250
for line in detail_lines:
    d2.text((38, y_pos), line, fill=GRAY)
    y_pos += 18

# Right: Cloud
d2.text((565,130), 'depth >= 4', fill=BLUE)
d2.line([(555,148),(695,148)], fill=BLUE, width=2)
d2.rounded_rectangle([595,130,810,172], radius=4, fill=(187,222,251), outline=BLUE)
d2.text((603,140), '云端 Claude API', fill=BLUE)
d2.text((603,156), 'ClaudeApiProvider.cs', fill=GRAY)

d2.line([(702,172),(702,210)], fill=BLUE, width=1)
pts2 = [(702,214),(642,240),(702,266),(762,240)]
d2.polygon(pts2, fill=(227,242,253), outline=BLUE)
d2.text((680,233), 'thinking?', fill=GRAY)

d2.text((630,223), 'depth=4', fill=BLUE)
d2.text((770,223), 'depth=5', fill=(13,71,161))

d2.line([(642,240),(600,240)], fill=BLUE, width=1)
d2.line([(600,240),(600,290)], fill=BLUE, width=1)
d2.line([(762,240),(800,240)], fill=(13,71,161), width=1)
d2.line([(800,240),(800,290)], fill=(13,71,161), width=1)

d2.rounded_rectangle([550,290,650,340], radius=4, fill=(227,242,253), outline=BLUE)
d2.text((558,300), 'Hard (无思考)', fill=BLUE)
d2.text((558,316), 'thinking: disabled', fill=GRAY)
d2.text((558,328), 'max_tokens: 64', fill=GRAY)

d2.rounded_rectangle([750,290,830,340], radius=4, fill=(232,234,246), outline=(63,81,181))
d2.text((758,300), 'Master (思考)', fill=(40,53,147))
d2.text((758,316), 'thinking: enabled', fill=GRAY)
d2.text((758,328), 'budget: 6K tokens', fill=GRAY)

# API
d2.rounded_rectangle([550,355,830,425], radius=4, fill=LIGHT_GRAY, outline=(158,158,158))
d2.text((558,365), 'Volces Ark Anthropic API', fill=GRAY)
d2.text((558,383), 'POST /api/coding/v1/messages', fill=GRAY)
d2.text((558,399), 'UCI提取 -> SAN匹配 -> Fallback', fill=GRAY)

# Merge
d2.line([(155,360),(155,420)], fill=GREEN, width=1)
d2.line([(155,420),(690,420)], fill=GRAY, width=1)
d2.line([(690,425),(690,420)], fill=GRAY, width=1)
d2.line([(425,420),(425,445)], fill=GRAY, width=2)
d2.polygon([(425,445),(420,437),(430,437)], fill=GRAY)

d2.rounded_rectangle([340,448,510,478], radius=12, fill=DARK)
d2.text((365,458), '返回最佳走法 (UCI)', fill='white')

img2.save(r'D:\unity\my_chess\diagrams\png\2_ai_routing.png')
print('Diagram 2 saved')

# === Diagram 3: State Transition ===
W3, H3 = 850, 480
img3 = Image.new('RGB', (W3, H3), 'white')
d3 = ImageDraw.Draw(img3)
title_bar(d3, W3, '图3-3  游戏状态转换图')

# States
d3.rounded_rectangle([350,55,500,88], radius=8, fill=DARK)
d3.text((385,63), '程序启动', fill='white')

d3.rounded_rectangle([350,115,500,152], radius=8, fill=(227,242,253), outline=BLUE, width=2)
d3.text((365,123), '主菜单', fill=BLUE)
d3.text((365,140), '选择游戏模式', fill=GRAY)

# Branches
d3.line([(350,133),(230,215)], fill=BLUE, width=1)
d3.text((270,165), 'Local', fill=BLUE)
d3.line([(390,152),(390,232)], fill=BLUE, width=1)
d3.text((400,190), 'vs AI', fill=BLUE)
d3.line([(500,133),(620,215)], fill=BLUE, width=1)
d3.text((560,165), 'Online', fill=BLUE)
d3.line([(500,130),(750,80)], fill=BLUE, width=1)

d3.rounded_rectangle([140,220,320,262], radius=8, fill=(200,230,201), outline=GREEN, width=1)
d3.text((155,230), '本地双人对战', fill=(46,125,50))
d3.text((155,248), '每步切换视角', fill=GRAY)

d3.rounded_rectangle([325,237,465,265], radius=4, fill=(200,230,201), outline=GREEN, width=1)
d3.text((335,244), '人机对战', fill=(46,125,50))

d3.rounded_rectangle([530,220,730,262], radius=8, fill=(200,230,201), outline=GREEN, width=1)
d3.text((545,230), '在线对战', fill=(46,125,50))
d3.text((545,248), '创房 · 加入 · Push', fill=GRAY)

d3.rounded_rectangle([700,60,830,95], radius=5, fill=(255,236,179), outline=(255,160,0), width=1)
d3.text((715,70), '排行榜', fill=(230,81,0))

# Game in progress
d3.line([(230,262),(320,290)], fill=GREEN, width=1)
d3.line([(395,265),(395,290)], fill=GREEN, width=1)
d3.line([(630,262),(500,290)], fill=GREEN, width=1)

d3.rounded_rectangle([220,293,580,332], radius=8, fill=(255,243,224), outline=(255,152,0), width=1)
d3.text((240,302), '游戏进行中', fill=(230,81,0))
d3.text((240,318), '走棋 -> Push -> 对手响应 -> 循环', fill=GRAY)

d3.line([(400,332),(400,362)], fill=GRAY, width=2)
d3.polygon([(400,362),(396,354),(404,354)], fill=GRAY)

d3.rounded_rectangle([300,365,500,402], radius=8, fill=(255,205,210), outline=(244,67,54), width=1)
d3.text((315,374), '游戏结束', fill=RED)
d3.text((315,390), '将杀 · 认输 · 和棋', fill=GRAY)

d3.line([(400,402),(400,425)], fill=GRAY, width=1)
d3.rounded_rectangle([350,428,450,450], radius=4, fill=(232,234,246), outline=(63,81,181), width=1)
d3.text((360,436), '提交分数 -> 排行榜', fill=(40,53,147))

d3.line([(500,383),(530,310)], fill=(158,158,158), width=1)
d3.text((535,340), '返回', fill=(158,158,158))

# Legend
d3.rounded_rectangle([30,240,100,320], radius=4, fill=LIGHT_GRAY, outline=(224,224,224))
d3.text((60,245), '图例', fill=GRAY)
d3.line([(40,270),(80,270)], fill=BLUE, width=2)
d3.text((85,263), '分支', fill=GRAY)
d3.line([(40,290),(80,290)], fill=GRAY, width=2)
d3.text((85,283), '转换', fill=GRAY)
d3.line([(40,308),(80,308)], fill=(158,158,158), width=1)
d3.text((85,301), '返回', fill=GRAY)

img3.save(r'D:\unity\my_chess\diagrams\png\3_state_transition.png')
print('Diagram 3 saved')

# === Diagram 4: Online Dataflow ===
W4, H4 = 900, 750
img4 = Image.new('RGB', (W4, H4), 'white')
d4 = ImageDraw.Draw(img4)
title_bar(d4, W4, '图3-4  在线对战数据流时序图')

# Column headers
cols = [
    (20,52,140, BLUE, '客户端A\n(房主)'),
    (200,52,100, BLUE, '客户端B\n(加入者)'),
    (380,52,120, (255,152,0), 'Cloud Code\n(C#)'),
    (570,52,120, (76,175,80), 'Cloud\nSave'),
    (730,52,90, (233,30,99), 'UGS\nPush'),
]
for x,y,w,clr,txt in cols:
    d4.rectangle([x,y,x+w,y+30], fill=clr)
    ty = y+4
    for line in txt.split('\n'):
        d4.text((x+4, ty), line, fill='white')
        ty += 12

# Lifelines
for x in [90,250,440,630,775]:
    d4.line([(x,82),(x,740)], fill=(187,222,251), width=1)

y = 100
# Phase 1: HostGame
d4.text((10,y), '阶段1', fill=BLUE)
d4.text((92,y), 'Create Room', fill=GRAY)
d4.line([(90,y+12),(435,130)], fill=BLUE, width=1)
d4.text((180,115), 'CloudCode.HostGame()', fill=BLUE)
d4.line([(440,135),(625,148)], fill=(255,152,0), width=1)
d4.text((500,138), 'CreateLobby(max=2)', fill=(230,81,0))
d4.line([(440,155),(625,168)], fill=(255,152,0), width=1)
d4.text((500,158), 'SetCustomItem(FEN+whiteId)', fill=(230,81,0))
d4.line([(435,175),(95,195)], fill=(255,152,0), width=1)
d4.text((180,182), 'return LobbyCode (6 digits)', fill=(230,81,0))
d4.text((92,200), '显示房间码, 等待对手', fill=GRAY)

# Phase 2: JoinGame
y2 = 230
d4.text((10,y2), '阶段2', fill=GREEN)
d4.text((252,y2), '输入房间码, Join', fill=GRAY)
d4.line([(250,y2+12),(435,250)], fill=GREEN, width=1)
d4.text((300,240), 'JoinGame(code)', fill=GREEN)
d4.line([(440,255),(625,268)], fill=(255,152,0), width=1)
d4.text((500,258), 'JoinLobbyByCode', fill=(230,81,0))
d4.text((440,278), '随机分配黑白方', fill=GRAY)
d4.line([(440,295),(625,308)], fill=(255,152,0), width=1)
d4.text((500,298), 'SetCustomItem(blackId)', fill=(230,81,0))
d4.line([(440,315),(770,335)], fill=(255,152,0), width=1)
d4.text((550,322), 'Push("opponentJoined", B)', fill=(230,81,0))
d4.line([(775,340),(100,365)], fill=(198,40,40), width=1)
d4.text((350,342), '"opponentJoined" -> OnGameStart(FEN)', fill=RED)
d4.text((92,370), '初始化UI, 进游戏', fill=GRAY)
d4.text((252,370), '初始化UI, 进游戏', fill=GRAY)

# Phase 3: MakeMove
y3 = 400
d4.text((10,y3), '阶段3', fill=RED)
d4.text((92,y3), 'A 走棋 e2->e4', fill=GRAY)
d4.line([(90,y3+12),(435,420)], fill=RED, width=1)
d4.text((180,410), 'MakeMove(session,"e2","e4")', fill=RED)
d4.text((440,430), 'GetCustomItems(board, ids)', fill=GRAY)
d4.text((440,448), '校验 turn==white', fill=GRAY)
d4.text((440,466), 'IsValidMove("e2","e4") -> true', fill=GRAY)
d4.text((440,484), 'board.Move(new Move("e2","e4"))', fill=GRAY)
d4.line([(440,500),(625,515)], fill=(255,152,0), width=1)
d4.text((500,505), 'SetCustomItem(board=new_FEN)', fill=(230,81,0))
d4.line([(440,525),(770,545)], fill=(255,152,0), width=1)
d4.text((550,530), 'Push("boardUpdated",FEN) -> B', fill=(230,81,0))
d4.line([(775,550),(260,575)], fill=RED, width=1)
d4.text((470,553), '"boardUpdated" -> OnBoardUpdate(FEN)', fill=RED)
d4.text((252,582), 'SyncBoard(FEN) -> Show History', fill=GRAY)
d4.text((252,600), '切换到B回合 -> Your Turn', fill=GRAY)
d4.text((92,582), '等待对手走棋...', fill=GRAY)

# Phase 4: Resign
y4 = 640
d4.text((10,y4), '阶段4', fill=(158,158,158))
d4.text((252,y4), 'B 认输', fill=GRAY)
d4.line([(250,y4+12),(435,665)], fill=(158,158,158), width=1)
d4.text((330,655), 'Resign(session)', fill=(158,158,158))
d4.text((440,675), 'Resign(Black) -> Save + Push', fill=(158,158,158))

img4.save(r'D:\unity\my_chess\diagrams\png\4_online_dataflow.png')
print('Diagram 4 saved')

print('Done! All 4 diagrams generated.')
