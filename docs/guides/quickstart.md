# Quick Start Guide

Get started with `wt` in just a few minutes. This guide walks you through creating your first worktree and covers basic commands.

## Prerequisites

- Git installed and configured
- `wt` installed (see [Installation Guide](../installation.md))
- An existing Git repository

---

## Your First Worktree

### Step 1: Navigate to Your Repository

Open a terminal and navigate to any Git repository:

```bash
cd /path/to/your/project
```

### Step 2: Create a Worktree

Create a new worktree for a feature branch:

```bash
wt create feature-new-feature
```

This command will:

- Create a new branch called `feature-new-feature`
- Set up a worktree at `../wt-feature-new-feature`
- Automatically open the worktree in your default editor (if configured)

**Output:**

```shell
✓ Created branch: feature-new-feature
✓ Created worktree: /Users/username/projects/wt-feature-new-feature
✓ Checked out: feature-new-feature
```

### Step 3: List Your Worktrees

View all worktrees in your repository:

```bash
wt list
```

**Output:**

```shell
┌─────────────────────────────────────────────┬──────────────────────┬─────────┐
│ Path                                        │ Branch               │ Status  │
├─────────────────────────────────────────────┼──────────────────────┼─────────┤
│ /Users/username/projects/wt                 │ main                 │ active  │
│ /Users/username/projects/wt-feature-new     │ feature-new-feature  │ active  │
└─────────────────────────────────────────────┴──────────────────────┴─────────┘
```

---

## Basic Commands

### Create Worktree with Custom Path

Specify a custom location for your worktree:

```bash
wt create feature-login --path ~/projects/myapp-login
```

### Create Worktree from Specific Base Branch

Create a branch from a specific base (e.g., `main`):

```bash
wt create feature-login --base main
```

### Auto-Launch Editor

Create a worktree and automatically open it in your editor:

```bash
wt create feature-login --editor vscode
```

Supported editors: `vscode`, `vim`, `emacs`, `nano`, `idea`

---

## Common Workflows

### Feature Development Workflow

1. **Start a new feature**:

   ```bash
   wt create feature-user-auth
   cd ../wt-feature-user-auth
   ```

2. **Work on your feature**:

   ```bash
   # Make changes, commit as usual
   git add .
   git commit -m "Add user authentication"
   ```

3. **Switch back to main**:

   ```bash
   cd ../wt  # or your main worktree path
   ```

4. **Clean up when done**:

   ```bash
   git worktree remove ../wt-feature-user-auth
   ```

### Parallel Development

Work on multiple features simultaneously:

```bash
# Terminal 1: Work on feature A
wt create feature-a
cd ../wt-feature-a

# Terminal 2: Work on feature B  
wt create feature-b
cd ../wt-feature-b

# Terminal 3: Review code in main
cd /path/to/wt
```

Each worktree is independent - changes in one don't affect others.

---

## Editor Integration

`wt` can automatically open new worktrees in your preferred editor.

### Supported Editors

- **Visual Studio Code** (`vscode`)
- **Vim** (`vim`)
- **Emacs** (`emacs`)
- **Nano** (`nano`)
- **IntelliJ IDEA** (`idea`)

### Example

```bash
wt create feature-ui --editor vscode
```

This will create the worktree and automatically launch VS Code in that directory.

---

## Tips and Best Practices

### 1. Naming Conventions

Use descriptive branch names with prefixes:

```bash
wt create feature-user-authentication
wt create bugfix-login-timeout
wt create hotfix-security-patch
```

### 2. Default Directory Structure

Worktrees are created as sibling directories with `wt-` prefix by default:

```
projects/
├── wt/                    (main worktree)
├── wt-feature-a/
├── wt-feature-b/
└── wt-bugfix-login/
```

### 3. Clean Up Regularly

Remove worktrees when you're done:

```bash
git worktree remove ../wt-feature-name
```

Or use Git's built-in prune command:

```bash
git worktree prune
```

### 4. Use JSON Output for Automation

For scripting and automation:

```bash
wt create feature-api --output json | jq '.worktree.path'
```

---

## Next Steps

- **[Command Reference](../commands/index.md)**: Detailed documentation for all commands
- **[Installation Guide](../installation.md)**: Advanced installation options
- **[Contributing](../contributing.md)**: Help improve `wt`

---

## Getting Help

- Run `wt --help` for command help
- Run `wt create --help` for create command options
- Visit the [GitHub repository](https://github.com/kuju63/wt)
- Report issues on [GitHub Issues](https://github.com/kuju63/wt/issues)
