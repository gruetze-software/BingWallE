"""Regenerate app icons from the SVG. Requires Pillow and resvg-py."""
from pathlib import Path
from io import BytesIO
import sys
from PIL import Image

root = Path(__file__).resolve().parents[1]
# Optional local tool directory used during development.
local_tools = root / 'artifacts' / 'logo-tools'
if local_tools.exists():
    sys.path.insert(0, str(local_tools))
import resvg_py

png = resvg_py.svg_to_bytes(svg_path=str(root / 'branding' / 'bingwalle.svg'), width=1024, height=1024)
master = Image.open(BytesIO(png)).convert('RGBA')
assets = root / 'BingWallE' / 'Assets'
master.save(root / 'branding' / 'bingwalle.png')
master.resize((256, 256), Image.Resampling.LANCZOS).save(assets / 'bingwalle.png')
master.save(assets / 'bingwalle.ico', sizes=[(16,16),(24,24),(32,32),(48,48),(64,64),(128,128),(256,256)])
master.save(assets / 'bingwalle.icns')
print('Generated PNG, Windows ICO and macOS ICNS from branding/bingwalle.svg')
