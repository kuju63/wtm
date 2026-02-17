# Data Model: Release Binary Hash Files

**Feature**: Release Binary Hash Files
**Branch**: 006-release-hash-files
**Date**: 2026-02-14

## Overview

This document defines the data structures and relationships for the release hash files feature. Since this is a CI/CD workflow feature, the data model focuses on file formats and artifact metadata rather than database schemas.

---

## Entities

### 1. Binary Artifact

A compiled executable or archive file included in a release.

**Attributes**:
- **filename** (string): Full name with version and platform (e.g., `wt-v1.0.0-windows-x64.exe`)
- **path** (string): Relative path in release-assets directory (e.g., `release-assets/wt-v1.0.0-windows-x64.exe`)
- **version** (string): Release version tag (e.g., `v1.0.0`)
- **platform** (string): Target platform (e.g., `windows-x64`, `linux-x64`, `linux-arm`, `macos-arm64`)
- **size** (integer): File size in bytes
- **format** (string): File extension/type (e.g., `.exe`, no extension for Unix executables)

**Relationships**:
- Has **exactly one** corresponding Individual Hash File
- Appears in **one** Combined Hash File (SHA256SUMS)
- Part of **one** Release

**Validation Rules**:
- Filename must follow pattern: `wt-<version>-<platform>[.exe]`
- Version must be valid semver with `v` prefix
- Platform must be one of: `windows-x64`, `linux-x64`, `linux-arm`, `macos-arm64`
- File must exist and be readable

**State Transitions**:
```
[Built] → [Hash Generated] → [Uploaded to Release]
```

**Example**:
```json
{
  "filename": "wt-v1.0.0-windows-x64.exe",
  "path": "release-assets/wt-v1.0.0-windows-x64.exe",
  "version": "v1.0.0",
  "platform": "windows-x64",
  "size": 8388608,
  "format": ".exe"
}
```

---

### 2. Individual Hash File

A text file containing the SHA256 checksum for a single binary artifact.

**Attributes**:
- **filename** (string): Binary filename + `.sha256` extension (e.g., `wt-v1.0.0-windows-x64.exe.sha256`)
- **path** (string): Relative path in release-assets directory
- **hash_value** (string): 64-character hexadecimal SHA256 hash
- **binary_filename** (string): Name of the corresponding binary file
- **format** (string): Always `GNU/Linux standard` (hash + two spaces + filename)
- **algorithm** (string): Always `SHA256`

**Relationships**:
- Corresponds to **exactly one** Binary Artifact
- Derived from same source as Combined Hash File

**Validation Rules**:
- Filename must be `<binary-filename>.sha256`
- Hash value must be exactly 64 hexadecimal characters (lowercase or uppercase)
- File content must follow format: `<hash>  <filename>` (two spaces)
- Binary filename in content must match the actual binary filename

**File Format**:
```text
<64-char-hex-hash>  <binary-filename>
```

**Example File Content** (`wt-v1.0.0-windows-x64.exe.sha256`):
```text
a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd  wt-v1.0.0-windows-x64.exe
```

**Example Metadata**:
```json
{
  "filename": "wt-v1.0.0-windows-x64.exe.sha256",
  "path": "release-assets/wt-v1.0.0-windows-x64.exe.sha256",
  "hash_value": "a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd",
  "binary_filename": "wt-v1.0.0-windows-x64.exe",
  "format": "GNU/Linux standard",
  "algorithm": "SHA256"
}
```

---

### 3. Combined Hash File (SHA256SUMS)

A single text file containing SHA256 checksums for all binary artifacts in a release.

**Attributes**:
- **filename** (string): Always `SHA256SUMS`
- **path** (string): Always `release-assets/SHA256SUMS`
- **entries** (array): List of hash entries, one per binary
- **format** (string): Always `GNU/Linux standard` (hash + two spaces + filename per line)
- **algorithm** (string): Always `SHA256`
- **signature_file** (string): Optional GPG signature file (`SHA256SUMS.asc`)

**Relationships**:
- Contains checksums for **all** Binary Artifacts in a release
- Has **one optional** GPG Signature File

**Validation Rules**:
- Must contain at least one entry
- Each entry must follow format: `<hash>  <filename>\n`
- All binary artifacts in release must have corresponding entry
- Entries should be sorted alphabetically by filename (convention)
- File must end with newline

**File Format**:
```text
<hash-1>  <binary-1>
<hash-2>  <binary-2>
...
<hash-n>  <binary-n>
```

**Example File Content** (`SHA256SUMS`):
```text
b2c3d4e5f6a78901234567890123456789012345678901234567890123456789  wt-v1.0.0-linux-amd64
c3d4e5f6a7b89012345678901234567890123456789012345678901234567890  wt-v1.0.0-linux-arm
d4e5f6a7b8c90123456789012345678901234567890123456789012345678901  wt-v1.0.0-macos-arm64
a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd  wt-v1.0.0-windows-x64.exe
```

**Example Metadata**:
```json
{
  "filename": "SHA256SUMS",
  "path": "release-assets/SHA256SUMS",
  "entries": [
    {
      "hash": "b2c3d4e5f6a78901234567890123456789012345678901234567890123456789",
      "filename": "wt-v1.0.0-linux-amd64"
    },
    {
      "hash": "c3d4e5f6a7b89012345678901234567890123456789012345678901234567890",
      "filename": "wt-v1.0.0-linux-arm"
    },
    {
      "hash": "d4e5f6a7b8c90123456789012345678901234567890123456789012345678901",
      "filename": "wt-v1.0.0-macos-arm64"
    },
    {
      "hash": "a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd",
      "filename": "wt-v1.0.0-windows-x64.exe"
    }
  ],
  "format": "GNU/Linux standard",
  "algorithm": "SHA256",
  "signature_file": "SHA256SUMS.asc"
}
```

---

### 4. Release

A published version of the wt CLI tool with associated assets.

**Attributes**:
- **tag_name** (string): Git tag and version identifier (e.g., `v1.0.0`)
- **name** (string): Release title (same as tag_name)
- **body** (string): Release notes in markdown format (includes checksums section)
- **draft** (boolean): Whether release is a draft (always `false` for published releases)
- **prerelease** (boolean): Whether release is a pre-release
- **created_at** (datetime): Release creation timestamp
- **published_at** (datetime): Release publication timestamp

**Relationships**:
- Contains **multiple** Binary Artifacts (3-5 typically)
- Contains **multiple** Individual Hash Files (one per binary)
- Contains **one** Combined Hash File (SHA256SUMS)
- Contains **one** SBOM file
- Has **one** Release Notes document

**Validation Rules** (from Success Criteria):
- Must have at least one binary artifact
- Every binary artifact must have corresponding `.sha256` file
- Must have exactly one `SHA256SUMS` file
- Release notes must include checksums section
- All hash files must be generated before release publication

**Assets List**:
```
- wt-<version>-windows-x64.exe
- wt-<version>-windows-x64.exe.sha256  (NEW)
- wt-<version>-linux-x64
- wt-<version>-linux-x64.sha256  (NEW)
- wt-<version>-linux-arm
- wt-<version>-linux-arm.sha256  (NEW)
- wt-<version>-macos-arm64
- wt-<version>-macos-arm64.sha256  (NEW)
- SHA256SUMS  (EXISTING)
- SHA256SUMS.asc  (EXISTING)
- wt-<version>-sbom.spdx.json  (EXISTING)
- wt-<version>-sbom.spdx.json.asc  (EXISTING)
```

---

## Relationships Diagram

```text
┌─────────────────┐
│     Release     │
│   (v1.0.0)      │
└────────┬────────┘
         │
         │ contains (1:N)
         │
    ┌────▼───────────────────────┐
    │                            │
    │  Binary Artifacts (N)      │
    │                            │
    ├────────────────────────────┤
    │ - wt-v1.0.0-windows-x64.exe│◄─────┐
    │ - wt-v1.0.0-linux-x64      │◄───┐ │
    │ - wt-v1.0.0-linux-arm      │◄─┐ │ │
    │ - wt-v1.0.0-macos-arm64    │◄┐│ │ │
    └────────────────────────────┘ ││ │ │
                                   ││ │ │
           ┌───────────────────────┘│ │ │
           │  has (1:1)             │ │ │
           │                        │ │ │
    ┌──────▼──────────────┐  ┌──────▼─▼─▼──────────┐
    │ Individual Hash     │  │ Combined Hash File  │
    │ Files (N)           │  │   (SHA256SUMS)      │
    ├─────────────────────┤  ├─────────────────────┤
    │ - *.sha256 (each)   │  │ Contains all hashes │
    │   Format:           │  │   Format:           │
    │   <hash>  <file>    │  │   <hash>  <file>    │
    └─────────────────────┘  │   (one per line)    │
                             └─────────────────────┘
                                      │
                                      │ signed by (0:1)
                                      │
                             ┌────────▼────────────┐
                             │ GPG Signature File  │
                             │ (SHA256SUMS.asc)    │
                             └─────────────────────┘
```

---

## File Naming Conventions

### Binary Artifacts

**Pattern**: `wt-<version>-<platform>[.exe]`

**Examples**:
- `wt-v1.0.0-windows-x64.exe`
- `wt-v1.0.0-linux-x64`
- `wt-v1.0.0-linux-arm`
- `wt-v1.0.0-macos-arm64`

**Rules**:
- Version includes `v` prefix
- Platform uses lowercase with hyphens
- Windows binaries have `.exe` extension
- Unix binaries have no extension

### Individual Hash Files

**Pattern**: `<binary-filename>.sha256`

**Examples**:
- `wt-v1.0.0-windows-x64.exe.sha256`
- `wt-v1.0.0-linux-x64.sha256`
- `wt-v1.0.0-linux-arm.sha256`
- `wt-v1.0.0-macos-arm64.sha256`

**Rules**:
- Exact binary filename + `.sha256` extension
- Maintained even if binary has `.exe` extension

### Combined Hash File

**Pattern**: `SHA256SUMS` (fixed name)

**Signature**: `SHA256SUMS.asc` (fixed name)

---

## Validation & Integrity

### Hash Value Validation

**Format**:
- Exactly 64 characters
- Hexadecimal only (0-9, a-f, A-F)
- Case-insensitive for comparison
- Conventionally lowercase

**Regex Pattern**:
```regex
^[a-fA-F0-9]{64}$
```

### File Format Validation

**Individual .sha256 File**:
```regex
^[a-fA-F0-9]{64}  .+$
```
- Hash (64 hex chars)
- Two spaces
- Filename (any valid filename)
- Newline (optional at EOF)

**SHA256SUMS File**:
```regex
^([a-fA-F0-9]{64}  .+\n)+$
```
- One or more entries
- Each entry: hash + two spaces + filename + newline
- File ends with newline

### Integrity Checks (CI/CD Workflow)

**Pre-Release Validation**:
1. Verify all binary artifacts have corresponding `.sha256` files
2. Verify `SHA256SUMS` contains entry for each binary
3. Verify hash values match between individual and combined files
4. Verify file format compliance (two spaces separator)
5. Verify hash values are valid hexadecimal

**Post-Release Validation** (optional workflow):
1. Download all release assets
2. Verify each binary against its `.sha256` file
3. Verify all binaries against `SHA256SUMS` file
4. Verify `SHA256SUMS.asc` GPG signature (if present)

---

## Processing Workflow

### Hash Generation Flow

```text
1. Build Binaries
   └─> release-assets/wt-v1.0.0-<platform>[.exe]

2. Generate Individual Hash Files
   ├─> For each binary:
   │   ├─> Calculate SHA256 hash
   │   ├─> Create <binary>.sha256 file
   │   └─> Write: "<hash>  <binary-filename>\n"

3. Generate Combined Hash File
   ├─> Collect all hash values
   ├─> Sort by filename (optional, convention)
   ├─> Write SHA256SUMS file
   └─> Format: "<hash>  <filename>\n" per binary

4. Sign Combined Hash File (optional)
   └─> Generate SHA256SUMS.asc (GPG signature)

5. Upload to Release
   ├─> Upload binaries
   ├─> Upload .sha256 files
   ├─> Upload SHA256SUMS
   └─> Upload SHA256SUMS.asc
```

### Hash Verification Flow (User)

**Individual File Verification**:
```text
1. Download binary (e.g., wt-v1.0.0-windows-x64.exe)
2. Download corresponding .sha256 file
3. Run platform verification command:
   - Linux: sha256sum -c wt-v1.0.0-windows-x64.exe.sha256
   - macOS: shasum -a 256 -c wt-v1.0.0-windows-x64.exe.sha256
   - Windows: PowerShell comparison script
4. Verify output shows "OK" or hash match
```

**Batch Verification**:
```text
1. Download all binaries
2. Download SHA256SUMS file
3. Run batch verification:
   - Linux/macOS: sha256sum -c SHA256SUMS
   - Windows: PowerShell loop through entries
4. Verify all entries show "OK" or hash match
```

---

## Error Handling

### Generation Errors

| Error | Cause | Resolution |
|-------|-------|------------|
| Binary not found | File path incorrect | Fix binary organization step |
| Hash calculation failed | File read error, permissions | Check file permissions, retry |
| .sha256 file write failed | Disk full, permissions | Check disk space, directory permissions |
| SHA256SUMS incomplete | Not all binaries hashed | Retry hash generation for missing binaries |

**Workflow Behavior**: On any generation error, fail the release workflow (FR-005) and notify release manager.

### Verification Errors (User-Side)

| Error | Cause | Resolution |
|-------|-------|------------|
| Hash mismatch | Corrupted download, tampered file | Re-download binary and hash file |
| File not found | Missing binary or hash file | Ensure both files downloaded |
| Invalid format | Malformed .sha256 file | Re-download hash file from official release |
| Tool not found | Missing sha256sum/shasum | Install coreutils or use alternative method |

---

## Performance Considerations

### Hash Generation

**Typical Performance**:
- Small binaries (<10MB): <1 second per file
- Large binaries (50-100MB): 2-5 seconds per file
- Total for all binaries (4-5 files): <10 seconds

**Optimization**:
- Generate hashes in parallel (one per binary)
- Use buffered I/O for large files
- Cache hashes if binary unchanged (not applicable for releases)

### Storage

**Disk Usage**:
- Individual .sha256 file: ~100 bytes each
- SHA256SUMS file: ~100 bytes × number of binaries
- Total overhead: <1 KB for typical release

**Network Usage**:
- Additional downloads per user: 4-5 × 100 bytes = <1 KB
- SHA256SUMS download: ~500 bytes
- Negligible impact on release asset storage and bandwidth

---

## Notes

- This data model describes file formats and metadata, not database schemas (feature is CI/CD only)
- All hash values use SHA256 algorithm exclusively (industry standard as of 2026)
- File format follows GNU/Linux standard for maximum compatibility
- GPG signature for SHA256SUMS is existing feature, maintained for backward compatibility
- Individual .sha256 files are new addition per FR-002
