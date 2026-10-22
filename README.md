<p align="center">
  <img src="branding/bingwalle.png" width="192" alt="BingWallE: hand-drawn turquoise mountains, a winding river and a yellow sun on a dark blue rounded square" />
</p>

# BingWallE 1.4.1

Collect Bing daily images and optional Windows Spotlight selections in one gallery.
Version 1.4.1 introduces a new app icon drawn by the maintainer in Paint.NET.

**Development branch:** the Avalonia 12 / native Wayland migration is not part of
the published 1.4.1 release. See [Wayland testing](docs/WAYLAND.md).

See [what is new in 1.4.1](docs/RELEASE-1.4.1.md).

<img width="2306" height="1920" alt="grafik" src="https://github.com/user-attachments/assets/07d19ada-e73a-49aa-b36c-bb1121cbaf05" />

<img width="1304" height="1760" alt="grafik" src="https://github.com/user-attachments/assets/b1f7b0dc-14e7-48b3-851e-2dfcf2530e0f" />

[Download the latest release](https://github.com/gruetze-software/BingWallE/releases/latest) ·
[Builds and tests](https://github.com/gruetze-software/BingWallE/actions/workflows/build.yml)

BingWallE is a desktop collector for Bing and Windows Spotlight wallpapers, built with C#, .NET 10 and
Avalonia 12.1.4 in this development branch. The interface is available in **German and English**. Open **Einstellungen / Settings** to select the application language, image download
region, Bing resolution, optional Windows Spotlight images and theme. Language changes take effect immediately; your choice is saved for the next start.
The download region is independent of the application language: choose Germany
for German image titles and descriptions, or the USA / United Kingdom for English content.
This region also controls the optional Spotlight source.
Use Refresh after changing the region.

## Features

- A responsive gallery with large image cards, source labels and readable dates.
- Click an image to open a large preview with its description and copyright.
  Close the preview using the close button or Escape.
- Select all images, none, or only new ones; saved images are marked.
- Choose from twelve image regions, including Germany, the USA, Japan and France.
- Optionally include Windows Spotlight images alongside Bing in the gallery (off by
  default). Source labels appear on cards and previews. Spotlight downloads keep the
  supplied original resolution, independently of the Bing HD / Full HD / UHD setting.
- Download HD, Full HD or UHD images. Availability depends on Bing; unavailable
  resolutions produce an error instead of silently falling back to a smaller image.
- Selection counts, download progress, cancellation and retry after errors.
- Choose a destination folder and open it directly in your file manager.
- Light, dark or system appearance, with persistent preferences.

Bing provides up to eight daily images per region. Windows Spotlight optionally
adds up to four images from its current selection. Spotlight is not a fixed daily
archive: refreshing can return different images. It uses an unofficial Microsoft
endpoint and availability can change. It works independently of whether Spotlight
is enabled on the local computer. If one source fails, images already loaded from
the other remain available and the app offers a retry.

After switching regions or changing image sources, use
**Aktualisieren** (Refresh) to load the new selection. Changing the resolution or
destination refreshes file detection: the saved status refers to the selected
resolution in the current folder. HD and UHD use separate filenames to avoid
overwriting existing Full HD images.

A download error stops the batch. Successfully saved images remain deselected;
**Erneut versuchen** (Retry) downloads only the remaining selection. Downloads use
temporary files to protect existing images if an operation fails.

Right-click an image for **Show**, **Open Path**, **Ignore** and **Delete local**.
Show opens the large preview. Open Path reveals the saved file in Windows Explorer
or Finder; on Linux it opens the containing folder. File actions refer to the
currently selected download resolution and are available for saved images.
Ignore keeps the image visible but excludes it from bulk selection and downloads.
The choice is saved across refreshes and restarts; **Stop ignoring** reverses it.
Delete local asks for confirmation before permanently removing that resolution's
file. Other downloaded resolutions are kept.

Enable **Refresh automatically on startup** in Settings to load the selected image sources when opening the app. This is off by default and never starts downloads.
The app remembers its normal window size and maximized state when closed. On the
next launch it restores them, fitting the normal size to the available screen work
area and display scaling. A minimized window is restored normally or maximized,
according to its last non-minimized state.

BingWallE focuses on collecting images. Automatic downloads and changing the
desktop wallpaper are not implemented.

## Image information and local tagging

Each gallery card and preview shows the requested download resolution. After a
successful download it shows the actual pixel dimensions returned by the image.
UHD dimensions vary by image; the app does not assume that every UHD file is 3840 × 2160.

New downloads embed the image title, description, copyright, source and keywords
using EXIF (including Windows Explorer fields) and standard XMP. Comments contain
the image description and source name. Publication date (when supplied), source
identity, source links, content region and model scores are
stored separately in application-specific XMP fields. Missing creation or capture
dates use the download time, with their fallback origin recorded in XMP; existing
dates remain unchanged. New filenames use ASCII letters, digits and underscores;
original titles stay in the metadata, and earlier filenames remain recognized.
Existing photographer/camera metadata is preserved; no author or rating is
invented, and JPEG image data is not recompressed. Existing files gain the new
metadata when selected and downloaded again.

The large preview separates the description, copyright, keywords derived from
the image description and additional local AI suggestions. Text keywords are available
before downloading and do not require a model. A small German/English vocabulary
recognizes subjects and broader categories; comma-separated locations are retained
from the caption. For example, the Mayotte octopus caption yields **Krake, Tier,
Meer, Mayotte, Indischer Ozean**. Underwater is not inferred from an octopus alone.
This is a limited vocabulary, not an unrestricted language understanding system.
German image regions use German category names; other regions use English names,
while locations retain their original spelling. Changing the interface language does not
change the download keywords.

Enable **Recognize image subjects locally as tags** in Settings for additional
suggestions during preview and download. The first recognition fetches the 14 MB
[MobileNetV2 model from the ONNX Model Zoo](https://github.com/onnx/models/tree/main/validated/vision/classification/mobilenet).
Its SHA-256 checksum is verified before use. It is cached under the current user's
local application-data directory in `bingwalle/models`; subsequent recognition
runs offline. Images are never uploaded for recognition. ONNX Runtime telemetry
is explicitly disabled before native initialization, including its background uploader.

The model classifies the main subject into 1,000 ImageNet categories. Only results
with a score of at least 50% are considered. Known labels are mapped to categories;
unmapped model labels remain in English. Scores do not guarantee correctness.
Preview and full-resolution downloads can produce different suggestions because
the input images differ. Uncertain recognition still leaves text-derived keywords.
Disabling local recognition keeps text keywords and avoids loading the model.
A failed download or recognition preserves existing files and allows a retry.

Third-party license notices are included in `licenses/` in the repository and packages.

## Downloads and first start

Download the package for your platform from the
[latest GitHub release](https://github.com/gruetze-software/BingWallE/releases/latest),
or use the Snap Store as described under [Linux installation](#linux-installation).
**The .NET runtime is included; no separate runtime or SDK installation is required.**

| Platform | Package |
| --- | --- |
| Windows x64 | `BingWallE-1.4.1-win-x64.zip` |
| Linux x64 | `BingWallE-1.4.1-linux-x64.deb` or `.tar.gz` |
| macOS Apple Silicon | `BingWallE-1.4.1-macos-arm64.dmg` or `.zip` |
| macOS Intel | `BingWallE-1.4.1-macos-x64.dmg` or `.zip` |

On Windows, extract the ZIP and run `bingwalle.exe`. Windows packages are not
signed with a code-signing certificate.

All four release targets are built and tested by GitHub Actions. The graphical
application has not been manually tested on every target device.
`SHA256SUMS.txt` in the release contains checksums for all GitHub packages.

### Linux installation

Choose the installation method that fits your distribution. Published Linux
packages target **x86_64 / amd64** and require a graphical desktop session.

| Installation method | Where to use it | Updates |
| --- | --- | --- |
| [Snap Store](#snap-ubuntu-and-other-distributions-with-snap-support) | Ubuntu and other distributions with snapd enabled, including Fedora, Arch Linux and openSUSE | Managed by Snap |
| [DEB package](#deb-debian-ubuntu-and-linux-mint) | Debian, Ubuntu and Ubuntu-based Linux Mint | Install a newer DEB manually |
| [TAR.GZ archive](#targz-other-linux-distributions) | Other compatible Linux distributions, with native dependencies installed | Extract a newer archive manually |

#### Snap: Ubuntu and other distributions with Snap support

<a href="https://snapcraft.io/bingwalle">
  <img src="https://snapcraft.io/static/images/badges/en/snap-store-black.svg"
       alt="Get it from the Snap Store" width="200">
</a>

On Ubuntu or another distribution with snapd enabled, install BingWallE from the
[Snap Store](https://snapcraft.io/bingwalle), or run:

```bash
sudo snap install bingwalle
snap run bingwalle
```

For Fedora, Arch Linux, openSUSE and other distributions, first follow the
[official snapd setup instructions for your distribution](https://snapcraft.io/docs/tutorials/install-the-daemon/).
Then use the same installation command above. These are installation options,
not a claim that BingWallE has been manually tested on every distribution.

Snap manages application updates. To check for an update manually, close BingWallE
and run `sudo snap refresh bingwalle`. The stable Store channel may contain a
different version from the latest GitHub release; inspect it with `snap info bingwalle`.
You do not need Snapcraft, the packaging tool, to install or run the application.

#### DEB: Debian, Ubuntu and Linux Mint

Download the Linux x64 **DEB** from the
[latest GitHub release](https://github.com/gruetze-software/BingWallE/releases/latest).
Open a terminal in its download folder and install the exact filename:

```bash
sudo apt install ./BingWallE-1.4.1-linux-x64.deb
```

Start **BingWallE** from the application menu or run `bingwalle`. The package
installs the application icon and desktop entry. The .NET runtime is included;
APT resolves the native dependencies declared by the package. Installing a newer
DEB with the same command upgrades the application. GitHub downloads do not set
up an APT repository or automatic application updates.

**Linux Mint:** the DEB is the direct installation option and does not require
Snap. Mint disables Snap installation by default; enabling it is optional and is
covered in [Canonical's Mint instructions](https://snapcraft.io/docs/tutorials/install-the-daemon/linux-mint/).
Linux Mint 21.x and 22.x are the intended DEB test targets; LMDE and ARM devices
have not been validated. See the [Mint test guide](docs/LINUX-MINT.md) for test
steps and troubleshooting. Flathub publication is still pending; BingWallE is
not yet available there for installation through Mint's Software Manager.

If you also have the Snap installed, close it before testing the DEB and use
`/usr/bin/bingwalle` to select the DEB explicitly. The two editions can use
different settings folders; this is not an automatic settings migration.

#### TAR.GZ: other Linux distributions

If you prefer not to use Snap or a DEB package, download
`BingWallE-1.4.1-linux-x64.tar.gz` from the GitHub release and extract it into a
folder. Open a terminal in that folder and run:

```bash
chmod +x bingwalle
./bingwalle
```

The archive includes the .NET runtime, but your distribution must provide its
native system libraries and the Avalonia desktop dependencies. Unlike the DEB,
the archive does not automatically install dependencies, a menu entry or an icon.
There is no RPM or Arch package supplied by this project; use Snap where supported
or validate the archive on your system. To upgrade, close the app and extract the
new release into a separate folder.

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

The **.NET 10 SDK** is required to build BingWallE 1.4.1.
The repository's `global.json` accepts stable .NET 10 SDK feature bands.
Application, tests and recognition smoke checks target .NET 10. macOS builds
require macOS 14 or later.

Run the following commands from the repository root:

```sh
dotnet restore BingWallE.sln
dotnet build BingWallE.sln -c Release
dotnet run --project BingWallE -c Release
```

## Tests

Flathub preparation and the remaining publisher steps are documented in
[docs/FLATHUB.md](docs/FLATHUB.md). Flathub review and publication are separate
from this GitHub release; follow the current manual submission requirements.

```sh
dotnet test BingWallE.sln -c Release
```

The xUnit tests cover selection, resolution variants, preview cancellation, retries,
cancellation, error handling, ASCII filenames and legacy-file recognition,
Bing/Spotlight responses, metadata date fallbacks, preferences and folder changes.
Avalonia headless tests also verify centered button labels and the optional
Ko-fi coffee icon in German and English.
Network responses are simulated and files are created in isolated temporary folders.
Tests do not change personal preferences or images and do not replace manual UI testing.

## Project structure

| Area | Responsibility |
| --- | --- |
| `Models` | Preferences, image data and cross-platform filenames |
| `Services/BingWallpaperClient` | Fetch Bing archive entries and previews |
| `Services/SpotlightWallpaperClient` | Fetch optional regional Windows Spotlight images and metadata |
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

The upstream [Flatpak build and install entry point](docs/FLATPAK-BUILD.md) keeps
the framework-dependent Linux build and desktop-file installation in the source
repository. It does not submit or publish a Flatpak. Native Wayland selection, X11 fallback and the remaining manual desktop tests
are documented in [Wayland testing](docs/WAYLAND.md).

The scripts read the version from `BingWallE/BingWallE.csproj` and write their output
to `artifacts/`. The app displays the full three-part version, for example
**1.4.1**. Every package includes the .NET runtime.

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

Version tags use the full package version, for example `v1.4.1`. Pushing a tag
starts the release build. The tag must match `<Version>` in the project file, and
`docs/RELEASE-<version>.md` must exist. The workflow publishes the GitHub release
only after all four build variants succeed. All packages and `SHA256SUMS.txt`
are attached. See [release notes for 1.4.1](docs/RELEASE-1.4.1.md).

## Snap packaging and publishing

For installation and updates, see [Snap installation](#snap-ubuntu-and-other-distributions-with-snap-support).
GitHub releases do not automatically update the Snap Store channel.
The separate **Build and test Snap** workflow produces test packages
without needing a publisher account.
See [the Snap publishing guide](docs/SNAP-PUBLISHING.md) for account registration,
local testing, permissions and the first beta upload. Store listing copy is in
[SNAP-STORE-LISTING.md](docs/SNAP-STORE-LISTING.md).

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
