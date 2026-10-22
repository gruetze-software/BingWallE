#!/usr/bin/env bash
# Upstream build/install entry point. The caller supplies the .NET runtime and
# pre-downloaded NuGet packages; this script does not change sandbox permissions.
set -euo pipefail

ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
PREFIX="${FLATPAK_DEST:-/app}"
NUGET_SOURCE="$ROOT/nuget-sources"
APP_ID=de.gruetze_software.BingWallE

usage() {
  echo "Usage: bash scripts/install-flatpak.sh [--prefix ABSOLUTE_PATH] [--nuget-source DIRECTORY]"
  echo 'Defaults: FLATPAK_DEST or /app; nuget-sources/ in the source repository.'
}

while (($#)); do
  case "$1" in
    --prefix|--nuget-source)
      [[ $# -ge 2 && -n "$2" && "$2" != --* ]] || { usage >&2; exit 2; }
      if [[ "$1" == --prefix ]]; then PREFIX="$2"; else NUGET_SOURCE="$2"; fi
      shift 2
      ;;
    --help|-h) usage; exit 0 ;;
    *) usage >&2; exit 2 ;;
  esac
done

[[ "$(uname -s)" == Linux ]] || { echo 'Run this script in the Linux build environment.' >&2; exit 1; }
[[ "$PREFIX" == /* && "$PREFIX" != / ]] || { echo 'The install prefix must be an absolute path other than /.' >&2; exit 2; }
[[ -d "$NUGET_SOURCE" ]] || { echo "Local NuGet source not found: $NUGET_SOURCE" >&2; exit 1; }
NUGET_SOURCE="$(cd -- "$NUGET_SOURCE" && pwd)"
command -v dotnet >/dev/null

WORK="$(mktemp -d)"
trap 'rm -rf -- "$WORK"' EXIT
cd "$ROOT"

# Restore exclusively from the caller's local feed (or the existing NuGet cache).
# Audit requests are disabled because Flatpak builds run without network access.
dotnet publish BingWallE/BingWallE.csproj -c Release --self-contained false \
  --source "$NUGET_SOURCE" -p:NuGetAudit=false -p:UseAppHost=true \
  -p:PublishSingleFile=false -p:DebugType=None -o "$WORK/publish"
test -x "$WORK/publish/bingwalle"

install -d "$PREFIX/bin"
cp -a "$WORK/publish/." "$PREFIX/bin/"
install -Dm644 "branding/$APP_ID.desktop" "$PREFIX/share/applications/$APP_ID.desktop"
install -Dm644 BingWallE/Assets/bingwalle.png "$PREFIX/share/icons/hicolor/256x256/apps/$APP_ID.png"
install -Dm644 "branding/$APP_ID.metainfo.xml" "$PREFIX/share/metainfo/$APP_ID.metainfo.xml"
install -Dm644 LICENSE "$PREFIX/share/licenses/$APP_ID/LICENSE"
install -d "$PREFIX/share/licenses/$APP_ID/third-party"
cp -a licenses/. "$PREFIX/share/licenses/$APP_ID/third-party/"

echo "Installed BingWallE and desktop metadata under $PREFIX"
