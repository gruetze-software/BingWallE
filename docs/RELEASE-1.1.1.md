# BingWallE 1.1.1

- Each image card and large preview now shows its download resolution; completed downloads show their actual pixel dimensions.
- Downloads include Bing titles, descriptions, copyright and source information in EXIF and XMP metadata, including Windows Explorer's Details fields. The JPEG image is not recompressed.
- Bing publication dates are labeled in comments. Unknown capture dates, authors and ratings are not invented; existing camera and photographer metadata is preserved.
- Optional local subject recognition adds English image tags. Enable it in Settings. The first use downloads a checksum-verified 14 MB MobileNetV2 model; images stay on your computer.
- Only model scores of at least 50% become tags. Predictions can be wrong and are identified as AI suggestions in the metadata. Uncertain images receive no subject tags.
- Patch versions are now visible in the app title and header.

Existing image files are not modified automatically. Select and download them again to add metadata.
Settings from 1.0 and 1.1 remain compatible; local tagging is off until enabled.

The release includes Windows x64 ZIP, Linux x64 DEB/TAR.GZ and macOS Intel/Apple Silicon ZIP/DMG packages,
plus SHA256SUMS.txt. Each package includes .NET and the local inference runtime; the model downloads separately.

macOS packages are ad-hoc signed but not Apple-notarized. Windows packages are unsigned.
See the [README](https://github.com/gruetze-software/BingWallE/blob/v1.1.1/README.md) for installation and macOS first-launch help.
