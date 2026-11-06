# BingWallE 1.6.2

## Refined app icon

- Give the maintainer-drawn icon more transparent space around its edges, using
  the Flathub icon grid as a reference.
- Add a blue-gray outline for better separation from desktop and store backgrounds.
- Update the application header, About dialog, Windows ICO, macOS ICNS, Linux and
  Snap icons, and the README icon from the same 1024 px master.
- Refresh the Flathub screenshot links to serve the updated image files directly.

This patch retains the smaller release packages introduced in 1.6.1.
The app continues to use .NET 10 and Avalonia 12.1.4, with native Wayland and X11
fallback on Linux. The minimum macOS version remains 14.

GitHub releases do not automatically update the Snap Store or Flathub;
those stores are published separately.
