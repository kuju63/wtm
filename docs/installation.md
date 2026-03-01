# Installation Guide

This guide will help you install the `wtm` command-line tool for Git worktree management.

## Quick Install

The fastest way to install `wtm`:

**macOS / Linux:**

```bash
curl -fsSL https://kuju63.github.io/wt/install.sh | sh
```

**Windows (PowerShell):**

```powershell
irm https://kuju63.github.io/wt/install.ps1 | iex
```

The installer automatically:

- Detects your platform and architecture
- Downloads the latest release
- Verifies the SHA256 checksum
- Installs to `~/.local/bin` (Unix) or `%LOCALAPPDATA%\Programs\wtm` (Windows)

For custom install directory, use `--prefix` (Unix) or `-Prefix` (Windows):

```bash
curl -fsSL https://kuju63.github.io/wt/install.sh | sh -s -- --prefix /usr/local
```

```powershell
& ([scriptblock]::Create((irm https://kuju63.github.io/wt/install.ps1))) -Prefix "$env:ProgramFiles\wtm"
```

---

## System Requirements

Before installing `wtm`, ensure your system meets the following requirements:

| Operating System | Minimum Version | Architecture |
|-----------------|-----------------|--------------|
| Windows         | Windows 10      | x64          |
| macOS           | macOS 11 (Big Sur) | ARM64 (Apple Silicon) |
| Linux           | Any modern distribution | x64, ARM   |

### Prerequisites

- **Git**: Version 2.25 or later must be installed and available in your PATH
  - To verify: `git --version`
- **Terminal/Command Prompt**: Access to a terminal or command prompt

---

## Download

Download the latest release for your platform from the [GitHub Releases](https://github.com/YOUR_USERNAME/wt/releases) page.

### Available Downloads

Download the latest version from the releases page. Files are named with the version number:

| Platform | Architecture | File Pattern |
|----------|-------------|--------------|
| Windows  | x64         | `wtm-v{version}-win-x64.zip` |
| macOS    | ARM64       | `wtm-v{version}-osx-arm64.tar.gz` |
| Linux    | x64         | `wtm-v{version}-linux-x64.tar.gz` |
| Linux    | ARM         | `wtm-v{version}-linux-arm.tar.gz` |

Example: For version 0.0.3, the Linux x64 file would be `wtm-v0.0.3-linux-x64.tar.gz`

---

## Verifying Downloads

**Important**: Before installing, we strongly recommend verifying the integrity of downloaded files using SHA256 checksums. This ensures the files have not been corrupted or tampered with during download.

### Quick Verification

Each release includes:

- **SHA256SUMS**: Combined hash file for all binaries
- **Individual `.sha256` files**: One per binary (e.g., `wtm-v1.0.0-windows-x64.exe.sha256`)

**Verify on Windows (PowerShell):**

```powershell
$hash = (Get-FileHash .\wtm-v1.0.0-windows-x64.exe).Hash
$expected = (Get-Content .\wtm-v1.0.0-windows-x64.exe.sha256).Split(" ")[0]
$hash -eq $expected  # Should return True
```

**Verify on Linux:**

```bash
sha256sum -c wtm-v1.0.0-linux-x64.sha256
# Output: wtm-v1.0.0-linux-x64: OK
```

**Verify on macOS:**

```bash
shasum -a 256 -c wtm-v1.0.0-macos-arm64.sha256
# Output: wtm-v1.0.0-macos-arm64: OK
```

For detailed verification instructions, troubleshooting, and security best practices, see the [Release Verification Guide](release-verification.md).

---

## Installation Instructions

Choose the installation method for your operating system:

### Windows Installation

1. **Download** the `wtm-v{version}-win-x64.zip` file from the releases page

2. **Extract** the archive:
   - Right-click the downloaded file and select "Extract All..."
   - Choose a destination folder (e.g., `C:\Program Files\wtm`)

3. **Add to PATH**:
   - Open System Properties → Environment Variables
   - Under "System variables", find and select "Path"
   - Click "Edit" and add the folder containing `wtm.exe`
   - Click "OK" to save

4. **Verify installation**:

   ```cmd
   wtm --version
   ```

### macOS Installation

1. **Download** the `wtm-v{version}-osx-arm64.tar.gz` file from the releases page

2. **Extract and install**:

   ```bash
   tar -xzf wtm-v*-osx-arm64.tar.gz
   sudo mv wtm /usr/local/bin/
   sudo chmod +x /usr/local/bin/wtm
   ```

3. **Verify installation**:

   ```bash
   wtm --version
   ```

### Linux Installation

1. **Download** the appropriate file for your architecture:
   - x64: `wtm-v{version}-linux-x64.tar.gz`
   - ARM: `wtm-v{version}-linux-arm.tar.gz`

2. **Extract and install**:

   ```bash
   tar -xzf wtm-v*-linux-*.tar.gz
   sudo mv wtm /usr/local/bin/
   sudo chmod +x /usr/local/bin/wtm
   ```

3. **Verify installation**:

   ```bash
   wtm --version
   ```

---

## Troubleshooting

### Command not found

**Symptom**: After installation, running `wtm` shows "command not found" or similar error.

**Solutions**:

- **Windows**: Verify the installation folder is in your PATH. Restart your terminal after modifying PATH.
- **macOS/Linux**: Ensure `/usr/local/bin` is in your PATH. Run `echo $PATH` to verify.
- Try specifying the full path: `/usr/local/bin/wtm --version`
- Restart your terminal or open a new terminal window

### Permission denied

**Symptom**: Error message saying "permission denied" when trying to run `wtm`.

**Solutions**:

- **macOS/Linux**: Make the file executable:

  ```bash
  sudo chmod +x /usr/local/bin/wtm
  ```

- Verify file ownership: `ls -l /usr/local/bin/wtm`
- Try running with sudo: `sudo wtm --version` (not recommended for regular use)

### Git not found

**Symptom**: `wtm` reports that Git is not installed or cannot be found.

**Solutions**:

- Install Git from [git-scm.com](https://git-scm.com)
- **Windows**: Ensure Git is added to PATH during installation
- **macOS**: Install via Homebrew: `brew install git`
- **Linux**: Install via package manager: `sudo apt install git` or `sudo yum install git`
- Verify Git installation: `git --version`

### Additional Help

If you encounter other issues:

1. Check the [GitHub Issues](https://github.com/YOUR_USERNAME/wt/issues) for known problems
2. Create a new issue with details about your environment and error messages
3. Consult the [Quick Start Guide](guides/quickstart.md) for usage examples

---

## Next Steps

Once installed:

1. **Verify your installation** worked correctly: `wtm --version`
2. **Learn verification best practices**: [Release Verification Guide](release-verification.md) (recommended for security-conscious users)
3. **Start using `wtm`**: Check out the [Quick Start Guide](guides/quickstart.md) to learn how to use `wtm` effectively
