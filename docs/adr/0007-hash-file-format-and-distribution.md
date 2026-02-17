# ADR 0007: Hash File Format and Distribution Strategy

**Status**: Accepted
**Date**: 2026-02-17
**Context**: Feature 006 - Release Binary Hash Files
**Related**: [spec.md](../../specs/006-release-hash-files/spec.md), [plan.md](../../specs/006-release-hash-files/plan.md)

## Context

Users need to verify the integrity of downloaded binaries to ensure they have not been corrupted or tampered with during download. We need to provide hash files in a format that:

1. Works with standard command-line tools across all platforms (Windows, Linux, macOS)
2. Supports both single-file and batch verification workflows
3. Follows industry standards for maximum compatibility
4. Provides a good user experience without requiring specialized tools

The feature must support three user stories:

- **US1**: Automated hash generation on release
- **US2**: User-side integrity verification
- **US3**: Hash documentation in release notes

## Decision

### Hash File Format: GNU/Linux Standard

We chose the **GNU/Linux standard format** for both individual `.sha256` files and the combined `SHA256SUMS` file:

```text
<64-char-hex-hash>  <filename>
```

**Key characteristics**:

- Exactly **two spaces** between hash and filename (not one space)
- 64 hexadecimal characters (SHA256 hash)
- Filename includes relative path if applicable

**Example individual file** (`wt-v1.0.0-windows-x64.exe.sha256`):

```text
a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd  wt-v1.0.0-windows-x64.exe
```

**Example combined file** (`SHA256SUMS`):

```text
a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd  wt-v1.0.0-linux-x64
b2c3d4e5f6a78901234567890123456789012345678901234567890123456789  wt-v1.0.0-macos-arm64
c3d4e5f6a7b89012345678901234567890123456789012345678901234567890  wt-v1.0.0-windows-x64.exe
```

### Distribution Strategy: Dual Approach

We provide **both** individual `.sha256` files and a combined `SHA256SUMS` file:

| File Type | Purpose | Use Case |
|-----------|---------|----------|
| **Individual `.sha256`** | One file per binary | Single-file download verification |
| **SHA256SUMS** | All hashes in one file | Batch verification, CI/CD automation |

**Release assets structure**:

```
wt-v1.0.0-windows-x64.exe
wt-v1.0.0-windows-x64.exe.sha256  (NEW)
wt-v1.0.0-linux-x64
wt-v1.0.0-linux-x64.sha256  (NEW)
wt-v1.0.0-macos-arm64
wt-v1.0.0-macos-arm64.sha256  (NEW)
SHA256SUMS  (EXISTING - maintained)
SHA256SUMS.asc  (EXISTING - GPG signature)
```

### Release Notes Display

Hash values are displayed directly in release notes using a **code block format**:

```markdown
## 📋 Checksums

​```text
a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd  wt-v1.0.0-linux-x64
b2c3d4e5f6a78901234567890123456789012345678901234567890123456789  wt-v1.0.0-macos-arm64
c3d4e5f6a7b89012345678901234567890123456789012345678901234567890  wt-v1.0.0-windows-x64.exe
​```

See the [Release Verification Guide](link) for verification instructions.
```

**Rationale**:

- Users can quickly view hashes without downloading separate files
- Code block preserves formatting for copy-paste
- Link to verification guide keeps release notes concise
- GitHub release page already provides download links for assets

## Alternatives Considered

### Alternative 1: Hash-Only Format

**Approach**: Individual files contain only the hash value (no filename)

**Pros**:

- Simpler file structure
- Smaller file size

**Cons**:

- Cannot use automated verification tools (`sha256sum -c`, `shasum -a 256 -c`)
- Users must manually compare hash values
- Not compatible with standard Linux/macOS workflows

**Rejected**: Incompatible with standard command-line tools, poor user experience

### Alternative 2: BSD Format

**Approach**: Use BSD-style format: `SHA256 (filename) = hash`

**Example**:

```text
SHA256 (wt-v1.0.0-windows-x64.exe) = a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd
```

**Pros**:

- Self-documenting format
- Human-readable

**Cons**:

- Not the default format for `sha256sum` (Linux)
- Requires `-r` flag for macOS `shasum`
- Less common in industry (Node.js, Terraform use GNU format)

**Rejected**: Less standard, requires additional flags for verification

### Alternative 3: SHA256SUMS Only (No Individual Files)

**Approach**: Provide only the combined `SHA256SUMS` file

**Pros**:

- Simpler release workflow
- Fewer files to upload
- Standard practice for many Linux distributions

**Cons**:

- Users downloading a single binary must download `SHA256SUMS` and parse it
- Some package managers expect individual hash files
- Less convenient for single-file verification

**Rejected**: Poor UX for single-file downloads, misses use case for individual verification

### Alternative 4: Individual .sha256 Only (No SHA256SUMS)

**Approach**: Provide only individual `.sha256` files

**Pros**:

- Convenient for single-file downloads
- Cleaner directory structure

**Cons**:

- No batch verification workflow
- Difficult for CI/CD to verify all downloads at once
- Breaks existing workflows expecting `SHA256SUMS`

**Rejected**: Removes backward compatibility, poor for automation

### Alternative 5: Inline Hashes in Release Notes Only

**Approach**: Display hashes only in release notes, no separate files

**Pros**:

- Minimal file overhead
- Users can view hashes immediately

**Cons**:

- Cannot use automated verification tools
- Manual copy-paste from web page is error-prone
- No programmatic verification in CI/CD
- Not standard practice

**Rejected**: Fails to meet automated verification requirements (FR-001, FR-002)

## Consequences

### Positive

- **Cross-Platform Compatibility**: Works with `sha256sum` (Linux), `shasum` (macOS), `Get-FileHash` (Windows PowerShell)
- **Standard Compliance**: Follows GNU/Linux format used by Node.js, Terraform, and major distributions
- **Dual Workflow Support**: Serves both single-file and batch verification use cases
- **Automated Verification**: Compatible with standard CLI tools (`sha256sum -c`, `shasum -a 256 -c`)
- **Backward Compatibility**: Maintains existing `SHA256SUMS` file for users already using it
- **Visibility**: Hash values displayed in release notes for quick reference

### Negative

- **File Overhead**: Adds 4-5 small files per release (~100 bytes each, <1 KB total)
- **Duplicate Information**: Same hash values stored in both individual files and `SHA256SUMS`
- **Maintenance**: Two types of hash files to generate and validate

### Mitigations

1. **Automated Generation**: `.github/scripts/generate-checksums.sh` generates both formats automatically
2. **Format Validation**: `.github/workflows/verify-hashes.yml` validates format compliance on PRs
3. **Error Handling**: Workflow fails if hash generation fails (FR-005 requirement)
4. **Documentation**: Comprehensive verification guides for all platforms

## Verification Workflow

### Automated (CI/CD)

```bash
# GitHub Actions: verify-hashes.yml
- Check all binaries have .sha256 files
- Validate hash format (64 hex chars)
- Validate file format (two spaces separator)
- Verify hash consistency (individual vs SHA256SUMS)
- Run sha256sum -c to verify correctness
```

### User Workflows

**Linux**:

```bash
sha256sum -c wt-v1.0.0-linux-x64.sha256
# Or batch: sha256sum -c SHA256SUMS
```

**macOS**:

```bash
shasum -a 256 -c wt-v1.0.0-macos-arm64.sha256
# Or batch: shasum -a 256 -c SHA256SUMS
```

**Windows PowerShell**:

```powershell
$hash = (Get-FileHash .\wt-v1.0.0-windows-x64.exe).Hash
$expected = (Get-Content .\wt-v1.0.0-windows-x64.exe.sha256).Split(" ")[0]
$hash -eq $expected  # Should return True
```

## Impact on Project Constitution

This decision aligns with project constitution principles:

- **IV. Documentation Clarity**: Comprehensive Japanese and English verification guides
- **V. Minimal Dependencies**: Uses OS-standard tools only (sha256sum, shasum, Get-FileHash)
- **VI. Comprehensive Testing**: Automated format validation and end-to-end testing

## References

### Standards & Research

- [HowToSHA256SUM - Ubuntu Community Help Wiki](https://help.ubuntu.com/community/HowToSHA256SUM)
- [sha256sum(1) - Linux manual page](https://man7.org/linux/man-pages/man1/sha256sum.1.html)
- [Get-FileHash PowerShell documentation](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/get-filehash)

### Industry Examples

- [Node.js SHASUMS256.txt format](http://nodejs.org/dist/latest/SHASUMS256.txt) - GNU/Linux format
- [Terraform SHA256SUMS format](https://discuss.hashicorp.com/t/change-in-sha256sums-for-terraform-downloads/52522) - GNU/Linux format
- [Deno releases with checksums](https://github.com/denoland/deno/releases) - Individual .sha256sum files

### Project Documentation

- [Research Findings](../../specs/006-release-hash-files/research.md)
- [Data Model](../../specs/006-release-hash-files/data-model.md)
- [Quickstart Guide (Japanese)](../../specs/006-release-hash-files/quickstart.md)

## Review Schedule

This ADR should be reviewed:

- If SHA256 is deprecated or replaced with a stronger algorithm
- If standard hash file formats change significantly
- If user feedback indicates verification difficulties
- After 6 months of releases to assess user adoption and issues
