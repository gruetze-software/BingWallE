#!/usr/bin/env bash
set -euo pipefail
ARCH="${1:-arm64}"
case "$ARCH" in arm64|x64) ;; *) echo "Usage: $0 [arm64|x64]" >&2; exit 1 ;; esac
[[ "$(uname -s)" == Darwin ]] || { echo "This script requires macOS (codesign, hdiutil, ditto)." >&2; exit 1; }
ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
SIGNING_MODE="${MACOS_SIGNING_MODE:-adhoc}"
case "$SIGNING_MODE" in
  adhoc) SIGN_ARGS=(--force --sign -) ;;
  developer-id)
    for name in MACOS_SIGNING_IDENTITY MACOS_SIGNING_KEYCHAIN MACOS_NOTARY_KEY_PATH APPLE_API_KEY_ID APPLE_API_ISSUER_ID; do
      [[ -n "${!name:-}" ]] || { echo "Missing signing setting: $name" >&2; exit 1; }
    done
    SIGN_ARGS=(--force --sign "$MACOS_SIGNING_IDENTITY" --keychain "$MACOS_SIGNING_KEYCHAIN" --timestamp --options runtime)
    ;;
  *) echo "Unknown macOS signing mode: $SIGNING_MODE" >&2; exit 2 ;;
esac
PROJECT="$ROOT/BingWallE/BingWallE.csproj"
VERSION="$(dotnet msbuild "$PROJECT" -nologo -getProperty:Version)"
[[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || { echo "Invalid version: $VERSION" >&2; exit 1; }
SHORT_VERSION="$VERSION"
mkdir -p "$ROOT/artifacts"
STAGE="$(mktemp -d "$ROOT/artifacts/stage-macos.XXXXXX")"
trap 'rm -rf -- "$STAGE"' EXIT
APP="$STAGE/BingWallE.app"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
# Keep native libraries in the bundle, where codesign can sign them explicitly.
dotnet publish "$PROJECT" -c Release -r "osx-$ARCH" --self-contained true \
    -p:PublishSingleFile=false -p:DebugType=None -o "$APP/Contents/MacOS"
test -s "$APP/Contents/MacOS/libonnxruntime.dylib"
cp "$ROOT/BingWallE/Assets/bingwalle.icns" "$APP/Contents/Resources/"
cp "$ROOT/LICENSE" "$ROOT/README.md" "$APP/Contents/Resources/"
cp -R "$ROOT/licenses" "$APP/Contents/Resources/"
mkdir -p "$APP/Contents/Resources/branding"
cp "$ROOT/bingwalle_icon_apple.png" "$APP/Contents/Resources/branding/bingwalle.png"
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
<key>LSMinimumSystemVersion</key><string>14.0</string>
<key>NSPrincipalClass</key><string>NSApplication</string>
<key>NSHumanReadableCopyright</key><string>© 2026 Grütze-Software</string>
</dict></plist>
PLIST
chmod 755 "$APP/Contents/MacOS/bingwalle"
while IFS= read -r -d '' binary; do
  # The main executable is signed with its enclosing bundle, after dependencies.
  [[ "$binary" != "$APP/Contents/MacOS/bingwalle" ]] || continue
  # Apple's code directory also treats .NET DLLs in Contents/MacOS as code.
  codesign "${SIGN_ARGS[@]}" "$binary"
done < <(find "$APP/Contents/MacOS" -type f -print0)
if [[ "$SIGNING_MODE" == developer-id ]]; then
  codesign "${SIGN_ARGS[@]}" --entitlements "$ROOT/branding/macos-entitlements.plist" "$APP"
else
  codesign "${SIGN_ARGS[@]}" "$APP"
fi
codesign --verify --deep --strict "$APP"
plutil -lint "$APP/Contents/Info.plist"
if [[ ( "$ARCH" == arm64 && "$(uname -m)" == arm64 ) || ( "$ARCH" == x64 && "$(uname -m)" == x86_64 ) ]]; then
  test "$("$APP/Contents/MacOS/bingwalle" --version)" = "BingWallE $VERSION"
fi
if [[ "$SIGNING_MODE" == developer-id ]]; then
  ditto -c -k --sequesterRsrc --keepParent "$APP" "$STAGE/notarization.zip"
  bash "$ROOT/scripts/notarize-macos.sh" "$STAGE/notarization.zip" "$APP"
  spctl --assess --type execute "$APP"
fi
ditto -c -k --sequesterRsrc --keepParent "$APP" "$ROOT/artifacts/BingWallE-$VERSION-macos-$ARCH.zip"
DMG_ROOT="$STAGE/dmg"
mkdir -p "$DMG_ROOT"
ditto "$APP" "$DMG_ROOT/BingWallE.app"
ln -s /Applications "$DMG_ROOT/Applications"
hdiutil create -volname "BingWallE $SHORT_VERSION" -srcfolder "$DMG_ROOT" -ov -format UDZO "$ROOT/artifacts/BingWallE-$VERSION-macos-$ARCH.dmg"
if [[ "$SIGNING_MODE" == developer-id ]]; then
  DMG="$ROOT/artifacts/BingWallE-$VERSION-macos-$ARCH.dmg"
  codesign --force --sign "$MACOS_SIGNING_IDENTITY" --keychain "$MACOS_SIGNING_KEYCHAIN" --timestamp "$DMG"
  bash "$ROOT/scripts/notarize-macos.sh" "$DMG" "$DMG"
  spctl --assess --type open --context context:primary-signature "$DMG"
fi
