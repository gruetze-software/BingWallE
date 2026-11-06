# Release package contents

Official packages publish for a specific runtime identifier and use fresh staging
directories. They include the application, its runtime dependencies, required
desktop integration, README and licenses. Source artwork, build caches, test
outputs and the optional image-recognition model are not copied into packages.

`BingWallE.csproj` filters Release publish items before single-file bundling or
copying the output. This removes dependency debug files (`.pdb`, `.dbg`, `.debug`,
`.dwarf`, `.mdb`, `.dSYM`) and link-time files (`.lib`, `.exp`, `.a`). Merely setting
`DebugType=None` is insufficient for symbols supplied by NuGet packages.
Debug builds used in Visual Studio retain their symbols.

Linux and macOS targets also exclude Windows ONNX DLLs that the package's build
props otherwise copy as content. The selected platform's native inference
library remains included. Required managed .NET DLLs remain on all platforms.

## Verification

`scripts/check-release-files.py` checks a directory, ZIP or tar archive. It rejects
the debug/link-time files above, portable `runtimes/` trees, mobile platform assets
and known native binaries belonging to another operating system. For example:

```bash
python3 scripts/check-release-files.py linux artifacts/BingWallE-1.6.0-linux-x64.tar.gz
python3 scripts/check-release-files.py macos path/to/BingWallE.app
```

CI checks Windows and macOS ZIPs, the Linux tar archive and extracted DEB contents
before uploading release artifacts. The macOS ZIP and DMG use the same staged app
bundle. Snap CI checks the app payload before packing; Snapcraft's runtime and
desktop-extension contents are managed separately by Snapcraft.

The checker is a packaging guard, not a substitute for starting the app or testing
the graphics backend and local image recognition. Rebuild into a fresh directory:
`dotnet publish` does not remove stale files from a previously populated output.

See [FLATPAK-BUILD.md](FLATPAK-BUILD.md) for the framework-dependent Linux installer
and its size comparison. Publishing a GitHub release does not automatically
update the Snap Store or Flathub.

## Rebuilding an existing release tag

The workflow uses macOS 26 build runners (ARM64 and Intel); the application's declared minimum
macOS version remains 14. GitHub retired its macOS 14 runners on 2 November 2026.

If a tagged workflow can no longer run because its runner image was retired,
start the current workflow from `main` with `release_tag` set to the existing tag:

```bash
gh workflow run build.yml --ref main -f release_tag=v1.6.2
```

All build, desktop-test and release jobs check out the supplied tag. The version
must match the app's project version, and the release still requires all platform
jobs and packaging checks. This uses the current workflow without moving the tag
or changing the application source. Leave `release_tag` empty for a test-only run.
The publish command creates a new release; it does not replace an existing one.

## Intel macOS inference dependency

ONNX Runtime 1.30.0 no longer ships an Intel macOS native library. The project
selects 1.23.2 for `osx-x64` publishing and for builds without a RID on an Intel
macOS SDK. Both the managed binding and native library come from the same package
version. Other targets keep 1.30.0. Native Intel CI runs the real model and the
recognition shutdown check before packaging; the app retains its existing model
hash verification and telemetry opt-out.
