import zipfile, re, sys

path = R'D:\unity\my_chess\课程设计报告.docx'
with zipfile.ZipFile(path, 'r') as z:
    with z.open('word/document.xml') as f:
        content = f.read().decode('utf-8')

out = []
for m in re.finditer(r'<w:t[^>]*>([^<]*)</w:t>', content):
    out.append(m.group(1))
full = '\n'.join(out)

checks = [
    '3.5.1 游戏引擎',
    '3.5.2 AI 算法',
    '3.5.3 在线对战',
    '3.5.4 云服务',
    '模块1：走法校验',
    '模块2：AI走棋',
    '模块3：在线对战',
    '模块4：排行榜',
    'A.2 怎么提需求',
    'A.3 怎么测试AI完成的结果',
    'A.4 怎么要求调整优化',
    'A.5 AI 工作流总结',
    'A.6 心得体会',
    'Plan 先行',
    'Unity MCP',
    'Git 分步管理',
]

result = []
for c in checks:
    count = full.count(c)
    status = 'OK' if count > 0 else 'MISSING'
    result.append(f'{status}: {c} ({count}x)')

result.append(f'\nTotal elements: {len(out)}, total chars: {len(full)}')

# Write to file with UTF-8
with open(R'D:\unity\my_chess\verify_result.txt', 'w', encoding='utf-8') as f:
    f.write('\n'.join(result))
print('Done')
