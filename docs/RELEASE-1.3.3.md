# BingWallE 1.3.3

Released on 19 October 2026.

- Move the application, tests and CI to .NET 10 LTS. New macOS packages require
  macOS 14 or later; previous releases are unchanged.
- Use the domain-based Linux AppStream ID `de.gruetze_software.BingWallE` with
  matching DEB desktop and icon names. The executable and settings paths are
  unchanged. Snap retains its package name `bingwalle`.
- Add a German/English Ko-fi coffee request to the information dialog.
- Include image metadata details and donation links in the store copy and
  upstream AppStream metadata. Preserve the README screenshots.
- Declare ICU and OpenSSL dependencies in future DEB packages and document Mint
  installation and manual validation.

This release provides the upstream source and desktop metadata for Flatpak
packaging. Local Flatpak functionality was tested by the maintainer. A Flathub
submission and acceptance are separate; this release does not publish to Flathub
or change the current Snap Store release.
