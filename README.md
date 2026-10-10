<p align="center">
  <img src="branding/bingwalle.png" width="144" alt="BingWallE: landscape pictures with a golden sun" />
</p>

# BingWallE 1.0

[Download the latest release](https://github.com/gruetze-software/BingWallE/releases/latest) ·
[Builds and tests](https://github.com/gruetze-software/BingWallE/actions/workflows/build.yml)

BingWallE is a desktop collector for Bing wallpapers, built with C#, .NET 8 and
Avalonia 11.3.10. The interface is available in **German and English**. Open **Einstellungen / Settings** to select the application language, Bing download
region, resolution and theme. Language changes take effect immediately; your choice is saved for the next start.
The download region is independent of the application language: choose Germany
for German Bing titles and filenames, or the USA / United Kingdom for English content.
Use Refresh after changing the region.

## Features

- A gallery with two or three columns, large image cards and readable dates.
- Click an image to open a large preview with its description and copyright.
  Close the preview using the close button or Escape.
- Select all images, none, or only new ones; saved images are marked.
- Choose from twelve Bing regions, including Germany, the USA, Japan and France.
- Download HD, Full HD or UHD images. Availability depends on Bing; unavailable
  resolutions produce an error instead of silently falling back to a smaller image.
- Selection counts, download progress, cancellation and retry after errors.
- Choose a destination folder and open it directly in your file manager.
- Light, dark or system appearance, with persistent preferences.

Each region provides up to eight daily images. After switching regions, use
**Aktualisieren** (Refresh) to load the new selection. Changing the resolution or
destination refreshes file detection: the saved status refers to the selected
resolution in the current folder. HD and UHD use separate filenames to avoid
overwriting existing Full HD images.

A download error stops the batch. Successfully saved images remain deselected;
**Erneut versuchen** (Retry) downloads only the remaining selection. Downloads use
temporary files to protect existing images if an operation fails.

BingWallE focuses on collecting images. Automatic downloads and changing the
desktop wallpaper are not implemented.

## Downloads and first start

Download the package for your platform from the
[latest release](https://github.com/gruetze-software/BingWallE/releases/latest).
**The .NET runtime is included; no separate runtime or SDK installation is required.**

| Platform | Package |
| --- | --- |
| Windows x64 | `BingWallE-1.0.0-win-x64.zip` |
| Linux x64 | `BingWallE-1.0.0-linux-x64.deb` or `.tar.gz` |
| macOS Apple Silicon | `BingWallE-1.0.0-macos-arm64.dmg` or `.zip` |
| macOS Intel | `BingWallE-1.0.0-macos-x64.dmg` or `.zip` |

On Windows, extract the ZIP and run `bingwalle.exe`. Windows packages are not
signed with a code-signing certificate.

On Linux, install the DEB on Debian/Ubuntu or extract the TAR.GZ and run
`./bingwalle`. If needed, mark the extracted executable with `chmod +x bingwalle`.
A graphical desktop session and the native Avalonia/.NET dependencies are required.
The DEB also installs the application icon and desktop entry.

All four targets are built and tested by GitHub Actions. The graphical application
has not been manually tested on every target device. `SHA256SUMS.txt` in the release
contains checksums for all packages.

### macOS installation and troubleshooting

Choose **arm64** for Apple Silicon or **x64** for an Intel Mac. Open the DMG and drag
**BingWallE.app** onto the **Applications** shortcut. ZIP packages are also available;
extract the app and move it to Applications.

The macOS bundles are **ad-hoc signed and verified during the release build**.
They are not signed with an Apple Developer certificate or notarized by Apple.
macOS may therefore block the first launch or report that the app is damaged.

First try opening the app, then allow it in **System Settings → Privacy & Security →
Open Anyway**, if that option is offered. Only do this for a copy downloaded from the
[official BingWallE release](https://github.com/gruetze-software/BingWallE/releases/latest)
that you trust.

If the downloaded app is still blocked by quarantine, the same app-specific
workaround documented for AmiDiskLab can be used in Terminal:

```bash
xattr -dr com.apple.quarantine "/Applications/BingWallE.app"
```

This removes the quarantine attribute from this app bundle only; it does not
notarize the app or disable Gatekeeper globally. If the app is elsewhere, replace
the path. You can type `xattr -dr com.apple.quarantine ` with a trailing space and
drag **BingWallE.app** into Terminal to insert its path. Start the app again afterwards.

If it still fails to start, check that you selected the correct architecture and
try launching the executable from Terminal to capture diagnostic output:

```bash
"/Applications/BingWallE.app/Contents/MacOS/bingwalle"
```

Include your macOS version, Mac architecture and the error output when reporting
an issue. A quarantine workaround does not resolve incompatible systems or damaged downloads.

## Build from source

The **.NET 8 SDK** is recommended. The repository's `global.json` also allows a newer
stable SDK if no matching .NET 8 SDK is installed. The application and tests still
target .NET 8; `dotnet run` and `dotnet test` require the .NET 8 runtime.

Run the following commands from the repository root:

```sh
dotnet restore BingWallE.sln
dotnet build BingWallE.sln -c Release
dotnet run --project BingWallE -c Release
```

## Tests

```sh
dotnet test BingWallE.sln -c Release
```

The xUnit tests cover selection, resolution variants, preview cancellation, retries,
cancellation, error handling, filenames, Bing responses, preferences and folder changes.
Network responses are simulated and files are created in isolated temporary folders.
Tests do not change personal preferences or images and do not replace manual UI testing.

## Project structure

| Area | Responsibility |
| --- | --- |
| `Models` | Preferences, image data and cross-platform filenames |
| `Services/BingWallpaperClient` | Fetch Bing archive entries and previews |
| `Services/WallpaperStore` | Detect existing images and save downloads safely |
| `Services/JsonSettingsStore` | Load preferences and save them through a temporary file |
| `Services/AvaloniaAppDialogs` | Folder picker and About dialog |
| `ViewModels` | User interactions, selection, status messages and previews |
| `Views` | Avalonia user interface |
| `App.axaml.cs` | Create services and connect them to the view model |
| `BingWallE.Tests` | Automated tests with replaceable services |

Services are passed to the view model through interfaces, allowing network and file
operations to be tested independently of the graphical interface.

Preferences are stored under `Environment.SpecialFolder.ApplicationData` in
`bingwalle/settings.json`, typically `%APPDATA%/bingwalle/settings.json` on Windows.
The default download destination is `BingImages` inside the Pictures folder.

## Build release packages

The scripts read the version from `BingWallE/BingWallE.csproj` and write their output
to `artifacts/`. The app displays the major and minor version: package version
`1.0.0` appears as **1.0**. Every package includes the .NET runtime.

| Platform | Command on that platform | Output |
| --- | --- | --- |
| Windows | `pwsh -File scripts/build-windows.ps1` | ZIP containing `bingwalle.exe` |
| Linux | `bash scripts/build-linux.sh` | TAR.GZ and DEB |
| macOS Apple Silicon | `bash scripts/build-macos.sh arm64` | APP bundle in ZIP and DMG |
| macOS Intel | `bash scripts/build-macos.sh x64` | APP bundle in ZIP and DMG |

Windows and Linux scripts also accept ARM64 using `-Architecture arm64` and the
`arm64` argument respectively; these variants are not published in the first release.
Linux packaging requires `dpkg-deb`. The older `BingWallE/build.sh` entry point calls
the appropriate platform script. macOS packaging requires macOS and its `codesign`,
`hdiutil` and `ditto` tools.

## Automated builds and releases

`.github/workflows/build.yml` builds and tests on Windows, Linux and macOS.
It creates Windows ZIP, Linux TAR.GZ/DEB and macOS ZIP/DMG packages for Intel and
Apple Silicon. Test reports and packages are available as workflow artifacts.

Version 1.0 uses the `v1.0` tag. Its tag build publishes the GitHub release only after
all four build variants succeed. All packages and `SHA256SUMS.txt` are attached.
Release notes are available in [docs/RELEASE-1.0.md](docs/RELEASE-1.0.md) (German).

## Diagnostic output

When starting with F5 in Visual Studio, open **View → Output** and select **Debug**
in **Show output from**.

Traces include timestamps, an identifier per HTTP request, the URL, HTTP status,
duration and byte count. `START` without `HEADERS` means no response headers have
arrived yet. `HEADERS` without `COMPLETE` means the response body is still being
transferred. Errors include stack traces. Image contents and HTTP headers are not logged.

Each HTTP request has a 30-second timeout. The interface distinguishes user cancellation
from a timeout. During a scan, the status line shows the current day and operation
(image metadata or preview).

## License

The source code is released under the [MIT License](LICENSE). Downloaded images
remain subject to their respective rights holders' terms; copyright information
is shown in the preview.
