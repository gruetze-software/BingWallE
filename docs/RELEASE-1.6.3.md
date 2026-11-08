# BingWallE 1.6.3

Search your saved collection, review keywords more easily and use the refreshed
platform icons, including the dedicated macOS design.

- Search **My collection** by title, description, filename and keywords, including
  personal tags. Combine filters for image source, minimum HD / Full HD / UHD
  resolution and the last 7, 30 or 365 days. Portrait images are supported.
- See matching and total image counts, reset filters and retain the search when
  switching between Discover and My collection during the same session.
- Add multiple keywords separated by semicolons, such as `night;stars`. Trim
  spaces, skip duplicate entries and validate the 100-character limit per tag.
- Copy a reduced image preview (up to 1024 pixels) and an AI keyword prompt
  separately into your preferred chat. On-screen instructions explain the steps.
  BingWallE uploads nothing; review the returned tags before adding and saving.
- Keep automatic subject and category tags from titles and descriptions, together
  with location names. AI-chat suggestions and personal tags supplement them.
  Do not automatically add Bing, Wallpaper or other source names as keywords;
  the source stays recorded separately in metadata. Legacy source keywords are
  removed from JPEG keyword fields only when you explicitly save reviewed tags.
- Remove local image recognition, its settings, ONNX Runtime and the model
  download. Clean up the retired model cache on startup without removing other
  files. Existing personal keywords and image metadata are preserved.
- Update Windows/Linux artwork and use the separate Apple icon in macOS app
  bundles, the Dock, application header and About dialog. The README and icon
  generation guide are updated.

macOS packages are Developer ID signed and notarized by Apple. The app continues
to use .NET 10 and Avalonia 12.1.4; the minimum macOS version remains 14. Linux
retains native Wayland with automatic X11 fallback. Packages are provided for
Windows x64, Linux x64 and macOS ARM64/x64.

GitHub releases do not automatically update the Snap Store or Flathub. Those
stores are published separately.
