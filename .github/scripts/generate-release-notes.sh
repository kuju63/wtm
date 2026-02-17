#!/usr/bin/env bash
# Generate release notes from Conventional Commits
# Usage: generate-release-notes.sh <previous-version> <new-version>

set -euo pipefail

PREVIOUS_VERSION="${1:-v0.0.0}"
NEW_VERSION="${2:-v0.1.0}"

echo "========================================="
echo "Generating release notes"
echo "Previous version: $PREVIOUS_VERSION"
echo "New version: $NEW_VERSION"
echo "========================================="

# Get commits since previous version
if [ "$PREVIOUS_VERSION" == "v0.0.0" ]; then
  COMMITS=$(git log --pretty=format:"%s|||%b" --reverse)
else
  COMMITS=$(git log "${PREVIOUS_VERSION}..HEAD" --pretty=format:"%s|||%b" --reverse)
fi

if [ -z "$COMMITS" ]; then
  echo "No commits to process"
  exit 0
fi

# Initialize categories
BREAKING_CHANGES=""
FEATURES=""
FIXES=""
OTHER=""

# Parse commits
while IFS= read -r line; do
  SUBJECT=$(echo "$line" | cut -d'|' -f1)
  BODY=$(echo "$line" | cut -d'|' -f4)

  # Check for breaking changes
  if echo "$BODY" | grep -qi "BREAKING CHANGE:"; then
    BREAKING_TEXT=$(echo "$BODY" | grep -i "BREAKING CHANGE:" | sed 's/BREAKING CHANGE: //i')
    BREAKING_CHANGES="${BREAKING_CHANGES}- ${BREAKING_TEXT}\n"
  elif echo "$SUBJECT" | grep -qiE "^feat(\(|:)"; then
    # Feature
    FEATURE_TEXT=$(echo "$SUBJECT" | sed -E 's/^feat(\([^)]*\))?: //i')
    FEATURES="${FEATURES}- ${FEATURE_TEXT}\n"
  elif echo "$SUBJECT" | grep -qiE "^fix(\(|:)"; then
    # Fix
    FIX_TEXT=$(echo "$SUBJECT" | sed -E 's/^fix(\([^)]*\))?: //i')
    FIXES="${FIXES}- ${FIX_TEXT}\n"
  else
    # Other (docs, style, refactor, test, chore, ci)
    OTHER="${OTHER}- ${SUBJECT}\n"
  fi
done <<< "$COMMITS"

# Build release notes
RELEASE_NOTES="# Release ${NEW_VERSION}\n\n"

if [ -n "$BREAKING_CHANGES" ]; then
  RELEASE_NOTES="${RELEASE_NOTES}## ⚠️ BREAKING CHANGES\n\n${BREAKING_CHANGES}\n"
fi

if [ -n "$FEATURES" ]; then
  RELEASE_NOTES="${RELEASE_NOTES}## ✨ Features\n\n${FEATURES}\n"
fi

if [ -n "$FIXES" ]; then
  RELEASE_NOTES="${RELEASE_NOTES}## 🐛 Bug Fixes\n\n${FIXES}\n"
fi

if [ -n "$OTHER" ]; then
  RELEASE_NOTES="${RELEASE_NOTES}## 📝 Other Changes\n\n${OTHER}\n"
fi

# Add installation instructions
RELEASE_NOTES="${RELEASE_NOTES}## 📦 Installation\n\n"
RELEASE_NOTES="${RELEASE_NOTES}Download the binary for your platform:\n\n"
RELEASE_NOTES="${RELEASE_NOTES}- **Windows x64**: \`wt-${NEW_VERSION}-windows-x64.exe\`\n"
RELEASE_NOTES="${RELEASE_NOTES}- **Linux x64**: \`wt-${NEW_VERSION}-linux-x64\`\n"
RELEASE_NOTES="${RELEASE_NOTES}- **Linux ARM**: \`wt-${NEW_VERSION}-linux-arm\` (optional)\n"
RELEASE_NOTES="${RELEASE_NOTES}- **macOS ARM64**: \`wt-${NEW_VERSION}-macos-arm64\`\n\n"

# Add checksums section
RELEASE_NOTES="${RELEASE_NOTES}## 📋 Checksums\n\n"

# Read and display SHA256SUMS content if available
if [ -f "release-assets/SHA256SUMS" ]; then
  RELEASE_NOTES="${RELEASE_NOTES}\`\`\`text\n"
  while IFS= read -r line; do
    RELEASE_NOTES="${RELEASE_NOTES}${line}\n"
  done < release-assets/SHA256SUMS
  RELEASE_NOTES="${RELEASE_NOTES}\`\`\`\n\n"
fi

RELEASE_NOTES="${RELEASE_NOTES}See the [Release Verification Guide](https://kuju63.github.io/wt/latest/release-verification.html) for verification instructions.\n\n"

# Add security section
RELEASE_NOTES="${RELEASE_NOTES}## 🔒 Security\n\n"
RELEASE_NOTES="${RELEASE_NOTES}Software Bill of Materials (SBOM) is available as \`wt-${NEW_VERSION}-sbom.spdx.json\`\n\n"

# Add footer
RELEASE_NOTES="${RELEASE_NOTES}---\n\n"
RELEASE_NOTES="${RELEASE_NOTES}**Full Changelog**: https://github.com/kuju63/wt/compare/${PREVIOUS_VERSION}...${NEW_VERSION}\n"

# Output to file
echo -e "$RELEASE_NOTES" > release-notes.md

echo ""
echo "Release notes generated successfully"
echo "Output file: release-notes.md"
echo ""
echo "========================================="
cat release-notes.md
echo "========================================="
