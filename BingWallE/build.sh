#!/bin/bash

set -e

echo "🔧 Starte Cross-Build für BingWallE (Windows + Linux)…"

# -----------------------------
# 1. Version automatisch ermitteln
# -----------------------------
if git describe --tags --dirty --always >/dev/null 2>&1; then
    VERSION=$(git describe --tags --dirty --always)
else
    VERSION="1.0.0"
fi

echo "📦 Version: $VERSION"

rm -rf dist publish-linux publish-win

mkdir -p dist

# -----------------------------
# 2. Windows-Build
# -----------------------------
echo "🪟 Baue Windows-Version…"

dotnet publish \
    -c Release \
    -r win-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o publish-win/

echo "✔ Windows-Build fertig: publish-win/bingwalle.exe"

# -----------------------------
# 3. Linux-Build
# -----------------------------
echo "🐧 Baue Linux-Version…"

dotnet publish \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o publish-linux/

echo "✔ Linux-Build fertig: publish-linux/bingwalle"

# -----------------------------
# 4. DEB-Struktur erzeugen
# -----------------------------
echo "📁 Erzeuge DEB-Struktur…"

PKG_DIR="dist/bingwalle_${VERSION}_amd64/package"

mkdir -p "${PKG_DIR}/DEBIAN"
mkdir -p "${PKG_DIR}/usr/local/bin"
mkdir -p "${PKG_DIR}/usr/share/applications"
mkdir -p "${PKG_DIR}/usr/share/icons/hicolor/256x256/apps"

# Binary kopieren
cp publish-linux/bingwalle "${PKG_DIR}/usr/local/bin/bingwalle"
chmod 755 "${PKG_DIR}/usr/local/bin/bingwalle"

# Icon
if [ -f "icon.png" ]; then
    cp icon.png "${PKG_DIR}/usr/share/icons/hicolor/256x256/apps/bingwalle.png"
fi

# Desktop-Datei
cat <<EOF > "${PKG_DIR}/usr/share/applications/bingwalle.desktop"
[Desktop Entry]
Name=BingWallE
Comment=Download and set Bing wallpapers
Exec=bingwalle
Icon=bingwalle
Terminal=false
Type=Application
Categories=Graphics;Utility;
EOF

# control-Datei
cat <<EOF > "${PKG_DIR}/DEBIAN/control"
Package: bingwalle
Version: ${VERSION}
Section: utils
Priority: optional
Architecture: amd64
Maintainer: Christoph <schaefer.christoph@posteo.de>
Description: BingWallE – Bing Wallpaper Downloader
 A small Avalonia application that downloads and sets Bing wallpapers on Linux Mint.
EOF

# -----------------------------
# 5. DEB bauen
# -----------------------------
echo "📦 Erzeuge DEB-Paket…"

dpkg-deb --build "${PKG_DIR}"

mv "${PKG_DIR}.deb" "dist/bingwalle_${VERSION}_amd64.deb"

echo "🎉 Fertig!"
echo "👉 Windows EXE: publish-win/bingwalle.exe"
echo "👉 Linux Binary: publish-linux/bingwalle"
echo "👉 Linux DEB: dist/bingwalle_${VERSION}_amd64.deb"
