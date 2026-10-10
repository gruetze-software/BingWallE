# BingWallE 1.1.0

## What's new

- A dedicated **Settings** dialog keeps the gallery tidy and groups application language, download region, resolution and theme.
- Full **German and English interfaces**, including the About dialog, dates, progress and error messages. Language changes apply immediately and are saved.
- The **download region is independent of the interface language**. Choose Germany for German Bing titles and filenames, or the USA / United Kingdom for English content, then select Refresh.
- Smoother app-logo rendering in the header and About dialog.
- Completed Windows file properties, macOS bundle information and Linux application/package metadata. Product versions no longer include a Git commit suffix.
- English README with macOS installation and first-launch troubleshooting.
- Version-tagged releases now validate the project version and release notes before publication.

## Downloads

All packages include the .NET runtime.

| Platform | Package |
| --- | --- |
| Windows x64 | `BingWallE-1.1.0-win-x64.zip` — extract and run `bingwalle.exe` |
| Linux x64 | `BingWallE-1.1.0-linux-x64.deb` or `.tar.gz` |
| macOS Apple Silicon | `BingWallE-1.1.0-macos-arm64.dmg` or `.zip` |
| macOS Intel | `BingWallE-1.1.0-macos-x64.dmg` or `.zip` |

On macOS, open the DMG and drag BingWallE to Applications. Bundles are ad-hoc signed
and verified but are not Apple-notarized. See the
[macOS first-launch instructions](https://github.com/gruetze-software/BingWallE/blob/v1.1.0/README.md#macos-installation-and-troubleshooting)
if macOS blocks the app. Windows packages are not code-signed. Linux requires a
graphical desktop and native system libraries.

`SHA256SUMS.txt` contains checksums for all seven packages. All four build targets
run the automated test suite; this does not replace manual testing on every device.
Existing preferences remain compatible. The app displays the short version **1.1**;
package and product versions are **1.1.0**.
