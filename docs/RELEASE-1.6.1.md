# BingWallE 1.6.1

Released on 1 November 2026.

## Smaller, cleaner packages

- Publish the Flatpak app specifically for Linux x64 or ARM64 instead of including
  native libraries for every supported operating system and architecture.
- Reduce the uncompressed Linux app publish from approximately 798 MiB to 54 MiB
  in a local comparison, about 93% less. The .NET runtime and shared Flatpak runtime
  are additional; these figures are not store download sizes.
- Exclude Windows ONNX native libraries from Linux and macOS packages.
- Remove dependency debug symbols and link-time files from all release packages,
  including files that would otherwise be embedded in single-file executables.
- Check release archive contents in CI before upload, including extracted DEB and
  Snap app payloads, to prevent packaging regressions.

My collection, editable and personal keywords, local recognition and metadata
preservation continue to work as in 1.6.0. Visual Studio debug builds retain their
symbols.

The application continues to use .NET 10 and Avalonia 12.1.4, with native Wayland
and automatic X11 fallback on Linux. The minimum macOS version remains 14.
Packages are provided for Windows x64, Linux x64, and macOS ARM64/x64.
GitHub releases do not automatically update the Snap Store or Flathub;
those stores are published separately.
