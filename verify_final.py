import zipfile, re

path = R'D:\unity\my_chess\课程设计报告.docx'
with zipfile.ZipFile(path, 'r') as z:
    with z.open('word/document.xml') as f:
        content = f.read().decode('utf-8')

out = []
for m in re.finditer(r'<w:t[^>]*>([^<]*)</w:t>', content):
    out.append(m.group(1))
full = '\n'.join(out)

# Check for content that should be present
checks = [
    # 方案对比 (using actual sub-heading phrases)
    ('游戏引擎选型', '3.5.x Games'),
    ('AI 算法选型', '3.5.x AI'),
    ('在线对战架构', '3.5.x Online'),
    ('云服务平台：华为云', '3.5.x Cloud'),
    ('华为云 ECS', 'Huawei ECS'),
    ('Plan先行', 'Plan section'),
    ('Git分步管理', 'Git section'),
    ('AI工作流总结', 'A.5 Workflow'),
    ('Unity MCP', 'MCP section'),
    ('多模态 LLM', 'Multimodal'),
    # Test tables
    ('TC-CHESS-01', 'Chess test cases'),
    ('TC-AI-01', 'AI test cases'),
    ('TC-ONLINE-01', 'Online test cases'),
    ('TC-LB-01', 'Leaderboard test cases'),
    # Appendix sections
    ('A.2 怎么提需求', 'A.2'),
    ('A.3 怎么测试AI完成的结果', 'A.3'),
    ('A.4 怎么要求调整优化', 'A.4'),
    ('A.5 AI工作流总结', 'A.5'),
    ('A.6 心得体会', 'A.6'),
    ('附录B', 'Appendix B'),
    ('Player.cs', 'Player.cs'),
    ('ChessBoard.cs', 'ChessBoard.cs'),
    ('server.js', 'server.js'),
]

results = []
for search, label in checks:
    count = full.count(search)
    status = 'OK' if count > 0 else 'MISSING'
    results.append(f'{status}: {label} ({search[:30]})')

results.append(f'\nTotal: {len(out)} elements, {len(full)} chars')

with open(R'D:\unity\my_chess\verify_result.txt', 'w', encoding='utf-8') as f:
    f.write('\n'.join(results))
print('Done')
