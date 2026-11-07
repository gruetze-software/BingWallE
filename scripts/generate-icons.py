"""Convert the maintainer's separate desktop and Apple PNGs. Requires Pillow."""
from pathlib import Path
import shutil
import sys

root = Path(__file__).resolve().parents[1]
local_tools = root / 'artifacts' / 'logo-tools'
if local_tools.exists():
    sys.path.insert(0, str(local_tools))
from PIL import Image

source = root / 'bingwalle_icon_1024.png'
with Image.open(source) as image:
    if image.width != image.height or image.mode != 'RGBA':
        raise ValueError('The logo master must be a square RGBA PNG with transparency.')
    master = image.copy()
with Image.open(root / 'bingwalle_icon_apple.png') as image:
    if image.size != (1024, 1024):
        raise ValueError('The Apple icon must be a 1024 x 1024 PNG.')
    apple = image.convert('RGBA')
assets = root / 'BingWallE' / 'Assets'
assets.mkdir(parents=True, exist_ok=True)
# The root PNG exports are artist-maintained inputs, never overwrite them.
shutil.copyfile(source, root / 'branding' / 'bingwalle.png')
frames = []
for size in (16, 24, 32, 48, 64, 128, 256):
    export = root / f'bingwalle_icon_{size}.png'
    if size in (32, 64, 128, 256):
        with Image.open(export) as image:
            if image.size != (size, size):
                raise ValueError(f'Unexpected dimensions: {export.name}')
            frames.append(image.convert('RGBA'))
    else:
        frames.append(master.resize((size, size), Image.Resampling.LANCZOS))
frames[-1].save(assets / 'bingwalle.png', optimize=True)
large = master.resize((1024, 1024), Image.Resampling.LANCZOS)
large.save(assets / 'bingwalle-logo.png', optimize=True)
large.save(assets / 'bingwalle.ico', sizes=[frame.size for frame in frames], append_images=frames)
apple.save(assets / 'bingwalle.icns')
apple.resize((256, 256), Image.Resampling.LANCZOS).save(assets / 'bingwalle-apple.png', optimize=True)
apple.save(assets / 'bingwalle-apple-logo.png', optimize=True)
print('Generated Windows/Linux assets from desktop PNGs; macOS ICNS and app logos from the Apple PNG.')
