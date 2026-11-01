# Upstream Flatpak build and installation

`scripts/install-flatpak.sh` owns the application's build and install steps for
a framework-dependent Linux package. It is upstream source code, not a Flathub
manifest or submission text. The script and this document are AI-assisted.

## Inputs

- A Linux build environment with the .NET 10 SDK, Bash and standard GNU tools.
- Pre-downloaded NuGet packages in `nuget-sources/` at the repository root, or a
  directory supplied with `--nuget-source`. The existing NuGet cache can also be
  used; an empty local feed is sufficient when every required package is cached.
- An installation prefix from `FLATPAK_DEST` (normally `/app`), or `--prefix`.
- A runtime supplied by the surrounding package. The script does not bundle .NET,
  install SDK extensions or grant any sandbox permissions.

The script selects `linux-x64` on x86-64 build machines and `linux-arm64` on
ARM64 build machines. Publishing with an explicit runtime identifier includes
only the matching native libraries, while keeping the app framework-dependent.
Without this selection, a portable publish includes native assets for other
operating systems and architectures, including Windows debug symbols. Unsupported
architectures fail before publishing instead of producing a portable package.
ONNX Runtime also adds Windows DLLs as content through its NuGet build props;
the project excludes those content entries for Linux and macOS targets. Managed
assemblies and the selected platform's native inference library remain included.

## Package size regression check

A local comparison for 1.6.0 measured 797.66 MiB for the previous portable app
publish versus 54.04 MiB for the Linux x64 app publish after selecting the RID
and excluding ONNX's Windows content (about 93% smaller). These are uncompressed
app files, not Flathub download sizes; the .NET runtime installed by the packaging
and the shared Freedesktop runtime are additional. The complete upstream staging
prefix, including desktop metadata and licenses, measured 54.08 MiB.

CI checks that the staged app starts, includes the Linux Skia, HarfBuzz and ONNX
libraries, and contains neither a portable `runtimes/` tree nor Windows native
ONNX DLLs, debug symbols or Apple/Android native packages.

## Running the installer

Run `bash scripts/install-flatpak.sh --help` for the argument syntax. Relative
NuGet paths are resolved against the caller's working directory; the build itself
always uses the repository containing the script.

For a local staging check outside Flatpak, supply a writable absolute prefix and
the local package feed. For example, after preparing those packages:

```bash
bash scripts/install-flatpak.sh --prefix "$PWD/artifacts/flatpak-stage" --nuget-source "$PWD/nuget-sources"
artifacts/flatpak-stage/bin/bingwalle --version
```

The staging check requires an available .NET runtime. It verifies installation;
it does not establish sandbox or graphical desktop compatibility.

## Installed files

| Location relative to the prefix | Contents |
| --- | --- |
| `bin/` | App host, managed assemblies and native dependencies from `dotnet publish` |
| `share/applications/` | Desktop launcher |
| `share/icons/hicolor/256x256/apps/` | Maintainer-drawn app icon |
| `share/metainfo/` | AppStream metadata |
| `share/licenses/de.gruetze_software.BingWallE/` | MIT license and third-party notices |

Publish uses a temporary directory and restores from the supplied local feed;
NuGet vulnerability-audit network requests are disabled for the offline build.
The script fails on a missing feed, build failure or missing executable before
copying anything into the prefix. Existing installed application files are
overwritten, so use a fresh staging prefix when validating a package.

The surrounding human-maintained packaging still selects the runtime, SDK,
source revision, downloaded dependencies and sandbox permissions. After adopting
the upstream entry point, rebuild the real Flatpak and repeat the folder portal,
download, external-link, image-opening and restart tests on Mint or Ubuntu.
Do not treat the CI staging check as a replacement for these tests.

See the [official .NET packaging guide](https://docs.flatpak.org/en/latest/dotnet.html)
and [Flathub requirements](https://docs.flathub.org/docs/for-app-authors/requirements).
