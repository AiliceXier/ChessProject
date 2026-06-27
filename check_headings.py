import re

with open('D:/unity/my_chess/full_report.md', 'r', encoding='utf-8') as f:
    content = f.read()

# Check all headings
headings = re.findall(r'^#{1,4}\s+(.+)', content, re.MULTILINE)
result = []
for h in headings:
    # Check for key patterns
    if any(kw in h for kw in ['3.5', 'A.5', 'Plan', 'Git', '工作流', '游戏引擎', 'AI 算法', '在线对战', '云服务', '方案对比']):
        result.append(f'FOUND: {h}')

# Also check for the literal text in the docx
with open('D:/unity/my_chess/verify_result.txt', 'w', encoding='utf-8') as f:
    f.write('\n'.join(result) if result else 'NOTHING FOUND')
    f.write('\n\n---\n\n')
    # show all headings
    f.write('\n'.join(headings))

print('Done')
