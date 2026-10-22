# BingWallE artwork

The current master is `bingwalle.png` (1024 x 1024, RGBA, transparent outside
its rounded square). Christoph Schaefer drew it manually in Paint.NET and
approved the final exports on 22 October 2026. The editable source is
`../bingwalle_icon.pdn`. The earlier AI-generated icon was replaced; it remains
in Git history and historical application screenshots.

Run `python scripts/generate-icons.py` from the repository root with Pillow installed.
This derives the 256 px Linux/header PNG, 1024 px About-dialog PNG, Windows
ICO (16-256 px) and macOS ICNS. Only resizing and format conversion are performed;
the drawing and transparency are preserved. Do not regenerate the artwork with AI.

The same app icon is used on Windows, Linux, macOS and Snap, and in the README.
The separate Gruetze-Software publisher logo is unchanged.

The Snap Store banner is composed from the hand-drawn icon and text by
`scripts/generate-store-banner.py`. Updating these files does not modify a live
store listing or previously published release packages.
