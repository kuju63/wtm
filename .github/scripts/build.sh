#!/usr/bin/env bash
# Universal build script for all platforms
# Usage: build.sh <version> <rid> <platform-name> <mandatory>
#
# Parameters:
#   version: Version number (e.g., v1.0.0)
#   rid: Runtime identifier (e.g., win-x64, linux-x64, osx-arm64)
#   platform-name: Display name (e.g., "Windows x64", "Linux ARM")
#   mandatory: true or false - whether build failure should block release

set -euo pipefail

VERSION="${1:-v0.1.0}"
RID="${2:-linux-x64}"
PLATFORM_NAME="${3:-Linux x64}"
MANDATORY="${4:-true}"

echo "========================================="
echo "Building $PLATFORM_NAME binary"
echo "Version: $VERSION"
echo "RID: $RID"
echo "Mandatory: $MANDATORY"
echo "========================================="

# Navigate to CLI project directory
cd wt.cli

# Clean previous builds
echo "Cleaning previous builds..."
dotnet clean -c Release

# Publish self-contained binary
echo "Publishing self-contained binary for $RID..."
if ! dotnet publish \
  -c Release \
  -r "$RID" \
  --self-contained \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:Version="${VERSION#v}" \
  -p:AssemblyVersion="${VERSION#v}" \
  -p:FileVersion="${VERSION#v}" \
  --output "./bin/Release/net10.0/$RID/publish"; then

  if [ "$MANDATORY" = "false" ]; then
    echo "⚠️ WARNING: Build failed for optional platform $PLATFORM_NAME"
    echo "Continuing without this binary..."
    exit 0
  else
    echo "❌ ERROR: Build failed for mandatory platform $PLATFORM_NAME"
    exit 1
  fi
fi

# Determine binary name based on RID
if [[ "$RID" == win-* ]]; then
  BINARY_NAME="wtm.exe"
else
  BINARY_NAME="wtm"
fi

BINARY_PATH="./bin/Release/net10.0/$RID/publish/$BINARY_NAME"

# Verify binary exists
if [ ! -f "$BINARY_PATH" ]; then
  if [ "$MANDATORY" = "false" ]; then
    echo "⚠️ WARNING: Binary not found at $BINARY_PATH for optional platform"
    exit 0
  else
    echo "❌ ERROR: Binary not found at $BINARY_PATH"
    exit 1
  fi
fi

# Make binary executable (Unix-like platforms only)
if [[ "$RID" != win-* ]]; then
  chmod +x "$BINARY_PATH"
fi

echo "✅ Build completed successfully"
echo "Binary location: $BINARY_PATH"
ls -lh "$BINARY_PATH"
