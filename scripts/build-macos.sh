#!/usr/bin/env bash
set -euo pipefail
ARCH="${1:-arm64}"
case "$ARCH" in arm64|x64) ;; *) echo "Usage: $0 [arm64|x64]" >&2; exit 1 ;; esac
[[ "$(uname -s)" == Darwin ]] || { echo "This script requires macOS (codesign, hdiutil, ditto)." >&2; exit 1; }
ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
PROJECT="$ROOT/BingWallE/BingWallE.csproj"
VERSION="$(dotnet msbuild "$PROJECT" -nologo -getProperty:Version)"
[[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || { echo "Invalid version: $VERSION" >&2; exit 1; }
SHORT_VERSION="${VERSION%.*}"
mkdir -p "$ROOT/artifacts"
STAGE="$(mktemp -d "$ROOT/artifacts/stage-macos.XXXXXX")"
trap 'rm -rf -- "$STAGE"' EXIT
APP="$STAGE/BingWallE.app"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
# Keep native libraries in the bundle, where codesign can sign them explicitly.
dotnet publish "$PROJECT" -c Release -r "osx-$ARCH" --self-contained true \
    -p:PublishSingleFile=false -p:DebugType=None -o "$APP/Contents/MacOS"
cp "$ROOT/BingWallE/Assets/bingwalle.icns" "$APP/Contents/Resources/"
cp "$ROOT/LICENSE" "$ROOT/README.md" "$APP/Contents/Resources/"
cat > "$APP/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
<key>CFBundleName</key><string>BingWallE</string>
<key>CFBundleDisplayName</key><string>BingWallE</string>
<key>CFBundleExecutable</key><string>bingwalle</string>
<key>CFBundleIdentifier</key><string>software.gruetze.bingwalle</string>
<key>CFBundlePackageType</key><string>APPL</string>
<key>CFBundleShortVersionString</key><string>$VERSION</string>
<key>CFBundleGetInfoString</key><string>BingWallE $VERSION · Grütze-Software</string>
<key>LSApplicationCategoryType</key><string>public.app-category.photography</string>
<key>CFBundleDevelopmentRegion</key><string>de</string>
<key>CFBundleVersion</key><string>$VERSION</string>
<key>CFBundleIconFile</key><string>bingwalle.icns</string>
<key>NSHighResolutionCapable</key><true/>
<key>LSMinimumSystemVersion</key><string>12.0</string>
<key>NSPrincipalClass</key><string>NSApplication</string>
<key>NSHumanReadableCopyright</key><string>© 2026 Christoph Schaefer · Grütze-Software</string>
</dict></plist>
PLIST
chmod 755 "$APP/Contents/MacOS/bingwalle"
find "$APP/Contents/MacOS" -type f -name '*.dylib' -exec codesign --force --sign - {} \;
codesign --force --deep --sign - "$APP"
codesign --verify --deep --strict "$APP"
plutil -lint "$APP/Contents/Info.plist"
ditto -c -k --sequesterRsrc --keepParent "$APP" "$ROOT/artifacts/BingWallE-$VERSION-macos-$ARCH.zip"
DMG_ROOT="$STAGE/dmg"
mkdir -p "$DMG_ROOT"
ditto "$APP" "$DMG_ROOT/BingWallE.app"
ln -s /Applications "$DMG_ROOT/Applications"
hdiutil create -volname "BingWallE $SHORT_VERSION" -srcfolder "$DMG_ROOT" -ov -format UDZO "$ROOT/artifacts/BingWallE-$VERSION-macos-$ARCH.dmg"
