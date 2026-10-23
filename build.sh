#!/bin/bash

# Jellythek Build & Release Script
# Usage: ./build.sh [VERSION]   (Standard: 1.0.1)

set -e

VERSION="${1:-1.0.1}"
PROJECT_DIR="Jellyfin.Plugin.Jellythek"
OUTPUT_DIR="release"
DLL_NAME="Jellyfin.Plugin.Jellythek.dll"
ZIP_NAME="Jellyfin.Plugin.Jellythek-v${VERSION}.zip"
GITHUB_USER="cibkp89"

echo "🔨 Building Jellythek v${VERSION}..."

# Clean
echo "🧹 Cleaning..."
rm -rf "${OUTPUT_DIR}"
rm -rf "${PROJECT_DIR}/bin" "${PROJECT_DIR}/obj"
mkdir -p "${OUTPUT_DIR}"

# Build (Version wird auch in die DLL geschrieben)
echo "🏗️  Building project..."
cd "${PROJECT_DIR}"
dotnet build --configuration Release \
    -p:Version="${VERSION}" \
    -p:AssemblyVersion="${VERSION}.0" \
    -p:FileVersion="${VERSION}.0"

# Copy main DLL and dependencies
echo "📦 Packaging..."
cp "bin/Release/net9.0/${DLL_NAME}" "../${OUTPUT_DIR}/"

# Copy System.ServiceModel.Syndication.dll if it exists
if [ -f "bin/Release/net9.0/System.ServiceModel.Syndication.dll" ]; then
    echo "📚 Including System.ServiceModel.Syndication.dll"
    cp "bin/Release/net9.0/System.ServiceModel.Syndication.dll" "../${OUTPUT_DIR}/"
else
    echo "⚠️  WARNING: System.ServiceModel.Syndication.dll not found!"
fi

cd ..

# Create ZIP (DLLs direkt im Root, ohne Unterordner)
echo "🗜️  Creating ZIP archive..."
cd "${OUTPUT_DIR}"
zip -j "${ZIP_NAME}" *.dll
cd ..

# Create checksum (Jellyfin verwendet MD5!)
echo "🔐 Generating MD5 checksum..."
if command -v md5sum >/dev/null 2>&1; then
    ZIP_CHECKSUM=$(md5sum "${OUTPUT_DIR}/${ZIP_NAME}" | awk '{print $1}')
else
    # macOS
    ZIP_CHECKSUM=$(md5 -q "${OUTPUT_DIR}/${ZIP_NAME}")
fi

echo "${ZIP_CHECKSUM}" > "${OUTPUT_DIR}/${ZIP_NAME}.md5"

echo ""
echo "✅ Build complete!"
echo "📁 Output:"
echo "   - ${OUTPUT_DIR}/${ZIP_NAME}"
echo "🔐 MD5 (ZIP): ${ZIP_CHECKSUM}"
echo ""
echo "📋 Files in ZIP:"
unzip -l "${OUTPUT_DIR}/${ZIP_NAME}"
echo ""
echo "Next steps:"
echo "1. Create a GitHub Release (tag: v${VERSION})"
echo "2. Upload ${OUTPUT_DIR}/${ZIP_NAME} to the release"
echo "3. Update manifest.json with:"
echo "   - sourceUrl: https://github.com/${GITHUB_USER}/jellyfin-plugin-jellythek/releases/download/v${VERSION}/${ZIP_NAME}"
echo "   - checksum: ${ZIP_CHECKSUM}"
echo "4. Commit and push manifest.json to main branch"
echo ""
echo "⚠️  Das hochgeladene ZIP muss exakt dieses sein. Nach jedem Neubau ändert sich die MD5!"
