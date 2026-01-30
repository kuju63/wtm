# Data Model: GitHub Pages Documentation Publishing

**Feature**: 001-github-pages-docs  
**Date**: 2026-01-15  
**Phase**: Phase 1 - Design

This document defines the key entities, their attributes, relationships, and validation rules for the GitHub Pages documentation publishing feature.

## Entities

### 1. Documentation Version

Represents a specific minor version of the documentation (e.g., v1.0, v1.1, v2.0).

**Attributes**:

| Attribute | Type | Required | Description | Validation Rules |
|-----------|------|----------|-------------|-----------------|
| `label` | string | Yes | Display name in version switcher (e.g., "v1.0 (latest)") | Must match pattern: `v\d+\.\d+( \(latest\))?` |
| `path` | string | Yes | URL path segment for this version | Must match pattern: `v\d+\.\d+` |
| `released` | datetime | Yes | ISO 8601 timestamp of release | Must be valid ISO 8601 format |
| `isLatest` | boolean | Yes | Whether this is the latest version | Only one version can have `isLatest: true` |

**Relationships**:

- Has many: Documentation Pages
- Has many: Command Documentation Pages
- Has one: API Reference Section

**State Transitions**:

```
[New Release] → Version Created (isLatest=true)
                ↓
                All other versions: isLatest → false
                ↓
                Version Published to GitHub Pages
```

**Business Rules**:

- Exactly one version must be marked as `isLatest: true` at all times
- Version path must be unique across all versions
- Versions are immutable once published (no content changes to deployed versions)
- Minor version level only (patch versions share documentation: v1.0.0, v1.0.1, v1.0.2 → all use v1.0)

**Example**:

```json
{
  "label": "v1.2 (latest)",
  "path": "v1.2",
  "released": "2026-01-15T10:30:00Z",
  "isLatest": true
}
```

---

### 2. Version Manifest

Aggregates all available documentation versions for the version switcher UI.

**Attributes**:

| Attribute | Type | Required | Description | Validation Rules |
|-----------|------|----------|-------------|-----------------|
| `versions` | array | Yes | List of Documentation Version objects | Must contain at least 1 version |

**Relationships**:

- Contains many: Documentation Versions (aggregation)

**State Transitions**:

```
[First Release] → Empty manifest → First version added
                                    ↓
[New Release]  → Fetch existing → Update with new version → Deploy
```

**Business Rules**:

- Stored at GitHub Pages root: `/version-manifest.json`
- Copied to each version directory for redundancy
- Automatically updated on every release
- Sorted by release date (newest first)
- Idempotent updates (re-running same version updates timestamp, doesn't duplicate)

**Example**:

```json
{
  "versions": [
    {
      "label": "v1.1 (latest)",
      "path": "v1.1",
      "released": "2026-01-15T10:30:00Z",
      "isLatest": true
    },
    {
      "label": "v1.0",
      "path": "v1.0",
      "released": "2026-01-01T14:20:00Z",
      "isLatest": false
    }
  ]
}
```

---

### 3. Command Documentation Page

Represents documentation for a single CLI command.

**Attributes**:

| Attribute | Type | Required | Description | Validation Rules |
|-----------|------|----------|-------------|-----------------|
| `commandName` | string | Yes | Name of the command (e.g., "create") | Must match existing CLI command |
| `description` | string | Yes | Command purpose and overview | Non-empty, extracted from System.CommandLine |
| `usage` | string | Yes | Syntax pattern | Format: `wt {commandName} [options]` |
| `options` | array | No | List of command options | Each option must have aliases, description |
| `examples` | array | Yes | Usage examples | At least 1 example required |
| `filePath` | string | Yes | Markdown file location | Format: `docs/commands/{commandName}.md` |

**Relationships**:

- Belongs to: Documentation Version
- Generated from: CLI Command Definition (wt.cli RootCommand)

**State Transitions**:

```
[CLI Code Change] → DocGenerator extracts metadata → Markdown generated → DocFX builds → HTML deployed
```

**Business Rules**:

- Auto-generated from System.CommandLine definitions (no manual editing)
- One markdown file per command
- Must exist for every command in wt.cli RootCommand.Subcommands
- Regenerated on every documentation build
- Examples are manually curated in DocGenerator code

**Example**:

```markdown
# wt create

Create a new Git worktree with optional editor integration.

## Usage
```bash
wt create [options] <branch-name>
```

## Options

### `--path`, `-p`

Custom path for the worktree directory.
**Type:** `string`

### `--editor`, `-e`

Editor to open after creation (vscode, vim, emacs).
**Type:** `string`

## Examples

### Create worktree with default path

```bash
wt create feature-login
```

### Create with custom path

```bash
wt create feature-login --path /tmp/wt-login
```

```

---

### 4. API Reference Section

Generated documentation for all public APIs in wt.cli project.

**Attributes**:
| Attribute | Type | Required | Description | Validation Rules |
|-----------|------|----------|-------------|-----------------|
| `namespace` | string | Yes | .NET namespace (e.g., "Kuju63.WorkTree.CommandLine") | Valid .NET namespace |
| `classes` | array | Yes | Public classes with XML docs | Auto-generated by DocFX |
| `interfaces` | array | Yes | Public interfaces with XML docs | Auto-generated by DocFX |
| `outputPath` | string | Yes | Generated files location | `api/` directory |

**Relationships**:
- Belongs to: Documentation Version
- Generated from: wt.cli.csproj XML documentation

**State Transitions**:
```

[Build wt.cli] → Generate XML docs → DocFX metadata extraction → API HTML generation → Deployment

```

**Business Rules**:
- 100% auto-generated from XML documentation comments (per SC-007)
- Requires `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in .csproj
- Only public types and members are documented
- Cross-references to .NET framework via xref configuration
- Must build successfully for Release configuration

**Example Structure**:
```

api/
├── Kuju63.WorkTree.CommandLine.html
├── Kuju63.WorkTree.CommandLine.Commands.html
├── Kuju63.WorkTree.CommandLine.Commands.CreateCommand.html
└── toc.yml

```

---

### 5. Installation Guide

Platform-specific instructions for installing the self-contained native binary.

**Attributes**:
| Attribute | Type | Required | Description | Validation Rules |
|-----------|------|----------|-------------|-----------------|
| `platforms` | array | Yes | Supported platforms (Windows, macOS, Linux) | Must cover: win-x64, linux-x64, linux-arm, osx-arm64 |
| `downloadLinks` | array | Yes | Links to GitHub release assets per platform | Must reference actual release artifacts |
| `installationSteps` | array | Yes | Step-by-step instructions per platform | At least 2 steps per platform (download + install) |
| `verificationCommand` | string | Yes | Command to verify installation | Must be: `wt --version` or `wt --help` |
| `filePath` | string | Yes | Markdown file location | `docs/installation.md` |

**Relationships**:
- Belongs to: Documentation Version
- Referenced by: Homepage (primary entry point per SC-001)

**State Transitions**:
```

[Manual Creation] → Version-specific review → Committed to repository → Built by DocFX

```

**Business Rules**:
- **NO .NET SDK required** - binary is self-contained native application
- **NO Git required for installation** - only needed for actual worktree operations
- Manually authored markdown file
- Must be complete before v1.0 release
- Updated when installation process changes
- Platform-specific sections clearly marked with OS names
- Must include troubleshooting section for common issues

**Example Sections**:
```markdown
# Installation

## System Requirements
- **Windows**: Windows 10 or later (x64)
- **macOS**: macOS 11 or later (ARM64/Apple Silicon)
- **Linux**: x64 or ARM architecture

**Note**: No .NET SDK or Git installation required for the `wt` tool itself. Git is only needed when you actually use worktrees.

## Download

Download the appropriate binary for your platform from the [latest release](https://github.com/kuju63/wt/releases/latest):
- **Windows**: `wt-win-x64.zip`
- **macOS**: `wt-osx-arm64.tar.gz`
- **Linux (x64)**: `wt-linux-x64.tar.gz`
- **Linux (ARM)**: `wt-linux-arm.tar.gz`

## Windows Installation

1. Download `wt-win-x64.zip` from releases
2. Extract to `C:\Program Files\wt\` (or any preferred location)
3. Add to PATH:
   ```powershell
   setx PATH "%PATH%;C:\Program Files\wt"
   ```

1. Restart terminal
2. Verify installation:

   ```powershell
   wt --version
   ```

## macOS Installation

1. Download `wt-osx-arm64.tar.gz`
2. Extract and move to `/usr/local/bin`:

   ```bash
   tar -xzf wt-osx-arm64.tar.gz
   sudo mv wt /usr/local/bin/
   sudo chmod +x /usr/local/bin/wt
   ```

3. Verify installation:

   ```bash
   wt --version
   ```

## Linux Installation

1. Download the appropriate archive for your architecture
2. Extract and install:

   ```bash
   tar -xzf wt-linux-x64.tar.gz
   sudo mv wt /usr/local/bin/
   sudo chmod +x /usr/local/bin/wt
   ```

3. Verify installation:

   ```bash
   wt --version
   ```

## Troubleshooting

### Command not found

- Ensure the wt binary is in your PATH
- On Windows, restart your terminal after updating PATH

### Permission denied (macOS/Linux)

- Run: `sudo chmod +x /usr/local/bin/wt`

### Git not found

- Install Git separately: `git --version` to check
- Only required when actually using worktree commands

```

---

### 6. Contribution Guide

Guidelines for contributing code, documentation, or bug reports to the project.

**Attributes**:
| Attribute | Type | Required | Description | Validation Rules |
|-----------|------|----------|-------------|-----------------|
| `developmentSetup` | section | Yes | Environment setup instructions | Must include .NET SDK requirement (for development only) |
| `codingStandards` | section | Yes | Reference to constitution principles | Must link to constitution.md |
| `pullRequestProcess` | section | Yes | PR submission guidelines | Step-by-step process |
| `issueTemplates` | section | Yes | Bug report and feature request formats | Link to GitHub issue templates |
| `filePath` | string | Yes | Markdown file location | `docs/contributing.md` |

**Relationships**:
- Belongs to: Documentation Version
- References: Project Constitution (.specify/memory/constitution.md)

**State Transitions**:
```

[Manual Creation] → Constitution alignment check → Committed → Built by DocFX

```

**Business Rules**:
- Must align with Constitution principles (especially III, V, VI)
- Must reference TDD workflow (Constitution VI)
- Must include code of conduct link
- **Clearly distinguish**: User installation (no .NET) vs. Developer setup (.NET SDK required)
- Language: Primarily Japanese with English technical terms

**Example Sections**:
```markdown
# Contributing to wt

## Development Environment Setup

**Note**: Unlike user installation, development requires the .NET SDK.

### Prerequisites for Development
- .NET 10.0 SDK or later
- Git 2.5 or later
- Your preferred editor (VS Code, Rider, Visual Studio)

### Setup Steps
1. Clone repository: `git clone https://github.com/kuju63/wt.git`
2. Restore dependencies: `dotnet restore`
3. Build: `dotnet build`
4. Run tests: `dotnet test`
5. Run locally: `dotnet run --project wt.cli -- create test-branch`

## Coding Standards
This project follows the principles defined in our [Constitution](../.specify/memory/constitution.md):
- **TDD (Test-Driven Development)** required for all features
- **Minimal dependencies** - justify each new package
- **Clean and secure code** - pass all static analysis

## Pull Request Process
1. Create feature branch from main
2. Write tests first (TDD)
3. Implement feature
4. Ensure all tests pass: `dotnet test`
5. Update documentation if needed
6. Submit PR with clear description

## Reporting Issues
Use GitHub issue templates:
- Bug Report: Include `wt --version` output and repro steps
- Feature Request: Describe use case and proposed solution
```

---

## Entity Relationship Diagram

```
┌─────────────────────────┐
│   Version Manifest      │
│  (version-manifest.json)│
└────────┬────────────────┘
         │ contains *
         ▼
┌─────────────────────────┐
│  Documentation Version  │ ◄─┐
│  (v1.0/, v1.1/, ...)    │   │
└────────┬────────────────┘   │
         │ has *              │ belongs to
         ├────────────────────┼─────────────┐
         │                    │             │
         ▼                    │             │
┌─────────────────────────┐  │             │
│ Command Documentation   │  │             │
│   (commands/*.md)       │──┘             │
└─────────────────────────┘                │
                                           │
         ┌─────────────────────────────────┤
         │                                 │
         ▼                                 │
┌─────────────────────────┐                │
│   API Reference         │                │
│     (api/*)             │────────────────┘
└─────────────────────────┘                
                                           
         ┌─────────────────────────────────┤
         │                                 │
         ▼                                 │
┌─────────────────────────┐                │
│  Installation Guide     │                │
│ (docs/installation.md)  │────────────────┘
└─────────────────────────┘                
                                           
         ┌─────────────────────────────────┘
         │
         ▼
┌─────────────────────────┐
│  Contribution Guide     │
│ (docs/contributing.md)  │
└─────────────────────────┘
```

## Data Flow

### Documentation Build Flow

```
1. Release Published (GitHub Event)
   ↓
2. Extract Version (v1.2.3 → v1.2)
   ↓
3. Generate Command Docs (Tools/DocGenerator)
   ↓
4. Build API Docs (wt.cli XML → DocFX metadata)
   ↓
5. Build Static Site (DocFX → HTML/CSS/JS)
   ↓
6. Fetch Existing Manifest (gh-pages branch)
   ↓
7. Update Manifest (Python script)
   ↓
8. Validate Links (LinkChecker)
   ↓
9. Deploy to GitHub Pages (v1.2/ directory + manifest)
```

### Version Switcher Flow

```
1. User loads documentation page
   ↓
2. JavaScript fetches /version-manifest.json
   ↓
3. Populate dropdown with versions
   ↓
4. Detect current version from URL (/v1.2/...)
   ↓
5. Pre-select in dropdown
   ↓
6. User selects different version
   ↓
7. Extract path without version (e.g., /installation.html)
   ↓
8. Navigate to /v1.1/installation.html
```

## Validation Rules Summary

| Entity | Key Validation | Error Handling |
|--------|----------------|----------------|
| Documentation Version | Only one `isLatest: true` | Python script enforces, removes from others |
| Version Manifest | At least 1 version | Build fails if empty after update |
| Command Documentation | Must exist for all CLI commands | DocGenerator throws if command missing |
| API Reference | XML docs must build successfully | `dotnet build` fails → deployment stops |
| Installation Guide | No .NET/Git prerequisites mentioned for users | Manual PR review verification |
| Contribution Guide | .NET SDK required for developers only | Manual PR review verification |

## Storage and Persistence

| Entity | Storage Location | Format | Persistence Strategy |
|--------|------------------|--------|---------------------|
| Version Manifest | `/version-manifest.json` on gh-pages | JSON | Updated on every release, versioned in git history |
| Documentation Version | `/v{major}.{minor}/` directory | Static HTML/CSS/JS | Immutable once published, kept for 2+ years (SC-006) |
| Command Docs | `docs/commands/*.md` (source) | Markdown → HTML | Auto-generated, committed to main branch |
| API Reference | `api/` directory (generated) | HTML | Auto-generated, not committed (build artifact) |
| Installation Guide | `docs/installation.md` (source) | Markdown → HTML | Manually authored, committed to main branch |
| Contribution Guide | `docs/contributing.md` (source) | Markdown → HTML | Manually authored, committed to main branch |

## Platform-Specific Binary Mapping

| Platform | Runtime Identifier | Release Asset Name | Installation Path |
|----------|-------------------|--------------------|-------------------|
| Windows x64 | win-x64 | wt-win-x64.zip | C:\Program Files\wt\ (or user choice) |
| macOS ARM64 | osx-arm64 | wt-osx-arm64.tar.gz | /usr/local/bin/ |
| Linux x64 | linux-x64 | wt-linux-x64.tar.gz | /usr/local/bin/ |
| Linux ARM | linux-arm | wt-linux-arm.tar.gz | /usr/local/bin/ |

**Key Point**: All binaries are self-contained with `PublishSingleFile=true` and `SelfContained=true`. No runtime dependencies required for end users.

## Future Considerations

### Automatic Update Mechanism (Not in Current Scope)

- Self-update command: `wt update`
- Check for new versions on startup?
- Auto-download and replace binary?

### Package Manager Distribution (Not in Current Scope)

- Windows: `winget install wt`
- macOS: `brew install wt`
- Linux: `apt install wt` / `snap install wt`

### Deprecation Strategy (Not in Current Scope)

- When to remove old versions from GitHub Pages?
- Archive location for deprecated versions?
- User notification for deprecated versions?

---

**Next Steps**: Create contracts/ directory with schema definitions for GitHub Actions workflow and DocFX configuration.
