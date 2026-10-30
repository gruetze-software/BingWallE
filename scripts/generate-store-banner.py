"""Compose a plain-layout alternative from the hand-drawn icon and plain text.

Requires Pillow. This does not upload or alter a store listing.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parents[1]


def font(size, bold=False):
    candidates = ([Path('C:/Windows/Fonts/segoeuib.ttf')] if bold else
                  [Path('C:/Windows/Fonts/segoeui.ttf')])
    candidates.append(Path('/usr/share/fonts/truetype/dejavu') /
                      ('DejaVuSans-Bold.ttf' if bold else 'DejaVuSans.ttf'))
    for path in candidates:
        if path.exists():
            return ImageFont.truetype(str(path), size)
    return ImageFont.load_default(size=size)


banner = Image.new('RGB', (1800, 600), '#173B50')
with Image.open(root / 'branding/bingwalle.png') as source:
    icon = source.convert('RGBA').resize((520, 520), Image.Resampling.LANCZOS)
banner.paste(icon, (40, 40), icon)
draw = ImageDraw.Draw(banner)
draw.text((620, 145), 'BingWallE', font=font(112, bold=True), fill='white')
draw.text((625, 300), 'Discover. Choose. Keep.', font=font(46), fill='#8DE3DE')
draw.text((625, 410), 'Grütze-Software', font=font(30), fill='white')
target = root / 'artifacts/snap-store/banner-simple.png'
target.parent.mkdir(parents=True, exist_ok=True)
banner.save(target, optimize=True)
print(f'Wrote {target.relative_to(root)} (1800 x 600)')
