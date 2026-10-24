# BingWallE 1.5.0

Released on 24 October 2026.

GitHub releases do not automatically update the Snap Store or Flathub.

- Upgrade the application, UI tests and screenshot tool to Avalonia 12.1.4.
  The application continues to use .NET 10; the minimum macOS version remains 14.
- Add native Wayland on compatible Linux sessions with automatic X11 fallback.
  Set the same desktop application identity on both backends and declare the
  required native Wayland/EGL dependencies in the DEB package.
- Improve the image preview: narrower layout, aspect-ratio-preserving image,
  space for the scrollbar, and a save status that remains visible while scrolling.
  Hide the download checkbox for already saved images.
- Keep expected HTTP cancellation logs concise while retaining error details
  for other failures.
- Provide `scripts/install-flatpak.sh` as the upstream offline Linux build/install
  entry point, including desktop integration and third-party license notices.
- Add native Wayland, X11 and automatic fallback smoke checks, updated xUnit v3
  tests, preview layout checks and a regression test for valid XMP language fields.

Windows and macOS retain their existing desktop backend selection. Existing
settings and image files remain compatible. XMP language qualifiers shown by some
Linux file managers are a reader display issue; image metadata stays standards-compliant.

GitHub builds provide packages for Windows x64, Linux x64 and macOS ARM64/x64.
Real Flatpak sandbox testing and interactive macOS validation are separate from
these build checks. Snap Store publishing remains a separate maintainer step.
