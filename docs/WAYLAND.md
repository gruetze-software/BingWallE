# Native Wayland readiness

Checked on 22 October 2026 against the official Avalonia documentation.

## Current application

BingWallE 1.4.1 uses Avalonia 11.3.20 with `UsePlatformDetect()`. On Linux this
selects X11. On Wayland desktops the app can run through XWayland; this is not a
native Wayland backend. No backend or package permissions have been changed by
the upstream build-script work.

## Available upstream support

Avalonia provides the optional `Avalonia.Wayland` backend starting with 12.1.0.
It needs an explicit `UseWayland()` call. According to the Linux guide, there is
no automatic fallback if a Wayland compositor is unavailable, and the backend
still has feature limitations. Merely changing sandbox socket permissions does
not switch BingWallE to native Wayland.

## Migration and validation work

1. Upgrade the Avalonia packages together to a maintained 12.1-or-later release.
   Review the breaking changes: Diagnostics has been replaced and the headless
   xUnit integration now uses xUnit 3. Update the app and test tooling together.
2. Add the matching Wayland package and select it only on Linux when the intended
   session offers a Wayland display. Keep Windows, macOS and Linux X11 startup
   paths. Check actual initialization failures rather than assuming that an
   environment variable guarantees a usable compositor.
3. Validate a real Wayland session under GNOME and KDE: rendering, scaling,
   window state, icons, dialogs, context menus, clipboard and keyboard focus.
4. Test portal folder selection, retained folder access after restart, image
   downloads, external URLs and file-manager opening inside the actual Flatpak.
5. Repeat existing Windows, macOS and X11 tests, including virtual machines
   without accelerated graphics and local recognition shutdown.
6. Only after the application is verified, have the human package maintainer
   adjust the Flatpak display permissions and validate the updated package.

This is an investigation and test plan, not an implemented migration or a claim
of native Wayland support. No new release is needed for this document.

## Sources

- [Avalonia Desktop Linux / Wayland](https://docs.avaloniaui.net/docs/platform-specific-guides/linux)
- [Supported platforms](https://docs.avaloniaui.net/docs/supported-platforms)
- [Breaking changes in Avalonia 12](https://docs.avaloniaui.net/docs/avalonia12-breaking-changes)
