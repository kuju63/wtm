# Installation Guide

This guide will help you install the `wt` command-line tool for Git worktree management.

## System Requirements

Before installing `wt`, ensure your system meets the following requirements:

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
| Windows  | x64         | `wt-v{version}-win-x64.zip` |
| macOS    | ARM64       | `wt-v{version}-osx-arm64.tar.gz` |
| Linux    | x64         | `wt-v{version}-linux-x64.tar.gz` |
| Linux    | ARM         | `wt-v{version}-linux-arm.tar.gz` |

Example: For version 0.0.3, the Linux x64 file would be `wt-v0.0.3-linux-x64.tar.gz`

---

## Installation Instructions

Choose the installation method for your operating system:

### Windows Installation

1. **Download** the `wt-v{version}-win-x64.zip` file from the releases page

2. **Extract** the archive:
   - Right-click the downloaded file and select "Extract All..."
   - Choose a destination folder (e.g., `C:\Program Files\wt`)

3. **Add to PATH**:
   - Open System Properties → Environment Variables
   - Under "System variables", find and select "Path"
   - Click "Edit" and add the folder containing `wt.exe`
   - Click "OK" to save

4. **Verify installation**:

   ```cmd
   wt --version
   ```

### macOS Installation

1. **Download** the `wt-v{version}-osx-arm64.tar.gz` file from the releases page

2. **Extract and install**:

   ```bash
   tar -xzf wt-v*-osx-arm64.tar.gz
   sudo mv wt /usr/local/bin/
   sudo chmod +x /usr/local/bin/wt
   ```

3. **Verify installation**:

   ```bash
   wt --version
   ```

### Linux Installation

1. **Download** the appropriate file for your architecture:
   - x64: `wt-v{version}-linux-x64.tar.gz`
   - ARM: `wt-v{version}-linux-arm.tar.gz`

2. **Extract and install**:

   ```bash
   tar -xzf wt-v*-linux-*.tar.gz
   sudo mv wt /usr/local/bin/
   sudo chmod +x /usr/local/bin/wt
   ```

3. **Verify installation**:

   ```bash
   wt --version
   ```

---

## Troubleshooting

### Command not found

**Symptom**: After installation, running `wt` shows "command not found" or similar error.

**Solutions**:

- **Windows**: Verify the installation folder is in your PATH. Restart your terminal after modifying PATH.
- **macOS/Linux**: Ensure `/usr/local/bin` is in your PATH. Run `echo $PATH` to verify.
- Try specifying the full path: `/usr/local/bin/wt --version`
- Restart your terminal or open a new terminal window

### Permission denied

**Symptom**: Error message saying "permission denied" when trying to run `wt`.

**Solutions**:

- **macOS/Linux**: Make the file executable:

  ```bash
  sudo chmod +x /usr/local/bin/wt
  ```

- Verify file ownership: `ls -l /usr/local/bin/wt`
- Try running with sudo: `sudo wt --version` (not recommended for regular use)

### Git not found

**Symptom**: `wt` reports that Git is not installed or cannot be found.

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

Once installed, check out the [Quick Start Guide](guides/quickstart.md) to learn how to use `wt` effectively.
