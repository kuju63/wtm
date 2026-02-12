#!/usr/bin/env python3
"""
Unit tests for update-version-manifest.py

Tests cover the following scenarios based on the contract specification:
- Empty manifest: new version addition
- Existing manifest: append new version while preserving existing entries
- Duplicate version: update existing entry instead of creating a duplicate
- isLatest flag: correctly updated across all entries
- Version sorting: descending order
- Validation: exactly one isLatest entry
"""

import json
import sys
import tempfile
import unittest
from datetime import datetime
from pathlib import Path
from unittest.mock import patch

# Add the script directory to the Python path
sys.path.insert(0, str(Path(__file__).parent))

# Import the module under test (filename contains hyphens, so use importlib)
import importlib
update_version_manifest = importlib.import_module("update-version-manifest")


class TestLoadManifest(unittest.TestCase):
    """Tests for load_manifest function."""

    def test_load_manifest_returns_empty_structure_when_file_does_not_exist(self):
        """Given a non-existent file, should return empty versions list."""
        non_existent = Path("/tmp/non_existent_manifest_test.json")
        if non_existent.exists():
            non_existent.unlink()
        
        result = update_version_manifest.load_manifest(non_existent)
        self.assertEqual(result, {"versions": []})

    def test_load_manifest_returns_existing_content(self):
        """Given an existing manifest file, should return its content."""
        with tempfile.NamedTemporaryFile(mode='w', suffix='.json', delete=False) as f:
            existing = {
                "versions": [
                    {
                        "version": "v0.1",
                        "isLatest": True,
                        "publishedDate": "2026-01-01T00:00:00Z"
                    }
                ]
            }
            json.dump(existing, f)
            f.flush()
            
            result = update_version_manifest.load_manifest(Path(f.name))
            self.assertEqual(result, existing)
            
            Path(f.name).unlink()

    def test_load_manifest_with_multiple_versions(self):
        """Given a manifest with multiple versions, should return all versions."""
        with tempfile.NamedTemporaryFile(mode='w', suffix='.json', delete=False) as f:
            existing = {
                "versions": [
                    {
                        "version": "v1.1",
                        "isLatest": True,
                        "publishedDate": "2026-02-01T00:00:00Z"
                    },
                    {
                        "version": "v1.0",
                        "isLatest": False,
                        "publishedDate": "2026-01-01T00:00:00Z"
                    }
                ]
            }
            json.dump(existing, f)
            f.flush()
            
            result = update_version_manifest.load_manifest(Path(f.name))
            self.assertEqual(len(result["versions"]), 2)
            
            Path(f.name).unlink()


class TestValidateManifest(unittest.TestCase):
    """Tests for validate_manifest function."""

    def test_validate_manifest_with_exactly_one_latest(self):
        """Valid manifest with exactly one isLatest=True should pass."""
        manifest = {
            "versions": [
                {"version": "v1.0", "isLatest": True, "publishedDate": "2026-01-01T00:00:00Z"}
            ]
        }
        self.assertTrue(update_version_manifest.validate_manifest(manifest))

    def test_validate_manifest_with_no_latest(self):
        """Manifest with no isLatest=True should fail."""
        manifest = {
            "versions": [
                {"version": "v1.0", "isLatest": False, "publishedDate": "2026-01-01T00:00:00Z"}
            ]
        }
        self.assertFalse(update_version_manifest.validate_manifest(manifest))

    def test_validate_manifest_with_multiple_latest(self):
        """Manifest with multiple isLatest=True should fail."""
        manifest = {
            "versions": [
                {"version": "v1.1", "isLatest": True, "publishedDate": "2026-02-01T00:00:00Z"},
                {"version": "v1.0", "isLatest": True, "publishedDate": "2026-01-01T00:00:00Z"}
            ]
        }
        self.assertFalse(update_version_manifest.validate_manifest(manifest))

    def test_validate_manifest_missing_versions_key(self):
        """Manifest without 'versions' key should fail."""
        manifest = {}
        self.assertFalse(update_version_manifest.validate_manifest(manifest))


class TestUpdateManifest(unittest.TestCase):
    """Tests for update_manifest function."""

    def test_add_version_to_empty_manifest(self):
        """Given empty manifest, should add the version as the first entry with isLatest=True."""
        with tempfile.NamedTemporaryFile(mode='w', suffix='.json', delete=False) as f:
            json.dump({"versions": []}, f)
            f.flush()
            manifest_path = Path(f.name)
        
        try:
            update_version_manifest.update_manifest(manifest_path, "v0.1")
            
            with open(manifest_path) as f:
                result = json.load(f)
            
            self.assertEqual(len(result["versions"]), 1)
            self.assertEqual(result["versions"][0]["version"], "v0.1")
            self.assertTrue(result["versions"][0]["isLatest"])
            self.assertIn("publishedDate", result["versions"][0])
        finally:
            manifest_path.unlink()

    def test_add_version_to_existing_manifest_preserves_entries(self):
        """Given manifest with v0.1, adding v0.2 should preserve v0.1 and add v0.2."""
        with tempfile.NamedTemporaryFile(mode='w', suffix='.json', delete=False) as f:
            existing = {
                "versions": [
                    {
                        "version": "v0.1",
                        "isLatest": True,
                        "publishedDate": "2026-01-01T00:00:00Z"
                    }
                ]
            }
            json.dump(existing, f)
            f.flush()
            manifest_path = Path(f.name)
        
        try:
            update_version_manifest.update_manifest(manifest_path, "v0.2")
            
            with open(manifest_path) as f:
                result = json.load(f)
            
            self.assertEqual(len(result["versions"]), 2)
            
            # Both versions should be present
            version_names = [v["version"] for v in result["versions"]]
            self.assertIn("v0.1", version_names)
            self.assertIn("v0.2", version_names)
            
            # v0.1 should still have its original publishedDate
            v01 = next(v for v in result["versions"] if v["version"] == "v0.1")
            self.assertEqual(v01["publishedDate"], "2026-01-01T00:00:00Z")
        finally:
            manifest_path.unlink()

    def test_add_version_marks_new_as_latest_and_unmarks_old(self):
        """Adding a new version should set isLatest=True for new and False for all existing."""
        with tempfile.NamedTemporaryFile(mode='w', suffix='.json', delete=False) as f:
            existing = {
                "versions": [
                    {
                        "version": "v0.1",
                        "isLatest": True,
                        "publishedDate": "2026-01-01T00:00:00Z"
                    }
                ]
            }
            json.dump(existing, f)
            f.flush()
            manifest_path = Path(f.name)
        
        try:
            update_version_manifest.update_manifest(manifest_path, "v0.2")
            
            with open(manifest_path) as f:
                result = json.load(f)
            
            v01 = next(v for v in result["versions"] if v["version"] == "v0.1")
            v02 = next(v for v in result["versions"] if v["version"] == "v0.2")
            
            self.assertFalse(v01["isLatest"])
            self.assertTrue(v02["isLatest"])
        finally:
            manifest_path.unlink()

    def test_duplicate_version_does_not_create_new_entry(self):
        """Adding the same version again should update existing entry, not create a duplicate."""
        with tempfile.NamedTemporaryFile(mode='w', suffix='.json', delete=False) as f:
            existing = {
                "versions": [
                    {
                        "version": "v0.2",
                        "isLatest": True,
                        "publishedDate": "2026-02-01T00:00:00Z"
                    },
                    {
                        "version": "v0.1",
                        "isLatest": False,
                        "publishedDate": "2026-01-01T00:00:00Z"
                    }
                ]
            }
            json.dump(existing, f)
            f.flush()
            manifest_path = Path(f.name)
        
        try:
            update_version_manifest.update_manifest(manifest_path, "v0.2")
            
            with open(manifest_path) as f:
                result = json.load(f)
            
            # Should still have exactly 2 versions, not 3
            self.assertEqual(len(result["versions"]), 2)
            
            # v0.2 should still be latest
            v02 = next(v for v in result["versions"] if v["version"] == "v0.2")
            self.assertTrue(v02["isLatest"])
            
            # v0.1 should not be latest
            v01 = next(v for v in result["versions"] if v["version"] == "v0.1")
            self.assertFalse(v01["isLatest"])
        finally:
            manifest_path.unlink()

    def test_duplicate_version_updates_published_date(self):
        """Re-running with the same version should update its publishedDate."""
        original_date = "2026-01-15T00:00:00Z"
        with tempfile.NamedTemporaryFile(mode='w', suffix='.json', delete=False) as f:
            existing = {
                "versions": [
                    {
                        "version": "v0.1",
                        "isLatest": True,
                        "publishedDate": original_date
                    }
                ]
            }
            json.dump(existing, f)
            f.flush()
            manifest_path = Path(f.name)
        
        try:
            update_version_manifest.update_manifest(manifest_path, "v0.1")
            
            with open(manifest_path) as f:
                result = json.load(f)
            
            v01 = next(v for v in result["versions"] if v["version"] == "v0.1")
            # publishedDate should be updated (different from original)
            self.assertNotEqual(v01["publishedDate"], original_date)
        finally:
            manifest_path.unlink()

    def test_versions_sorted_descending(self):
        """Versions should be sorted in descending order."""
        with tempfile.NamedTemporaryFile(mode='w', suffix='.json', delete=False) as f:
            existing = {
                "versions": [
                    {
                        "version": "v0.1",
                        "isLatest": True,
                        "publishedDate": "2026-01-01T00:00:00Z"
                    }
                ]
            }
            json.dump(existing, f)
            f.flush()
            manifest_path = Path(f.name)
        
        try:
            update_version_manifest.update_manifest(manifest_path, "v0.2")
            
            with open(manifest_path) as f:
                result = json.load(f)
            
            version_names = [v["version"] for v in result["versions"]]
            # v0.2 should come before v0.1 (descending)
            self.assertEqual(version_names, ["v0.2", "v0.1"])
        finally:
            manifest_path.unlink()

    def test_multiple_versions_sorted_correctly(self):
        """Multiple versions should be sorted in correct descending order."""
        with tempfile.NamedTemporaryFile(mode='w', suffix='.json', delete=False) as f:
            existing = {
                "versions": [
                    {
                        "version": "v1.1",
                        "isLatest": True,
                        "publishedDate": "2026-03-01T00:00:00Z"
                    },
                    {
                        "version": "v1.0",
                        "isLatest": False,
                        "publishedDate": "2026-02-01T00:00:00Z"
                    },
                    {
                        "version": "v0.1",
                        "isLatest": False,
                        "publishedDate": "2026-01-01T00:00:00Z"
                    }
                ]
            }
            json.dump(existing, f)
            f.flush()
            manifest_path = Path(f.name)
        
        try:
            update_version_manifest.update_manifest(manifest_path, "v2.0")
            
            with open(manifest_path) as f:
                result = json.load(f)
            
            version_names = [v["version"] for v in result["versions"]]
            self.assertEqual(version_names, ["v2.0", "v1.1", "v1.0", "v0.1"])
        finally:
            manifest_path.unlink()

    def test_output_is_valid_json_with_trailing_newline(self):
        """Output file should be valid JSON with a trailing newline."""
        with tempfile.NamedTemporaryFile(mode='w', suffix='.json', delete=False) as f:
            json.dump({"versions": []}, f)
            f.flush()
            manifest_path = Path(f.name)
        
        try:
            update_version_manifest.update_manifest(manifest_path, "v0.1")
            
            with open(manifest_path) as f:
                content = f.read()
            
            # Should end with newline
            self.assertTrue(content.endswith('\n'))
            
            # Should be valid JSON
            parsed = json.loads(content)
            self.assertIn("versions", parsed)
        finally:
            manifest_path.unlink()

    def test_manifest_file_does_not_exist_creates_new(self):
        """If manifest file doesn't exist, should create it with the new version."""
        with tempfile.TemporaryDirectory() as tmpdir:
            manifest_path = Path(tmpdir) / "versions.json"
            
            update_version_manifest.update_manifest(manifest_path, "v0.1")
            
            self.assertTrue(manifest_path.exists())
            
            with open(manifest_path) as f:
                result = json.load(f)
            
            self.assertEqual(len(result["versions"]), 1)
            self.assertEqual(result["versions"][0]["version"], "v0.1")
            self.assertTrue(result["versions"][0]["isLatest"])

    def test_published_date_is_utc_iso_format(self):
        """publishedDate should be in UTC ISO 8601 format ending with 'Z'."""
        with tempfile.NamedTemporaryFile(mode='w', suffix='.json', delete=False) as f:
            json.dump({"versions": []}, f)
            f.flush()
            manifest_path = Path(f.name)
        
        try:
            update_version_manifest.update_manifest(manifest_path, "v0.1")
            
            with open(manifest_path) as f:
                result = json.load(f)
            
            published_date = result["versions"][0]["publishedDate"]
            self.assertTrue(published_date.endswith("Z"))
            
            # Should be parseable as ISO 8601
            datetime.fromisoformat(published_date.rstrip("Z"))
        finally:
            manifest_path.unlink()

    def test_add_third_version_preserves_all_existing(self):
        """Adding a third version to a manifest with two versions should preserve all three."""
        with tempfile.NamedTemporaryFile(mode='w', suffix='.json', delete=False) as f:
            existing = {
                "versions": [
                    {
                        "version": "v0.2",
                        "isLatest": True,
                        "publishedDate": "2026-02-01T00:00:00Z"
                    },
                    {
                        "version": "v0.1",
                        "isLatest": False,
                        "publishedDate": "2026-01-01T00:00:00Z"
                    }
                ]
            }
            json.dump(existing, f)
            f.flush()
            manifest_path = Path(f.name)
        
        try:
            update_version_manifest.update_manifest(manifest_path, "v0.3")
            
            with open(manifest_path) as f:
                result = json.load(f)
            
            self.assertEqual(len(result["versions"]), 3)
            
            version_names = [v["version"] for v in result["versions"]]
            self.assertIn("v0.1", version_names)
            self.assertIn("v0.2", version_names)
            self.assertIn("v0.3", version_names)
            
            # Only v0.3 should be latest
            for v in result["versions"]:
                if v["version"] == "v0.3":
                    self.assertTrue(v["isLatest"])
                else:
                    self.assertFalse(v["isLatest"])
        finally:
            manifest_path.unlink()


if __name__ == "__main__":
    unittest.main()
