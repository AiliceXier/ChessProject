"""
Convert SVG diagrams to PNG for embedding in docx.
"""
import cairosvg
import os

svg_dir = r'D:\unity\my_chess\diagrams'
png_dir = r'D:\unity\my_chess\diagrams\png'
os.makedirs(png_dir, exist_ok=True)

svgs = [
    '1_system_architecture.svg',
    '2_ai_routing.svg',
    '3_state_transition.svg',
    '4_online_dataflow.svg',
]

for svg_file in svgs:
    svg_path = os.path.join(svg_dir, svg_file)
    png_path = os.path.join(png_dir, svg_file.replace('.svg', '.png'))
    cairosvg.svg2png(url=svg_path, write_to=png_path, output_width=1800, output_height=1300, scale=2.0)
    print(f'Converted: {svg_file} -> {png_path}')

print("All SVGs converted to PNG.")
