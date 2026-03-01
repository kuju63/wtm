# wtm - Git Worktree Manager

A modern CLI tool to simplify Git worktree management. Create worktrees with a single command and optionally launch your favorite editor.

## Features

- ✨ **Simple worktree creation**: `wtm create feature-branch`
- 📋 **List all worktrees**: `wtm list` - Display all worktrees with their branches
- 🎯 **Smart defaults**: Automatically creates worktrees in `../wt-<branch>` directory
- 🚀 **Editor integration**: Auto-launch VS Code, Vim, Emacs, or IntelliJ IDEA
- 🛠️ **Custom paths**: Specify where to create worktrees
- 📊 **Table format**: Human-readable table output with Unicode box-drawing characters
- 📋 **Multiple output formats**: Human-readable or JSON for automation
- ✅ **Cross-platform**: Works on macOS, Linux, and Windows

## Installation

### Prerequisites

- .NET 10.0 SDK or later
- Git 2.5 or later

### Quick Install (Recommended)

**macOS / Linux:**

```bash
curl -fsSL https://kuju63.github.io/wt/install.sh | sh
```

**Windows (PowerShell):**

```powershell
irm https://kuju63.github.io/wt/install.ps1 | iex
```

The install script automatically:

- Detects your platform and architecture
- Downloads the latest version
- Verifies the SHA256 checksum
- Installs to `~/.local/bin` (Unix) or `%LOCALAPPDATA%\Programs\wtm` (Windows)

### Download from Releases

Download the latest release for your platform from the [GitHub Releases](https://github.com/kuju63/wt/releases) page.

Each release includes:

- Pre-built binaries for Windows, Linux (x64, ARM), and macOS (ARM64)
- **SHA256 checksums** for verifying download integrity
- Individual `.sha256` files for each binary
- Combined `SHA256SUMS` file for batch verification

**Verify your download** (recommended):

```bash
# Linux/macOS
sha256sum -c wtm-v1.0.0-linux-x64.sha256

# macOS (alternative)
shasum -a 256 -c wtm-v1.0.0-macos-arm64.sha256

# Windows PowerShell
$hash = (Get-FileHash .\wtm-v1.0.0-windows-x64.exe).Hash
$expected = (Get-Content .\wtm-v1.0.0-windows-x64.exe.sha256).Split(" ")[0]
$hash -eq $expected  # Should return True
```

For detailed installation and verification instructions, see the [Installation Guide](https://kuju63.github.io/wt/latest/installation.html).

### Build from source

```bash
git clone https://github.com/kuju63/wt.git
cd wt
dotnet build
dotnet run --project wt.cli -- create --help
```

### Install globally (optional)

```bash
dotnet pack
dotnet tool install --global --add-source ./nupkg wt
```

## Quick Start

### Basic Usage

Create a new worktree with a new branch:

```bash
wtm create feature-login
```

This will:

1. Create a new branch named `feature-login` from your current branch
2. Create a worktree at `../wt-feature-login`
3. Check out the new branch in the worktree

### Specify Base Branch

Create a branch from a specific base branch:

```bash
wtm create feature-login --base main
```

### Custom Path

Create a worktree at a custom location:

```bash
wtm create feature-login --path ~/projects/myapp-feature-login
```

### Auto-Launch Editor

Create a worktree and automatically open it in your editor:

```bash
wtm create feature-login --editor vscode
```

Supported editors:

- `vscode` - Visual Studio Code
- `vim` - Vim
- `emacs` - Emacs
- `nano` - Nano
- `idea` - IntelliJ IDEA

### JSON Output

For automation and scripting:

```bash
wtm create feature-login --output json
```

### Verbose Mode

Show detailed diagnostic information:

```bash
wtm create feature-login --verbose
```

## Command Reference

### `wtm list`

List all worktrees with their branch information.

Display all Git worktrees in the repository with their paths, checked-out branches, and status in a table format. Missing worktrees (registered but not existing on disk) are highlighted with warnings.

**Output:**

```shell
┌─────────────────────────────────┬──────────────────┬─────────┐
│ Path                            │ Branch           │ Status  │
├─────────────────────────────────┼──────────────────┼─────────┤
│ /Users/dev/project/wt           │ main             │ active  │
│ /Users/dev/project/wt-feature   │ feature-branch   │ active  │
└─────────────────────────────────┴──────────────────┴─────────┘
```

**Exit Codes:**

- `0` - Success
- `1` - Git not found
- `2` - Not a Git repository
- `10` - Git command failed
- `99` - Unexpected error

### `wtm create <branch> [options]`

Create a new worktree with a new branch.

**Arguments:**

- `<branch>` - Name of the branch to create (required)

**Options:**

- `-b, --base <base>` - Base branch to branch from (default: current branch)
- `-p, --path <path>` - Path where the worktree will be created (default: `../wt-<branch>`)
- `-e, --editor <type>` - Editor to launch after creating worktree (choices: vscode, vim, emacs, nano, idea)
- `--output <format>` - Output format: human or json (default: human)
- `-v, --verbose` - Show detailed diagnostic information
- `-h, --help` - Show help and usage information

## Examples

### Example 1: Simple Feature Branch

```bash
wtm create feature-auth
```

Output:

```shell
✓ Created branch: feature-auth
✓ Created worktree: /Users/username/projects/wt-feature-auth
✓ Checked out: feature-auth
```

### Example 2: Bug Fix with Custom Path

```bash
wtm create bugfix-123 --base main --path ~/bugfixes/fix-123
```

### Example 3: With Editor Launch

```bash
wtm create feature-ui --editor vscode
```

This will create the worktree and automatically open VS Code in the new worktree directory.

### Example 4: Automation with JSON

```bash
wtm create feature-api --output json | jq '.worktree.path'
```

Output:

```json
{
  "success": true,
  "worktree": {
    "path": "/Users/username/projects/wt-feature-api",
    "branch": "feature-api",
    "baseBranch": "main",
    "createdAt": "2026-01-03T12:34:56Z"
  }
}
```

## Troubleshooting

### Error: Not a git repository

**Solution**: Run `wtm` from within a Git repository directory.

```bash
cd /path/to/your/git/repo
wtm create my-branch
```

### Error: Branch already exists

**Solution**: Use a different branch name or delete the existing branch first.

```bash
git branch -d existing-branch
wtm create existing-branch
```

### Error: Path already exists

**Solution**: Specify a different path or remove the existing directory.

```bash
wtm create my-branch --path ~/different/path
```

### Error: Editor not found

**Solution**: Ensure the editor is installed and available in your PATH.

```bash
# For VS Code on macOS
code --version

# For Vim
vim --version
```

## Supply Chain Transparency

### SBOM (Software Bill of Materials)

Every release of `wtm` includes a complete Software Bill of Materials (SBOM) that provides transparency about all dependencies used in the software:

- **📄 Format**: SPDX 2.3 (ISO/IEC 5962:2021 compliant)
- **🔍 Transparency**: Complete list of all direct and transitive dependencies
- **🛡️ Security**: Automatic vulnerability tracking via GitHub Dependabot
- **⚖️ Compliance**: License information for all components
- **📦 Availability**: Attached to every GitHub release

#### Download SBOM

```bash
# Download SBOM from latest release
VERSION=$(curl -s https://api.github.com/repos/kuju63/wt/releases/latest | grep '"tag_name":' | sed -E 's/.*"([^"]+)".*/\1/')
curl -L https://github.com/kuju63/wt/releases/download/${VERSION}/wtm-${VERSION}-sbom.spdx.json \
  -o wtm-sbom.spdx.json

# Or download a specific version
curl -L https://github.com/kuju63/wt/releases/download/v1.0.0/wtm-v1.0.0-sbom.spdx.json \
  -o wtm-sbom.spdx.json
```

#### Verify SBOM

```bash
# Install SPDX validator
npm install -g @spdx/spdx-validator

# Validate SBOM format
spdx-validator wtm-sbom.spdx.json
```

#### View Dependencies

```bash
# List all dependencies with versions
jq -r '.packages[] | "\(.name)@\(.versionInfo)"' wtm-sbom.spdx.json

# Check license information
jq -r '.packages[] | "\(.name): \(.licenseDeclared)"' wtm-sbom.spdx.json
```

**Learn more**: See the [SBOM Usage Guide](docs/guides/sbom-usage.md) for detailed information.

## Development

### Project Structure

```tree
wt/
├── wt.cli/              # CLI application
│   ├── Commands/        # Command implementations
│   ├── Models/          # Data models
│   ├── Services/        # Business logic
│   └── Utils/           # Helper utilities
├── wt.tests/            # Unit and integration tests
└── specs/               # Feature specifications
```

### Running Tests

```bash
dotnet test
```

### Test Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Contributing

Contributions are welcome! Please follow the [development guidelines](specs/001-create-worktree/quickstart.md).

## License

[MIT License](./LICENSE)

## Acknowledgments

Built with:

- [System.CommandLine](https://github.com/dotnet/command-line-api) - Modern command-line parsing
- [xUnit](https://xunit.net/) - Testing framework
- [FluentAssertions](https://fluentassertions.com/) - Fluent assertion library
