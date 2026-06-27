import zipfile, re, sys

path = R'D:\unity\my_chess\课程设计报告.docx'
with zipfile.ZipFile(path, 'r') as z:
    with z.open('word/document.xml') as f:
        raw = f.read()

# decode with utf-8 (docx XML uses UTF-8)
content = raw.decode('utf-8')
# extract text from w:t elements
out = []
for m in re.finditer(r'<w:t[^>]*>([^<]*)</w:t>', content):
    out.append(m.group(1))
# write output
out_path = R'D:\unity\my_chess\docx_verify.txt'
with open(out_path, 'w', encoding='utf-8') as f:
    f.write('\n'.join(out[:200]))
print(f'Extracted {len(out)} text elements, showing first 200:')
