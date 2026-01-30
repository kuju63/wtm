# Research Phase: Remove Git Worktree Feature

**Date**: 2026-01-24 | **Feature**: Remove Git Worktree (005-remove-worktree)

## Research Findings

### 1. File System Operations Architecture

**Decision**: Use existing `System.IO.Abstractions.IFileSystem` abstraction layer for all directory deletion operations.

**Rationale**:

- Already a production dependency in the project (version 22.1.0)
- Provides OS-agnostic directory and file operations
- Enables testability through mocking (System.IO.Abstractions.TestingHelpers)
- Consistent with existing project patterns (PathHelper, etc.)

**Implementation Details**:

- `IFileSystem.Directory.Delete(path, recursive: true)` for recursive directory deletion
- Wraps with try-catch to capture permission/access errors
- Returns detailed failure information for FR-010 (partial deletion reporting)

**Testing Approach**:

- Unit tests: Mock IFileSystem using Moq
- Integration tests: Use System.IO.Abstractions.TestingHelpers.MockFileSystem for isolated testing without touching disk
- E2E tests: Real file system with test-specific temporary directories

---

### 2. Git Worktree Metadata Removal

**Decision**: Use existing `IGitService` to delegate git worktree removal via `git worktree remove` command.

**Rationale**:

- GitService already abstracts git command execution
- Standard git command handles all worktree metadata cleanup
- Consistent with existing pattern (CreateCommand uses GitService for validation)
- Avoids reimplementing git's internal state management

**Implementation Details**:

- Call `git worktree remove [--force] <path>`
- GitService returns structured result with success/failure
- Use `--force` flag when RemoveWorktreeOptions.Force is true
- Git command execution is already cross-platform tested

**Error Handling**:

- Git exit codes indicate specific failure reasons (not found, locked, etc.)
- Parse git stderr to provide user-friendly error messages per FR-008

---

### 3. Uncommitted Changes Detection

**Decision**: Use `git status --porcelain <worktree-path>` to detect uncommitted changes before removal.

**Rationale**:

- Cheap operation (no file access); just queries git index
- Prevents data loss by blocking removal of worktrees with modified files
- Allows `--force` to bypass this check (explicit opt-in by developer)
- Aligns with git's safety-first philosophy

**Implementation Details**:

- GitService method: `HasUncommittedChanges(worktreeInfo.Path)` returns bool
- Checks untracked files, modified files, staged but uncommitted changes
- Force flag overrides this check (FR-007 behavior)
- No interactive prompt (per clarification Q1)

**Testing Approach**:

- Mock git command to simulate committed/uncommitted states
- Integration test with real worktree containing unsaved changes

---

### 4. Worktree Validation Logic

**Decision**: Implement worktree validation checks in WorktreeService before initiating removal.

**Rationale**:

- Centralize business logic in service layer (existing pattern)
- Enables unit testing of validation rules in isolation
- Reusable validation methods (e.g., IsMainWorktree, IsCurrentWorktree)
- Clear separation from command/CLI layer

**Validation Checks** (FR-005, FR-006):

1. **Main Worktree Protection**: Worktree path == git root's main worktree path → ERROR
2. **Current Worktree Check**: Worktree == currently-checked-out session → ERROR
3. **Worktree Existence**: Queried worktree ID/path exists in git → ERROR if not found
4. **Uncommitted Changes** (FR-007): If not --force, check for modified files → ERROR if present

**Current Worktree Detection**:

- Compare worktree path against `git rev-parse --show-toplevel` + current session's working directory
- Or use `git worktree list --porcelain` and match against pwd

---

### 5. Partial Deletion Reporting

**Decision**: Capture undeleted files during directory deletion and report them separately; remove worktree metadata regardless.

**Rationale**:

- Implements FR-010 behavior: pragmatic partial failure handling
- Prioritizes cleaning up git metadata (the main goal)
- Leaves disk cleanup for user manual resolution if needed
- Prevents orphaned worktree entries in git

**Implementation Details**:

- Directory.Delete() attempts recursive deletion; catch UnauthorizedAccessException / IOException
- Collect list of files that couldn't be deleted with their exception messages
- Return structured result: { Success: bool, DeletedCount: int, UndeleteableItems: List<(path, reason)> }
- Output formatted error list per FR-008

**Testing Approach**:

- Mock filesystem: simulate locked file preventing deletion
- Check that removal proceeds and reports the failure
- Verify worktree entry is removed from git despite partial disk failure

---

### 6. Error Message Design

**Decision**: Structured error codes and context-specific user guidance (existing pattern).

**Rationale**:

- Project already uses ErrorCodes enum (WT001-002, etc.)
- Enables automation (scripts can parse error codes)
- User-friendly suggestions improve developer experience per SC-004

**Error Code Allocation** (extending existing WT codes):

- **WT-RM-001**: Worktree not found
- **WT-RM-002**: Cannot remove main/current worktree
- **WT-RM-003**: Uncommitted changes present (suggest commit/stash or use --force)
- **WT-RM-004**: Permission denied during directory deletion (suggest running as admin or manual cleanup)
- **WT-RM-005**: Worktree locked by another process (suggest --force or waiting)

**User Guidance Examples**:

```
error: Cannot remove worktree 'feature-branch': Current directory is in this worktree
Suggestion: Use 'cd <parent-repo>' to switch out of the worktree, then retry removal
```

---

### 7. Command-Line Interface Design

**Decision**: Follow existing command pattern (System.CommandLine 2.0) with verb-noun naming.

**Rationale**:

- Consistent with `create` and `list` commands
- Automatic --help generation
- Type-safe argument parsing
- Already familiar to users of existing wt commands

**Command Syntax**:

```
wt remove <worktree-id-or-path> [--force] [--verbose] [--output json|human]
```

**Arguments & Options**:

- `<worktree-id-or-path>`: Positional argument (required) — branch name or path
- `--force, -f`: Optional flag (boolean) — bypass uncommitted changes check
- `--verbose, -v`: Optional flag (boolean) — detailed diagnostics
- `--output, -o`: Optional option {human|json} — output format (existing pattern)

**Automatic Help**:

```
wt remove <worktree> [options]

Description:
  Remove a git worktree and delete its working directory from disk.

Arguments:
  <worktree>                The branch name or worktree path to remove

Options:
  -f, --force              Force removal even with uncommitted changes or locks
  -v, --verbose            Show detailed diagnostics
  -o, --output <format>    Output format: human (default) or json
  -h, --help               Show help
```

---

### 8. Output Formats

**Decision**: Implement human and JSON output following existing TableFormatter pattern.

**Rationale**:

- Consistent with list/create commands
- Enables automation and scripting (JSON)
- Human-readable by default

**Human Output Example**:

```
✓ Worktree 'feature-branch' removed successfully
  Deleted: /path/to/worktree
  Metadata: Cleaned from git worktree list
```

**JSON Output Example**:

```json
{
  "success": true,
  "worktree": "feature-branch",
  "path": "/path/to/worktree",
  "filesDeleted": 245,
  "undeleteableItems": [],
  "duration": "0.2s"
}
```

**Partial Failure Example (JSON)**:

```json
{
  "success": false,
  "worktree": "feature-branch",
  "path": "/path/to/worktree",
  "worktreeRemoved": true,
  "filesDeleted": 240,
  "undeleteableItems": [
    {"path": "node_modules/locked-package", "reason": "Permission denied"}
  ],
  "message": "Worktree removed but 1 file could not be deleted"
}
```

---

## Dependency Review

All required functionality is covered by existing dependencies:

| Dependency | Version | Use |
|------------|---------|-----|
| System.CommandLine | 2.0.2 | CLI parsing and command registration (existing) |
| System.IO.Abstractions | 22.1.0 | Cross-platform directory deletion (existing) |
| xUnit | 2.9.3 | Testing framework (existing) |
| Moq | 4.20.72 | Mocking IFileSystem, IGitService (existing) |
| System.IO.Abstractions.TestingHelpers | 22.1.0 | MockFileSystem for isolated tests (existing) |

**No new dependencies required.**

---

## Integration Points

1. **GitService**: Extend to detect uncommitted changes; delegate removal to git command
2. **WorktreeService**: Add RemoveWorktree method with validation logic
3. **PathHelper**: Use for worktree path resolution and normalization
4. **Program.cs**: Register RemoveCommand in CLI
5. **ErrorCodes**: Add WT-RM-001 through WT-RM-005
6. **Output formatters**: Extend TableFormatter with removal-specific formatting (optional; may use simple text output)

---

## No NEEDS CLARIFICATION Issues

All technical decisions resolved through research. Ready for Phase 1 design.
