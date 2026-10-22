# Avalonia 12 / native Wayland development branch

Branch: `codex/avalonia12-wayland`. This migration is not released or merged into
`main`. Published BingWallE 1.4.1 still uses Avalonia 11.3.20 / X11.

## Implemented

- Upgrade the app, headless tests and screenshot tool to Avalonia 12.1.4.
- Migrate the UI tests to xUnit 3.2.2, including cancellation and source metadata.
- Remove Avalonia's retired validation plugin configuration and the unused,
  retired Diagnostics package. Keep the CommunityToolkit validation behavior.
- Configure native Wayland with Avalonia's `UseWaylandWithFallback()` after the
  default desktop backend. Failed Wayland initialization falls back to X11.
- Set the Linux application identity to `de.gruetze_software.BingWallE` for native
  Wayland and X11 desktop integration.
- Preserve Windows and macOS platform selection.
- Update the Skia image-resize call to the equivalent Mitchell cubic sampler
  required by SkiaSharp 3, and the screenshot PNG encoder API.
- Declare the native Wayland/EGL dependencies in the DEB package.

## Testing the backend

On Linux, the default is `auto`: try Wayland and fall back to X11 if initialization
fails. No backend override is needed for normal use. For targeted testing:

```bash
BINGWALLE_LINUX_BACKEND=wayland dotnet run --project BingWallE -c Release
BINGWALLE_LINUX_BACKEND=x11 dotnet run --project BingWallE -c Release
```

The forced Wayland option intentionally reports an initialization failure instead
of silently using X11. With no override (or `auto`), fallback is enabled. Desktop
startup logs the actual window implementation, so a running XWayland window is
not mistaken for a native Wayland test. Windows and macOS ignore this Linux-only
environment variable.

## Automated checks

The Linux CI job runs the real main window, Settings and About under a disposable
headless Weston compositor with software EGL rendering and an Xvfb X server. The
shared application backend configuration is used; the test tool replaces settings
storage and disallows HTTP requests, protecting personal data and avoiding downloads.
It verifies native Wayland in automatic/forced modes, forced X11, and automatic
X11 fallback when the configured Wayland display is missing. Logs are uploaded as
`linux-display-logs`. Run the same checks locally with:

```bash
bash scripts/test-linux-backends.sh
```

The script needs .NET 10, Weston, Xvfb, GNU timeout and the Mesa/Wayland native
libraries. This is a window-creation and shutdown smoke test, not full interactive
acceptance testing and not a Flatpak sandbox test.

## Still required before release

- Test a real GNOME and KDE Wayland session: input, context menus, scaling,
  window state, icons and dialog ownership/focus. Wayland compositors control
  window placement, so absolute centering cannot be assumed.
- Build a real Flatpak from this branch and test portal folder selection, retained
  folder access after restart, downloads, browser links and file-manager opening.
- Test the existing Windows and macOS UI manually and repeat X11 VM checks.
- Native Wayland still has upstream feature limitations; keep X11 fallback and
  use the explicit X11 override if a compositor/driver-specific problem occurs.

The existing Flathub manifest is untouched. Its human maintainer must choose
appropriate sandbox sockets, update downloaded NuGet inputs for the new packages,
and validate the resulting Flatpak after the application tests are successful.
The new source entry point remains `scripts/install-flatpak.sh`.

## Sources

- [Avalonia Linux / Wayland](https://docs.avaloniaui.net/docs/platform-specific-guides/linux)
- [Avalonia 12 breaking changes](https://docs.avaloniaui.net/docs/avalonia12-breaking-changes)
- [Fallback implementation in 12.1.4](https://github.com/AvaloniaUI/Avalonia/blob/12.1.4/src/Avalonia.Wayland/AvaloniaWaylandPlatformExtensions.cs)
