# BingWallE 1.6.4

Give the image gallery more space with a compact navigation row.

- Move the region summary and Refresh button directly beside **Discover** and
  **My collection**, replacing the wide panel that previously occupied a full row.
- Use the same compact layout for the local folder summary in My collection.
  Long folder paths are shortened visually; the full path remains in the tooltip.
- Keep the controls aligned and usable in German and English, including narrow
  windows. Refresh and cancellation continue to work in both views.

The app continues to use .NET 10 and Avalonia 12.1.4. macOS packages are Developer
ID signed and notarized by Apple, with macOS 14 as the minimum version. Linux
retains native Wayland with automatic X11 fallback.

GitHub releases do not automatically update the Snap Store or Flathub. Those
stores are published separately.
