#!/usr/bin/env bash
# Generate SHA256 checksums for all binaries
# Usage: generate-checksums.sh <artifacts-directory>
#
# Generates:
# - SHA256SUMS: Combined hash file with all binaries (existing feature)
# - <binary>.sha256: Individual hash file per binary (new feature)
#
# Format: <hash>  <filename> (two spaces, GNU/Linux standard)

set -euo pipefail

ARTIFACTS_DIR="${1:-.}"

echo "========================================="
echo "Generating SHA256 checksums"
echo "Artifacts directory: $ARTIFACTS_DIR"
echo "========================================="

# Input validation: Check if directory exists
if [ ! -d "$ARTIFACTS_DIR" ]; then
  echo "❌ ERROR: Directory not found: $ARTIFACTS_DIR"
  exit 1
fi

# Input validation: Check if directory is readable
if [ ! -r "$ARTIFACTS_DIR" ]; then
  echo "❌ ERROR: Directory not readable: $ARTIFACTS_DIR"
  exit 1
fi

cd "$ARTIFACTS_DIR"

# Find all binary files
BINARIES=$(find . -type f \( -name "wtm-*-windows-*.exe" -o -name "wtm-*-linux-*" -o -name "wtm-*-macos-*" \) ! -name "*.sha256" ! -name "SHA256SUMS*")

# Input validation: Check if any binaries were found
if [ -z "$BINARIES" ]; then
  echo "❌ ERROR: No binary files found in $ARTIFACTS_DIR"
  echo "Expected file patterns: wtm-*-windows-*.exe, wtm-*-linux-*, wtm-*-macos-*"
  exit 1
fi

echo "Found binaries:"
echo "$BINARIES"
echo ""

# Generate combined SHA256SUMS file
: > SHA256SUMS  # Create empty file

# Track success/failure for individual .sha256 file generation
FAILED_FILES=()

# Generate checksums for each binary
while IFS= read -r binary; do
  # Input validation: Verify file exists and is readable
  if [ ! -f "$binary" ]; then
    echo "⚠️  WARNING: File not found: $binary (skipping)"
    FAILED_FILES+=("$binary")
    continue
  fi

  if [ ! -r "$binary" ]; then
    echo "❌ ERROR: File not readable: $binary"
    FAILED_FILES+=("$binary")
    continue
  fi

  echo "Computing SHA256 for $binary..."

  # Compute hash for SHA256SUMS (combined file)
  sha256sum "$binary" >> SHA256SUMS

  # Generate individual .sha256 file
  # Format: <hash>  <filename> (two spaces)
  INDIVIDUAL_HASH_FILE="${binary}.sha256"
  sha256sum "$binary" > "$INDIVIDUAL_HASH_FILE"

  if [ -f "$INDIVIDUAL_HASH_FILE" ]; then
    echo "  ✅ Generated: $INDIVIDUAL_HASH_FILE"
  else
    echo "  ❌ ERROR: Failed to generate $INDIVIDUAL_HASH_FILE"
    FAILED_FILES+=("$binary")
  fi
done <<< "$BINARIES"

echo ""

# Check if any files failed
if [ ${#FAILED_FILES[@]} -gt 0 ]; then
  echo "❌ ERROR: Failed to generate hash files for the following binaries:"
  for file in "${FAILED_FILES[@]}"; do
    echo "  - $file"
  done
  exit 1
fi

echo "✅ Checksums generated successfully"
echo ""
echo "========================================="
echo "SHA256SUMS content:"
echo "========================================="
cat SHA256SUMS
echo "========================================="
echo ""
echo "Individual .sha256 files:"
echo "========================================="
ls -lh -- *.sha256
echo "========================================="
