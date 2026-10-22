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
