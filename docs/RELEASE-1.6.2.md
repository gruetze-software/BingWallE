# BingWallE 1.6.2

## Refined app icon

- Give the maintainer-drawn icon more transparent space around its edges, using
  the Flathub icon grid as a reference.
- Add a blue-gray outline for better separation from desktop and store backgrounds.
- Update the application header, About dialog, Windows ICO, macOS ICNS, Linux and
  Snap icons, and the README icon from the same 1024 px master.
- Refresh the Flathub screenshot links to serve the updated image files directly.
- Restore local image recognition on Intel Macs by using ONNX Runtime 1.23.2
  there; newer NuGet releases no longer supply the required Intel macOS library.
  Windows, Linux and Apple Silicon retain ONNX Runtime 1.30.0.

This patch retains the smaller release packages introduced in 1.6.1.
The app continues to use .NET 10 and Avalonia 12.1.4, with native Wayland and X11
fallback on Linux. The minimum macOS version remains 14.

GitHub releases do not automatically update the Snap Store or Flathub;
those stores are published separately.

## Signed macOS downloads

On 7 November 2026, the macOS ARM64 and Intel ZIP and DMG downloads were replaced
with Developer ID-signed and Apple-notarized packages. Both architectures passed
signature, notarization-ticket and Gatekeeper checks in the
[signed build](https://github.com/gruetze-software/BingWallE/actions/runs/42360123161).
The application version and features are unchanged. The release checksums were
updated; Windows and Linux downloads and the version tag remain unchanged.
