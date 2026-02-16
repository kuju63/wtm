# Release Hash Files Feature - Research Findings

**Date**: 2026-02-14
**Feature**: Release Binary Hash Files
**Branch**: 001-release-hash-files

## Overview

This document consolidates research findings for implementing hash files in the release process. Research focused on industry standards, cross-platform compatibility, and best practices from popular open-source projects.

---

## 1. Individual .sha256 File Format

### Decision

**Use GNU/Linux format: `<hash>  <filename>` (hash, two spaces, filename)**

### Rationale

- **Standard compatibility**: Default format produced by `sha256sum` on Linux
- **Cross-platform support**: Works with verification tools across all platforms:
  - Linux: `sha256sum -c file.sha256`
  - macOS: `shasum -a 256 -c file.sha256`
  - Windows PowerShell: Can parse and compare programmatically
- **Two spaces separator**: Exact format expected by standard tools (not one space)
- **Tool compatibility**: Node.js, Terraform, and most major projects use this format

### Alternatives Considered

| Alternative | Pros | Cons | Verdict |
|-------------|------|------|---------|
| Hash-only format | Simpler, smaller file size | Cannot use automated verification, requires manual scripting | Not recommended |
| BSD format `SHA256 (filename) = hash` | Self-documenting, human-readable | Not default on most systems, less common | Valid but less standard |
| Hash + filename (single space) | Similar to standard | Technically incorrect, causes verification failures | Avoid |

### Examples from Popular Projects

**Node.js** (SHASUMS256.txt):
```
21eacf97f520b95b8e6d774e68832c323a8118767f3c1a95a431de7169c89c2f  node-v25.6.1-aix-ppc64.tar.gz
a80cb252d170a4730f78f5950cf19a46106f156e5886e5c1cc8c5602aea60243  node-v25.6.1-darwin-arm64.tar.gz
```

**Terraform** (terraform-provider checksums):
```
3a61fff3689f27c89bce22893219919c629d2e10b96e7eadd5fef9f0e90bb353  tflint-ruleset-aws_darwin_amd64.zip
482419fdeed00692304e59558b5b0d915d4727868b88a5adbbbb76f5ed1b537a  tflint-ruleset-aws_linux_amd64.zip
```

---

## 2. SHA256SUMS vs Individual .sha256 Files

### Decision

**Provide BOTH: `SHA256SUMS` file (all hashes) + individual `.sha256` files (per binary)**

### Rationale

- **Maximum compatibility**: Serves different user workflows and automation needs
- **SHA256SUMS benefits**:
  - Single file to verify all downloads at once
  - Standard practice in Linux distributions (Ubuntu, Debian)
  - Easier for CI/CD pipelines to download one file
  - Compatible with `sha256sum -c SHA256SUMS` for batch verification
- **Individual .sha256 benefits**:
  - Convenient for single-file downloads
  - Some package managers expect individual hash files
  - Better for automated downloaders that fetch binary + hash together
  - Cleaner directory structure when downloading specific platform builds
- **No conflicts**: Both can coexist without issues

### Industry Standard

| Project | Format | Notes |
|---------|--------|-------|
| Node.js | `SHASUMS256.txt` + GPG signature | All binaries in one file |
| Terraform | `terraform_X.Y.Z_SHA256SUMS` | All binaries in one file |
| Deno | Individual `.sha256sum` files | Per-binary files |
| GoReleaser | `checksums.txt` (customizable) | Configurable format |

### Implementation for wt

- Generate `SHA256SUMS` file (existing, maintain for backward compatibility)
- Generate individual `.sha256` files (new, per FR-002)
- Both contain identical hash values in same format
- No conflicts between formats

---

## 3. Hash Display in Release Notes

### Decision

**Use code block with download links, optionally include collapsed details section for full checksums**

### Rationale

- **Readability**: Code blocks preserve formatting, enable copy-paste without errors
- **Maintainability**: Can be auto-generated from checksum files during release
- **Standard practice**: Most projects use code blocks or tables
- **File references**: Direct users to download checksum files rather than embedding all hashes inline

### Recommended Format (Option A - Simple Reference)

```markdown
## Checksums

SHA256 checksums are available for all release assets:
- Download [`SHA256SUMS`](https://github.com/user/repo/releases/download/v1.0.0/SHA256SUMS)
- Individual `.sha256` files are included alongside each binary

### Verification

**Linux/macOS:**
```bash
sha256sum -c SHA256SUMS
```

**Windows (PowerShell):**
```powershell
$hash = (Get-FileHash wt.exe -Algorithm SHA256).Hash
$expected = (Get-Content wt.exe.sha256).Split(" ")[0]
$hash -eq $expected
```
```

### Alternative Format (Option B - Inline Code Block)

For releases with few binaries (<10):

```markdown
## Checksums

```text
a1b2c3d4...  wt-v1.0.0-linux-amd64
b2c3d4e5...  wt-v1.0.0-darwin-amd64
c3d4e5f6...  wt-v1.0.0-windows-x64.exe
```

Download [`SHA256SUMS`](link) to verify all files.
```

### Alternative Format (Option C - Collapsed Details)

For comprehensive releases:

```markdown
## Checksums

Download [`SHA256SUMS`](link) or individual `.sha256` files for verification.

<details>
<summary>View all SHA256 checksums</summary>

```text
a1b2c3d4e5f6...  wt-v1.0.0-linux-amd64
b2c3d4e5f6a7...  wt-v1.0.0-darwin-amd64
c3d4e5f6a7b8...  wt-v1.0.0-windows-x64.exe
```
</details>
```

### Alternatives Considered

| Alternative | Pros | Cons | Verdict |
|-------------|------|------|---------|
| Markdown table | Structured, easy to scan | Breaks on mobile, hard to copy-paste | Avoid |
| Bulleted list | Simple, readable | Not standard format, can't pipe to tools | Acceptable for short lists |
| No display | Cleaner release notes | Users unaware of checksums | Not recommended |

---

## 4. Cross-Platform Hash Verification

### Windows (PowerShell)

**Verify individual .sha256 file:**

```powershell
# Method 1: Manual comparison
$hash = (Get-FileHash .\wt.exe -Algorithm SHA256).Hash
$expected = (Get-Content .\wt.exe.sha256).Split(" ")[0]
if ($hash -eq $expected) {
    Write-Host "✓ Checksum verified" -ForegroundColor Green
} else {
    Write-Host "✗ Checksum mismatch!" -ForegroundColor Red
}

# Method 2: One-liner
(Get-FileHash .\wt.exe).Hash -eq (Get-Content .\wt.exe.sha256).Split(" ")[0]
```

**Verify SHA256SUMS file:**

```powershell
Get-Content SHA256SUMS | ForEach-Object {
    $expected, $file = $_ -split '  '
    $actual = (Get-FileHash $file -Algorithm SHA256).Hash
    if ($actual -eq $expected) {
        Write-Host "✓ $file" -ForegroundColor Green
    } else {
        Write-Host "✗ $file" -ForegroundColor Red
    }
}
```

**Key considerations:**
- `Get-FileHash` is built-in to PowerShell 5.1+ and PowerShell Core
- Default algorithm is SHA256
- Hash output is uppercase, but comparison is case-insensitive

### Linux (sha256sum)

**Verify individual .sha256 file:**

```bash
sha256sum -c wt-linux-amd64.sha256
# Output: wt-linux-amd64: OK
```

**Verify SHA256SUMS file:**

```bash
sha256sum -c SHA256SUMS
# Output:
# wt-linux-amd64: OK
# wt-darwin-amd64: OK
# wt.exe: OK
```

**Verify specific file from SHA256SUMS:**

```bash
grep wt-linux-amd64 SHA256SUMS | sha256sum -c
```

**Key considerations:**
- `sha256sum` expects exactly two spaces between hash and filename
- Files must be in the same directory or use relative paths
- Exit code 0 = success, non-zero = failure (good for CI/CD)

### macOS (shasum)

**Verify individual .sha256 file:**

```bash
shasum -a 256 -c wt-darwin-amd64.sha256
# Output: wt-darwin-amd64: OK
```

**Verify SHA256SUMS file:**

```bash
shasum -a 256 -c SHA256SUMS
```

**Alternative with openssl:**

```bash
openssl sha256 wt-darwin-amd64
# Output: SHA256(wt-darwin-amd64)= a1b2c3d4...
```

**Key considerations:**
- macOS doesn't have `sha256sum` by default - use `shasum -a 256`
- `-a` flag specifies algorithm (256 for SHA-256)
- Same format as Linux `sha256sum` (two spaces separator)

---

## Summary: Technical Decisions

| Aspect | Decision | Format/Example |
|--------|----------|----------------|
| **Individual file format** | GNU/Linux standard | `<hash>  <filename>` (two spaces) |
| **File extension** | `.sha256` | `wt-v1.0.0-windows-x64.exe.sha256` |
| **Combined file** | Yes, maintain | `SHA256SUMS` (all binaries) |
| **Release notes display** | Code block + download link | See Option A (recommended) |
| **File content** | Hash + filename | Both included in each `.sha256` file |
| **Algorithm** | SHA256 only | Modern standard, widely supported |

---

## Implementation Guidelines

### File Generation

1. Generate SHA256 hash for each binary using platform-native tools
2. Create individual `.sha256` files with format: `<hash>  <filename>` (two spaces)
3. Create combined `SHA256SUMS` file with all hashes
4. Maintain existing `SHA256SUMS.asc` GPG signature (backward compatibility)

### Release Assets

Upload the following to GitHub Releases:

- Binary files (existing): `wt-v1.0.0-windows-x64.exe`, `wt-v1.0.0-linux-amd64`, etc.
- Individual hash files (new): `wt-v1.0.0-windows-x64.exe.sha256`, etc.
- Combined hash file (existing): `SHA256SUMS`
- GPG signature (existing): `SHA256SUMS.asc`
- SBOM (existing): `wt-v1.0.0-sbom.spdx.json`, `wt-v1.0.0-sbom.spdx.json.asc`

### Release Notes Template

Use Option A (Simple Reference) format:

- Include "Checksums" section
- Link to `SHA256SUMS` file
- Mention individual `.sha256` files
- Provide verification instructions for Windows, Linux, macOS

### Testing Checklist

- [ ] Generate `.sha256` files with correct format (two spaces)
- [ ] Verify `.sha256` files with `sha256sum -c` on Linux
- [ ] Verify `.sha256` files with `shasum -a 256 -c` on macOS
- [ ] Verify `.sha256` files with PowerShell on Windows
- [ ] Verify `SHA256SUMS` file with batch verification on all platforms
- [ ] Confirm release notes template includes checksums section
- [ ] Test download + verification workflow for end users

---

## References

### Standards & Documentation

- [HowToSHA256SUM - Ubuntu Community Help Wiki](https://help.ubuntu.com/community/HowToSHA256SUM)
- [sha256sum(1) - Linux manual page](https://man7.org/linux/man-pages/man1/sha256sum.1.html)
- [Get-FileHash PowerShell documentation](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/get-filehash)

### Industry Examples

- [Node.js SHASUMS256.txt format](http://nodejs.org/dist/latest/SHASUMS256.txt)
- [Terraform SHA256SUMS format](https://discuss.hashicorp.com/t/change-in-sha256sums-for-terraform-downloads/52522)
- [Deno releases with checksums](https://github.com/denoland/deno/releases)
- [GoReleaser Checksums documentation](https://goreleaser.com/customization/checksum/)

---

**Note**: All research findings are based on current industry standards as of 2026-02-14. No NEEDS CLARIFICATION markers remain - all technical decisions have been made based on established best practices.
