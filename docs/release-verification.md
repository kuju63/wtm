# Release Verification Guide

**Feature**: Release Binary Hash Files
**Audience**: End users of the wtm tool
**Last Updated**: 2026-02-17

## Overview

After downloading the wtm tool, you can verify that the files have not been corrupted or tampered with by checking their SHA256 hashes. This guide explains how to verify downloads on each platform.

---

## Why Verify Downloads?

Files can be corrupted during download or maliciously tampered with. Verifying SHA256 hashes allows you to:

- ✅ Confirm the file matches the official release exactly
- ✅ Detect corruption during download
- ✅ Protect against malicious tampering

---

## Download

Download the binary and hash file for your platform from the GitHub Releases page.

**Required files**:

1. Binary file (e.g., `wtm-v1.0.0-windows-x64.exe`)
2. Hash file (e.g., `wtm-v1.0.0-windows-x64.exe.sha256`)

Or:

1. Multiple binary files
2. `SHA256SUMS` file (contains hashes for all binaries)

---

## Windows Verification

### Prerequisites

- PowerShell 5.1 or later (included with Windows 10/11)

### Verify with Individual Hash File

Open PowerShell in the same folder as the downloaded binary and run:

```powershell
# Calculate hash of downloaded file
$hash = (Get-FileHash .\wtm-v1.0.0-windows-x64.exe -Algorithm SHA256).Hash

# Read expected hash from .sha256 file
$expected = (Get-Content .\wtm-v1.0.0-windows-x64.exe.sha256).Split(" ")[0]

# Compare
if ($hash -eq $expected) {
    Write-Host "✓ Hash verified: File is authentic" -ForegroundColor Green
} else {
    Write-Host "✗ Hash mismatch: File is corrupted or tampered" -ForegroundColor Red
}
```

### Batch Verification with SHA256SUMS

To verify multiple binaries:

```powershell
Get-Content SHA256SUMS | ForEach-Object {
    $expected, $file = $_ -split '  '
    if (Test-Path $file) {
        $actual = (Get-FileHash $file -Algorithm SHA256).Hash
        if ($actual -eq $expected) {
            Write-Host "✓ $file" -ForegroundColor Green
        } else {
            Write-Host "✗ $file - Hash mismatch!" -ForegroundColor Red
        }
    } else {
        Write-Host "- $file - File not found" -ForegroundColor Yellow
    }
}
```

**Example individual .sha256 file content** (`wtm-v1.0.0-windows-x64.exe.sha256`):

```text
a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd  wtm-v1.0.0-windows-x64.exe
```

Note: Two spaces separate the hash and filename.

---

## Linux Verification

### Prerequisites

- `sha256sum` command (included in most Linux distributions)

### Verify with Individual Hash File

Run this command in the same directory as the downloaded binary:

```bash
sha256sum -c wtm-v1.0.0-linux-x64.sha256
```

**Success output**:

```
wtm-v1.0.0-linux-x64: OK
```

**Failure output**:

```
wtm-v1.0.0-linux-x64: FAILED
sha256sum: WARNING: 1 computed checksum did NOT match
```

### Batch Verification with SHA256SUMS

To verify multiple binaries at once:

```bash
sha256sum -c SHA256SUMS
```

**Success output**:

```
wtm-v1.0.0-linux-x64: OK
wtm-v1.0.0-linux-arm: OK
wtm-v1.0.0-windows-x64.exe: OK
wtm-v1.0.0-macos-arm64: OK
```

**Example SHA256SUMS file content**:

```text
a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd  wtm-v1.0.0-linux-x64
b2c3d4e5f6a78901234567890123456789012345678901234567890123456789  wtm-v1.0.0-linux-arm
c3d4e5f6a7b89012345678901234567890123456789012345678901234567890  wtm-v1.0.0-macos-arm64
d4e5f6a7b8c90123456789012345678901234567890123456789012345678901  wtm-v1.0.0-windows-x64.exe
```

Note: Each line contains a hash (64 characters), two spaces, and the filename.

### Verify Specific File from SHA256SUMS

To verify only a specific file:

```bash
grep wtm-v1.0.0-linux-x64 SHA256SUMS | sha256sum -c
```

---

## macOS Verification

### Prerequisites

- `shasum` command (included with macOS)

### Verify with Individual Hash File

Run this command in the same directory as the downloaded binary:

```bash
shasum -a 256 -c wtm-v1.0.0-macos-arm64.sha256
```

**Success output**:

```
wtm-v1.0.0-macos-arm64: OK
```

**Failure output**:

```
wtm-v1.0.0-macos-arm64: FAILED
shasum: WARNING: 1 computed checksum did NOT match
```

### Batch Verification with SHA256SUMS

To verify multiple binaries at once:

```bash
shasum -a 256 -c SHA256SUMS
```

### Alternative Method: Using openssl

```bash
# Calculate hash
openssl sha256 wtm-v1.0.0-macos-arm64

# Output example:
# SHA256(wtm-v1.0.0-macos-arm64)= a1b2c3d4e5f6...

# Manually compare with hash file content
cat wtm-v1.0.0-macos-arm64.sha256
```

---

## Troubleshooting

### Hash Mismatch

1. **Re-download the file**: It may have been corrupted during download
2. **Re-download the hash file**: The hash file itself may be corrupted
3. **Download from official releases page**: Ensure you're downloading from a trusted source
4. **If still mismatched**: Report the issue on GitHub Issues

### File Not Found Error

**Cause**: Binary and hash file are not in the same directory

**Solution**:

- Place both files in the same folder
- Or specify absolute paths to the files

### Command Not Found Error

**Linux**:

```bash
# Debian/Ubuntu
sudo apt-get install coreutils

# Fedora/RHEL
sudo dnf install coreutils
```

**macOS**:

```bash
# Install GNU coreutils (if you want to use sha256sum instead of shasum)
brew install coreutils

# Use gsha256sum instead of sha256sum
gsha256sum -c wtm-v1.0.0-macos-arm64.sha256
```

---

## Frequently Asked Questions

### Q: What's the difference between SHA256SUMS and individual .sha256 files?

**A**:

- **SHA256SUMS**: Contains hashes for all binaries in one file. Convenient for verifying multiple files at once.
- **Individual .sha256 files**: One hash file per binary. Convenient for downloading and verifying a single binary.

Both contain the same hash values, and you can choose whichever method you prefer.

### Q: Is hash verification required?

**A**: Not required, but strongly recommended, especially when:

- Using in enterprise or production environments
- Security is critical for your use case
- Downloading from unofficial mirrors

### Q: What is the SHA256SUMS.asc file?

**A**: This is a GPG signature for the SHA256SUMS file. It provides advanced security verification. You can use a GPG public key to verify the signature, ensuring the hash file itself hasn't been tampered with.

For GPG verification details, see the project's SECURITY.md or documentation.

### Q: Are hash values case-sensitive?

**A**: No, SHA256 hash comparisons are case-insensitive. `A1B2C3...` and `a1b2c3...` are treated as the same hash.

---

## Next Steps

After successful verification:

1. **Move the binary to an appropriate location**:

   - Linux/macOS: `/usr/local/bin/` or `~/.local/bin/`
   - Windows: `C:\Program Files\wt\` or a directory in your PATH

2. **Grant execute permissions** (Linux/macOS):

   ```bash
   chmod +x wtm-v1.0.0-linux-x64
   ```

3. **Start using the wtm tool**:

   ```bash
   wtm --version
   wtm --help
   ```

For detailed usage instructions, see the [Installation Guide](installation.md).

---

## Related Resources

- [GitHub Releases Page](https://github.com/kuju63/wt/releases)
- [What is SHA256?](https://en.wikipedia.org/wiki/SHA-2)
- [File Integrity Verification Best Practices](https://help.ubuntu.com/community/HowToSHA256SUM)
- [PowerShell Get-FileHash Documentation](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/get-filehash)

---

**Need Help?** Please ask questions or report bugs on [GitHub Issues](https://github.com/kuju63/wt/issues).
