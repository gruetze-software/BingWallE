#!/usr/bin/env bash
set -euo pipefail
ARCH="${1:-x64}"
case "$ARCH" in x64) DEB_ARCH=amd64 ;; arm64) DEB_ARCH=arm64 ;; *) echo "Usage: $0 [x64|arm64]" >&2; exit 1 ;; esac
ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
PROJECT="$ROOT/BingWallE/BingWallE.csproj"
VERSION="$(dotnet msbuild "$PROJECT" -nologo -getProperty:Version)"
[[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || { echo "Invalid version: $VERSION" >&2; exit 1; }
command -v dpkg-deb >/dev/null
mkdir -p "$ROOT/artifacts"
STAGE="$(mktemp -d "$ROOT/artifacts/stage-linux.XXXXXX")"
trap 'rm -rf -- "$STAGE"' EXIT
PUBLISH="$STAGE/publish"
dotnet publish "$PROJECT" -c Release -r "linux-$ARCH" --self-contained true \
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -o "$PUBLISH"
cp "$ROOT/LICENSE" "$ROOT/README.md" "$PUBLISH/"
cp -R "$ROOT/licenses" "$PUBLISH/"
mkdir -p "$PUBLISH/branding"
cp "$ROOT/branding/bingwalle.png" "$PUBLISH/branding/"
cp "$ROOT/BingWallE/Assets/bingwalle.png" "$PUBLISH/de.gruetze_software.BingWallE.png"
cp "$ROOT/branding/de.gruetze_software.BingWallE.metainfo.xml" "$PUBLISH/"
cp "$ROOT/branding/de.gruetze_software.BingWallE.desktop" "$PUBLISH/"
chmod 755 "$PUBLISH/bingwalle"
tar -czf "$ROOT/artifacts/BingWallE-$VERSION-linux-$ARCH.tar.gz" -C "$PUBLISH" .
PKG="$STAGE/package"
mkdir -p "$PKG/DEBIAN" "$PKG/usr/bin" "$PKG/usr/share/applications" "$PKG/usr/share/icons/hicolor/256x256/apps" "$PKG/usr/share/doc/bingwalle"
install -m 755 "$PUBLISH/bingwalle" "$PKG/usr/bin/bingwalle"
install -m 644 "$ROOT/BingWallE/Assets/bingwalle.png" "$PKG/usr/share/icons/hicolor/256x256/apps/de.gruetze_software.BingWallE.png"
install -m 644 "$ROOT/LICENSE" "$PKG/usr/share/doc/bingwalle/copyright"
cp -R "$ROOT/licenses" "$PKG/usr/share/doc/bingwalle/"
install -m 644 "$ROOT/branding/de.gruetze_software.BingWallE.desktop" "$PKG/usr/share/applications/de.gruetze_software.BingWallE.desktop"
install -Dm 644 "$ROOT/branding/de.gruetze_software.BingWallE.metainfo.xml" "$PKG/usr/share/metainfo/de.gruetze_software.BingWallE.metainfo.xml"
cat > "$PKG/DEBIAN/control" <<CONTROL
Package: bingwalle
Version: $VERSION
Section: graphics
Priority: optional
Architecture: $DEB_ARCH
Maintainer: Christoph Schaefer <schaefer.christoph@posteo.de>
Depends: libwayland-client0, libwayland-cursor0, libwayland-egl1, libxkbcommon0, libegl1, libgles2, libc6, libgcc-s1, libstdc++6, zlib1g, libfontconfig1, libice6, libsm6, libx11-6, libicu74 | libicu70 | libicu72 | libicu76, libssl3t64 | libssl3
Homepage: https://github.com/gruetze-software/BingWallE
Description: Bing wallpaper collector
 Browse and collect recent Bing wallpapers with previews and downloads up to UHD.
CONTROL
chmod 644 "$PKG/DEBIAN/control" "$PKG/usr/share/applications/de.gruetze_software.BingWallE.desktop"
dpkg-deb --root-owner-group --build "$PKG" "$ROOT/artifacts/BingWallE-${VERSION}-linux-${ARCH}.deb"
