---
_layout: landing
---

# wtm - Git Worktree Manager

A modern CLI tool to simplify Git worktree management. Create worktrees with a single command and optionally launch your favorite editor.

## ✨ Features

- **Simple worktree creation**: `wtm create feature-branch`
- **List all worktrees**: Display all worktrees with their branches in a beautiful table
- **Smart defaults**: Automatically creates worktrees in `../wt-<branch>` directory
- **Editor integration**: Auto-launch VS Code, Vim, Emacs, or IntelliJ IDEA
- **Custom paths**: Specify where to create worktrees
- **Multiple output formats**: Human-readable or JSON for automation
- **Cross-platform**: Works on macOS, Linux, and Windows

---

## 🚀 Quick Start

### Installation

Get started in minutes with a single command:

**macOS / Linux:**

```bash
curl -fsSL https://kuju63.github.io/wt/install.sh | sh
```

**Windows (PowerShell):**

```powershell
irm https://kuju63.github.io/wt/install.ps1 | iex
```

**[📥 Installation Guide](docs/installation.md)**

### Your First Worktree

```bash
# Create a new feature branch worktree
wtm create feature-login

# List all worktrees
wtm list
```

**[📖 Quick Start Guide](docs/guides/quickstart.md)**

---

## 📚 Documentation

### For Users

- **[Installation Guide](docs/installation.md)** - Install `wtm` on your system
- **[Quick Start Guide](docs/guides/quickstart.md)** - Get up and running in 5 minutes
- **[Command Reference](docs/commands/index.md)** - Detailed command documentation with examples

> 💡 **Quick Search**: Use the search bar above to find specific commands, options, or topics instantly.

### For Developers

- **[API Reference](api/index.md)** - Complete API documentation
- **[Contributing Guide](docs/contributing.md)** - Help improve `wtm`

---

## 💡 Why Use Worktrees?

Git worktrees allow you to have multiple working directories from a single repository:

- **Work on multiple features simultaneously** without switching branches
- **Review pull requests** without stashing your current work
- **Run different versions** side-by-side for testing
- **Keep your workspace organized** with parallel directories

`wtm` makes managing worktrees simple and intuitive.

---

## 🎯 Example Workflow

```bash
# Start working on a new feature
wtm create feature-user-auth --editor vscode

# In another terminal, fix a bug
wtm create bugfix-login-timeout --base main

# Check all your worktrees
wtm list
```

Output:

```shell
┌─────────────────────────────────┬──────────────────────┬─────────┐
│ Path                            │ Branch               │ Status  │
├─────────────────────────────────┼──────────────────────┼─────────┤
│ /projects/wt                    │ main                 │ active  │
│ /projects/wt-feature-user-auth  │ feature-user-auth    │ active  │
│ /projects/wt-bugfix-login       │ bugfix-login-timeout │ active  │
└─────────────────────────────────┴──────────────────────┴─────────┘
```

---

## 🔗 Links

- **[GitHub Repository](https://github.com/kuju63/wt)**
- **[Report an Issue](https://github.com/kuju63/wt/issues)**
- **[Release Notes](https://github.com/kuju63/wt/releases)**

---

## 📄 License

[MIT License](./LICENSE)
