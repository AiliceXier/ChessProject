import zipfile, re, sys

path = r'D:\unity\my_chess\软件II课程设计项目说明V2.2 - 段景山班级.docx'
with zipfile.ZipFile(path, 'r') as z:
    with z.open('word/document.xml') as f:
        raw = f.read()

# The output looked like ISO-8859. Try GBK/GB18030
# Force the encoding based on actual char test
for enc in ['gb18030', 'gbk', 'gb2312', 'utf-8', 'big5']:
    try:
        s = raw.decode(enc)
        # check if it parses
        # Look for first w:t content
        m = re.search(r'<w:t[^>]*>([^<]*)</w:t>', s)
        if m and len(m.group(1)) > 0:
            sample = m.group(1)
            if all('一' <= c <= '鿿' or ord(c) < 128 for c in sample):
                sys.stderr.write(f"SUCCESS with {enc}: {sample}\n")
                content = s
                break
    except Exception as e:
        sys.stderr.write(f"{enc}: {e}\n")

# Just decode as gb18030 (which is a superset of GBK)
if 'content' not in dir():
    content = raw.decode('gb18030', errors='replace')

# walk all w:t text
out = []
for m in re.finditer(r'<w:t[^>]*>([^<]*)</w:t>', content):
    out.append(m.group(1))

# write to file with explicit UTF-8 BOM-free encoding
import io
with open(r'D:\unity\my_chess\docx_content.txt', 'w', encoding='utf-8') as f:
    f.write('\n'.join(out))
sys.stderr.write("Wrote file\n")
