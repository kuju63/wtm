#!/usr/bin/env python3
"""
Update version manifest for documentation versioning.
Maintains a JSON manifest of all published documentation versions.
"""

import json
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


def load_manifest(manifest_path: Path) -> dict[str, Any]:
    """Load existing manifest or return empty structure."""
    if manifest_path.exists():
        with open(manifest_path, 'r') as f:
            return json.load(f)
    return {"versions": []}


def validate_manifest(manifest: dict[str, Any]) -> bool:
    """Validate manifest structure and content."""
    if "versions" not in manifest:
        print("ERROR: Manifest missing 'versions' key", file=sys.stderr)
        return False

    # Ensure exactly one version is marked as latest
    latest_count = sum(1 for v in manifest["versions"] if v.get("isLatest", False))
    if latest_count != 1:
        print(f"ERROR: Manifest must have exactly one 'isLatest' version (found {latest_count})", file=sys.stderr)
        return False
    
    return True


def update_manifest(manifest_path: Path, new_version: str) -> None:
    """Add new version to manifest and update isLatest flags."""
    manifest = load_manifest(manifest_path)
    
    # Mark all existing versions as not latest
    for version in manifest["versions"]:
        version["isLatest"] = False
    
    now = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S") + "Z"
    
    # Check if version already exists (idempotent update)
    existing_versions = {v["version"] for v in manifest["versions"]}
    if new_version in existing_versions:
        # Update existing entry
        for version in manifest["versions"]:
            if version["version"] == new_version:
                version["isLatest"] = True
                version["publishedDate"] = now
                break
    else:
        # Add new version entry
        new_entry = {
            "version": new_version,
            "isLatest": True,
            "publishedDate": now
        }
        manifest["versions"].append(new_entry)
    
    # Sort by version (descending)
    manifest["versions"].sort(key=lambda v: v["version"], reverse=True)
    
    # Validate before writing
    if not validate_manifest(manifest):
        sys.exit(1)
    
    # Write updated manifest
    with open(manifest_path, 'w') as f:
        json.dump(manifest, f, indent=2)
        f.write('\n')  # Add trailing newline
    
    print(f"✓ Updated manifest with version {new_version}")


def main():
    if len(sys.argv) != 3:
        print("Usage: update-version-manifest.py <manifest_path> <version>", file=sys.stderr)
        sys.exit(1)
    
    manifest_path = Path(sys.argv[1])
    new_version = sys.argv[2]
    
    update_manifest(manifest_path, new_version)


if __name__ == "__main__":
    main()
