# Getting Started

This guide will help you get started with the `wtm` CLI tool for managing Git worktrees.

## Prerequisites

- .NET 10.0 SDK or later
- Git 2.5 or later (for `git worktree` support)
- A Git repository

## Installation

### Build from Source

```bash
git clone https://github.com/kuju63/wt.git
cd wt
dotnet build
```

### Run the Tool

```bash
dotnet run --project wt.cli -- <command>
```

### Install Globally (Optional)

```bash
dotnet pack
dotnet tool install --global --add-source ./nupkg wt
```

## Basic Workflow

### 1. Create a New Worktree

Create a new worktree for a feature branch:

```bash
wtm create feature-login
```

This command will:

1. Create a new branch `feature-login` from your current branch
2. Create a worktree at `../wt-feature-login`
3. Check out the new branch in the worktree

### 2. List All Worktrees

View all worktrees in your repository:

```bash
wtm list
```

Output example:

```text
┌─────────────────────────────────┬──────────────────┬─────────┐
│ Path                            │ Branch           │ Status  │
├─────────────────────────────────┼──────────────────┼─────────┤
│ /Users/dev/project/wt           │ main             │ active  │
│ /Users/dev/project/wt-feature   │ feature-login    │ active  │
└─────────────────────────────────┴──────────────────┴─────────┘
```

The list shows:

- **Path**: The filesystem path of the worktree
- **Branch**: The checked-out branch (or commit hash for detached HEAD)
- **Status**: `active` if the worktree exists, `missing` if it's registered but not on disk

### 3. Navigate to Your Worktree

```bash
cd ../wt-feature-login
# Start working on your feature
```

### 4. Work with Multiple Features

You can create multiple worktrees to work on different features simultaneously:

```bash
wtm create feature-auth
wtm create feature-ui
wtm create bugfix-123 --base main
wtm list
```

Each worktree is independent, allowing you to switch between features without stashing changes.

## Advanced Usage

### Custom Worktree Path

```bash
wtm create feature-api --path ~/projects/myapp-api
```

### Auto-Launch Editor

```bash
wtm create feature-ui --editor vscode
```

Supported editors:

- `vscode` - Visual Studio Code
- `vim` - Vim
- `emacs` - Emacs
- `nano` - Nano
- `idea` - IntelliJ IDEA

### JSON Output for Automation

```bash
wtm create feature-test --output json
wtm list --format json  # Coming in future release
```

## Common Scenarios

### Scenario 1: Working on Multiple Features

```bash
# Main repository for code reviews
cd ~/projects/myapp

# Feature 1: User authentication
wtm create feature-auth
cd ../wt-feature-auth
# ... work on authentication ...

# Feature 2: New UI while auth is in progress
cd ~/projects/myapp
wtm create feature-ui
cd ../wt-feature-ui
# ... work on UI ...

# View all active worktrees
cd ~/projects/myapp
wtm list
```

### Scenario 2: Hotfix on Production

```bash
# Create hotfix from main branch
wtm create hotfix-critical-bug --base main

# Work on the fix in isolation
cd ../wt-hotfix-critical-bug
# ... fix the bug ...
# ... commit and push ...
```

### Scenario 3: Review Pull Requests

```bash
# Create worktree for PR review
wtm create review-pr-123 --base main
cd ../wt-review-pr-123

# Fetch and checkout the PR branch
git fetch origin pull/123/head:pr-123
git checkout pr-123

# Review the code, test, etc.
```

## Tips and Best Practices

1. **Keep worktrees organized**: Use the default naming convention `wt-<branch>` for consistency
2. **List regularly**: Use `wtm list` to see all your worktrees and their status
3. **Clean up**: Remove worktrees when done with `git worktree remove <path>`
4. **Missing worktrees**: If `wtm list` shows a worktree as `missing`, remove it with `git worktree prune`
5. **Detached HEAD**: Be careful with detached HEAD state - create a branch if you need to keep changes

## Next Steps

- See [Command Reference](../README.md#command-reference) for detailed command options
- Learn about [Architecture Decision Records](adr/) for design decisions
- Read the [API Documentation](../api/) for programmatic usage
