# BingWallE artwork

The current master is `bingwalle.png` (1024 x 1024, RGBA, transparent outside
its rounded square). Christoph Schaefer drew it manually in Paint.NET and
updated its footprint and added a blue-gray outline on 6 November 2026.
Detached export artifacts outside the icon were removed from the PNG master
without changing the connected drawing. The editable source is
`../bingwalle_icon.pdn`. The earlier AI-generated icon was replaced; it remains
in Git history and historical application screenshots.

Run `python scripts/generate-icons.py` from the repository root with Pillow installed.
This derives the 256 px Linux/header PNG, 1024 px About-dialog PNG, Windows
ICO (16-256 px) and macOS ICNS. Only resizing and format conversion are performed;
the drawing and transparency are preserved. Do not regenerate the artwork with AI.

The same app icon is used on Windows, Linux, macOS and Snap, and in the README.
The separate Gruetze-Software publisher logo is unchanged.

For editing in Paint.NET, follow the [German icon guide](../docs/ICON-ANLEITUNG-DE.md).
It uses the [official Flathub icon grid](reference/flathub-app-icon-grid.svg)
as a temporary alignment layer, with guidance on footprint, centering, contrast
and export. Hide the reference layer before exporting the PNG master.

The separate Snap Store banner and its provenance are documented in
[snap-store/README.md](snap-store/README.md). Updating artwork files does not
modify a live store listing or previously published release packages.
