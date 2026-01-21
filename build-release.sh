#!/bin/bash

# Extract version from .csproj file
VERSION=$(grep -oP '<Version>\K[^<]+' Jellyfin.Plugin.PosterDB.csproj)
if [ -z "$VERSION" ]; then
    VERSION="1.0.0.0"
fi

PLUGIN_NAME="Jellyfin.Plugin.PosterDB"

echo "🔨 Building PosterDB Plugin v${VERSION}..."

dotnet clean -c Release
dotnet publish -c Release -o ./publish

mkdir -p ./release

cd publish
zip -r "../release/${PLUGIN_NAME}_${VERSION}.zip" . -x "*.pdb" -x "*.deps.json" -x "*runtimeconfig.json"
cd ..

CHECKSUM=$(md5sum "./release/${PLUGIN_NAME}_${VERSION}.zip" | awk '{print $1}' | tr '[:lower:]' '[:upper:]')

echo ""
echo "Build complete!"
echo "Package: ./release/${PLUGIN_NAME}_${VERSION}.zip"
echo "MD5 Checksum: ${CHECKSUM}"
echo ""
echo "Update manifest.json with this checksum:"
echo "   \"checksum\": \"${CHECKSUM}\""
