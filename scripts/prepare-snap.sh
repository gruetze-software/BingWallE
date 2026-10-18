#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
PROJECT="$ROOT/BingWallE/BingWallE.csproj"
VERSION="$(dotnet msbuild "$PROJECT" -nologo -getProperty:Version)"
[[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || { echo "Invalid version: $VERSION" >&2; exit 1; }
PAYLOAD="$ROOT/artifacts/snap-payload"
# Only this dedicated generated directory may be replaced; never touch the project sources.
[[ "$PAYLOAD" == "$ROOT/artifacts/snap-payload" && ! -L "$PAYLOAD" ]] || { echo 'Unsafe payload path' >&2; exit 1; }
rm -rf -- "$PAYLOAD"
APP="$PAYLOAD/opt/bingwalle"
mkdir -p "$APP" "$PAYLOAD/bin" "$PAYLOAD/usr/share/applications" "$PAYLOAD/usr/share/metainfo"
dotnet publish "$PROJECT" -c Release -r linux-x64 --self-contained true \
  -p:PublishSingleFile=false -p:DebugType=None -o "$APP"
printf '%s\n' "$VERSION" > "$APP/VERSION"
cp LICENSE README.md "$APP/"
cp -R licenses "$APP/"
mkdir -p "$APP/branding"
cp branding/bingwalle.png "$APP/branding/"
install -m 755 snap/local/bingwalle-launch "$PAYLOAD/bin/bingwalle-launch"
install -m 644 snap/gui/bingwalle.desktop "$PAYLOAD/usr/share/applications/de.gruetze_software.BingWallE.desktop"
install -m 644 branding/de.gruetze_software.BingWallE.metainfo.xml "$PAYLOAD/usr/share/metainfo/"
echo "Prepared Snap payload for BingWallE $VERSION (amd64)."
