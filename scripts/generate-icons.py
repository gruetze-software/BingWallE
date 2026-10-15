"""Regenerate platform icons from the approved PNG master. Requires Pillow."""
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[1]
local_tools = root / 'artifacts' / 'logo-tools'
if local_tools.exists():
    sys.path.insert(0, str(local_tools))
from PIL import Image

source = root / 'branding' / 'bingwalle.png'
with Image.open(source) as image:
    if image.width != image.height or image.mode != 'RGBA':
        raise ValueError('The logo master must be a square RGBA PNG with transparency.')
    master = image.copy()
assets = root / 'BingWallE' / 'Assets'
assets.mkdir(parents=True, exist_ok=True)
# Preserve the approved artwork and alpha; use Lanczos for format-specific downsampling.
master.resize((256, 256), Image.Resampling.LANCZOS).save(assets / 'bingwalle.png', optimize=True)
large = master.resize((1024, 1024), Image.Resampling.LANCZOS)
large.save(assets / 'bingwalle-logo.png', optimize=True)
large.save(assets / 'bingwalle.ico', sizes=[(16,16),(24,24),(32,32),(48,48),(64,64),(128,128),(256,256)])
large.save(assets / 'bingwalle.icns')
print('Generated PNG, high-resolution logo, Windows ICO and macOS ICNS from branding/bingwalle.png')
