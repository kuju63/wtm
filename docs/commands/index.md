# Command Reference

Complete reference documentation for all `wt` commands.

## Overview

`wt` provides a simple command-line interface for managing Git worktrees. All commands follow a consistent pattern and provide helpful error messages.

## Available Commands

### Worktree Management

| Command | Description |
|---------|-------------|
| [create](create.md) | Create a new worktree with a new branch |
| [list](list.md) | List all worktrees with their branch information |

## Quick Reference

### Create a worktree

```bash
# Basic usage
wt create feature-login

# With options
wt create feature-auth --base main --editor vscode
```

### List worktrees

```bash
wt list
```

## Common Options

Most commands support these common options:

**`-h, --help`**

Show help and usage information for the command.

**`-v, --verbose`**

Show detailed diagnostic information (where applicable).

## Global Behavior

### Exit Codes

All commands use consistent exit codes:

| Code | Meaning |
|------|---------|
| 0    | Success |
| 1    | General error |
| 2    | Not a Git repository |
| 10   | Git command failed |
| 99   | Unexpected error |

### Output Formats

Commands that produce structured output support multiple formats:

- **Human** (default): Formatted tables and messages for terminal display
- **JSON**: Machine-readable output for automation and scripting

Example:

```bash
wt create feature-api --output json | jq '.worktree.path'
```

## Getting Help

### Command help

Get help for any command:

```bash
wt --help
wt create --help
wt list --help
```

### Documentation

- [Installation Guide](../installation.md) - Install `wt` on your system
- [Quick Start Guide](../guides/quickstart.md) - Get started in 5 minutes
- [GitHub Repository](https://github.com/kuju63/wt) - Source code and issues

## Examples by Use Case

### Starting a new feature

```bash
wt create feature-user-auth --base main --editor vscode
cd ../wt-feature-user-auth
# Work on your feature...
```

### Bug fix workflow

```bash
wt create bugfix-login-timeout --base main
cd ../wt-bugfix-login-timeout
# Fix the bug...
```

### Code review

```bash
# Create worktree from pull request branch
wt create pr-123 --base origin/pr-123
cd ../wt-pr-123
# Review code...
```

### Checking worktree status

```bash
wt list
```

## Tips and Best Practices

1. **Use descriptive branch names**: `feature-user-auth` is better than `feature1`
2. **Clean up regularly**: Remove worktrees when done with `git worktree remove <path>`
3. **Leverage editor integration**: Use `--editor` to save time opening files
4. **Automate with JSON**: Use `--output json` for scripts and CI/CD pipelines

## See Also

- [Installation](../installation.md)
- [Quick Start](../guides/quickstart.md)
- [Git Worktree Documentation](https://git-scm.com/docs/git-worktree)
