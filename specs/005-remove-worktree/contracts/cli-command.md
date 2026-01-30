# CLI Command Contract: Remove Worktree

**Scope**: Command-line interface specification for `wt remove`

## Command Signature

```bash
wt remove <worktree> [OPTIONS]
```

## Arguments

### `<worktree>` (required, positional)

**Type**: string
**Description**: Worktree identifier (branch name or path)
**Constraints**:

- Non-empty
- Max 255 characters
- Resolved to absolute path if relative path is provided
- Must correspond to existing worktree in current git repository

**Examples**:

- Branch name: `feature-auth`
- Relative path: `../feature-auth`
- Absolute path: `/home/dev/repos/project/.git/worktrees/feature-auth`

## Options

### `--force, -f` (optional, flag)

**Type**: boolean
**Default**: false
**Description**: Force removal even if worktree has uncommitted changes or is locked

**Behavior**:

- When set: skips uncommitted changes validation
- When set: bypasses worktree lock protection
- Explicitly requires developer consent to potential data loss

**Exit Code Impact**: Removal may succeed when standard removal would fail

---

### `--verbose, -v` (optional, flag)

**Type**: boolean
**Default**: false
**Description**: Show detailed diagnostics and intermediate steps

**Output**:

- Git commands executed
- Validation checks performed
- File deletion progress
- Timestamps for each operation

---

### `--output, -o` (optional, option)

**Type**: enum { "human", "json" }
**Default**: "human"
**Description**: Output format for result

**Behavior**:

- `human`: Pretty-printed, user-friendly output with Unicode formatting
- `json`: Structured JSON for automation/scripting

---

### `--help, -h` (optional, flag)

**Type**: boolean
**Default**: false
**Description**: Display command help and exit

**Output**: Full command usage, arguments, options, and examples

---

## Exit Codes

| Code | Meaning | Recoverable |
|------|---------|------------|
| 0 | Success | N/A |
| 1 | General failure (validation or removal error) | Yes |
| 2 | Worktree not found | Yes |
| 10 | Git command failed | Depends |
| 99 | Unexpected error | No |

---

## Output Specification

### Success Case (Human Output)

```
✓ Worktree 'feature-auth' removed successfully
  Path: /home/dev/repos/project/.git/worktrees/feature-auth
  Deleted: 1,234 files
  Duration: 0.234s
```

### Success Case (JSON Output)

```json
{
  "success": true,
  "worktree": "feature-auth",
  "path": "/home/dev/repos/project/.git/worktrees/feature-auth",
  "worktreeMetadataRemoved": true,
  "filesDeleted": 1234,
  "undeleteableItems": [],
  "duration": "0.234s"
}
```

### Failure Case: Not Found (Human)

```
✗ Error: Worktree 'invalid-branch' not found
  Available worktrees: feature-auth, bugfix-123, chore-docs

Use 'wt list' to see all worktrees
```

**Exit Code**: 2

### Failure Case: Not Found (JSON)

```json
{
  "success": false,
  "worktree": "invalid-branch",
  "error": "NotFound",
  "errorCode": "WT-RM-001",
  "message": "Worktree 'invalid-branch' not found"
}
```

---

### Failure Case: Main Worktree (Human)

```
✗ Error: Cannot remove main worktree
  The main working directory is protected from deletion

Switch to a different directory and try again:
  cd /path/to/repository
```

**Exit Code**: 1

### Failure Case: Uncommitted Changes (Human)

```
✗ Error: Cannot remove 'feature-auth': uncommitted changes present
  Commit or stash changes before removal:
    cd /path/to/feature-auth
    git commit -am "Save work"
    cd /path/to/repository
    wt remove feature-auth

Or force removal (WARNING: changes will be lost):
  wt remove feature-auth --force
```

**Exit Code**: 1

---

### Partial Failure: Some Files Remain (Human)

```
⚠ Worktree 'feature-auth' removed, but 2 files could not be deleted
  Path: /home/dev/repos/project/.git/worktrees/feature-auth
  Worktree metadata: Removed
  Files deleted: 1,232 of 1,234

Remaining files (manual cleanup required):
  • node_modules/.bin/package (Permission denied)
  • .DS_Store (File in use)

Use 'rm -rf <path>' to force deletion if needed.
```

**Exit Code**: 1 (partial failure is still a failure)

### Partial Failure (JSON)

```json
{
  "success": false,
  "worktree": "feature-auth",
  "path": "/home/dev/repos/project/.git/worktrees/feature-auth",
  "worktreeMetadataRemoved": true,
  "filesDeleted": 1232,
  "undeleteableItems": [
    {
      "path": "node_modules/.bin/package",
      "reason": "Permission denied"
    },
    {
      "path": ".DS_Store",
      "reason": "File in use"
    }
  ],
  "message": "Worktree removed but 2 files could not be deleted"
}
```

---

## Validation Sequence

1. **Parse arguments** (System.CommandLine)
   - Validate WorktreeId is non-empty
   - Validate OutputFormat is "human" or "json"

2. **Find worktree** (via git worktree list)
   - Resolve worktree identifier to path
   - Return NotFound error if no match

3. **Safety checks** (if not --force)
   - Check not main worktree
   - Check not current worktree
   - Check no uncommitted changes

4. **Lock checks** (if not --force)
   - Check worktree is not locked by another process

5. **Execute removal**
   - Call `git worktree remove [--force] <path>`
   - Remove working directory recursively
   - Collect and report any failures

6. **Format output**
   - Human-readable or JSON based on --output flag

---

## Error Messages & Codes

| Code | Condition | Message | Suggestion |
|------|-----------|---------|-----------|
| WT-RM-001 | Worktree not found | "Worktree 'X' not found" | Use `wt list` to find available worktrees |
| WT-RM-002 | Main/current worktree | "Cannot remove main/current worktree" | Switch to parent directory |
| WT-RM-003 | Uncommitted changes | "Uncommitted changes present in 'X'" | Commit/stash changes or use `--force` |
| WT-RM-004 | Permission denied | "Permission denied accessing path 'X'" | Run with appropriate permissions or manual cleanup |
| WT-RM-005 | Locked by process | "Worktree locked by another process" | Use `--force` to override or wait |

---

## Examples

### Remove a worktree by branch name

```bash
$ wt remove feature-auth
✓ Worktree 'feature-auth' removed successfully
  Path: /home/dev/repos/project/.git/worktrees/feature-auth
  Deleted: 1,234 files
  Duration: 0.234s
```

### Force removal with uncommitted changes

```bash
$ wt remove feature-auth --force
⚠ Forcing removal of 'feature-auth' with uncommitted changes
✓ Worktree removed (changes lost)
```

### JSON output for scripting

```bash
$ wt remove feature-auth --output json | jq .filesDeleted
1234
```

### Verbose output with diagnostics

```bash
$ wt remove feature-auth --verbose
[1/5] Validating worktree 'feature-auth'...
[2/5] Checking for uncommitted changes...
[3/5] Removing git metadata...
[4/5] Removing working directory...
[5/5] Cleaning up...
✓ Worktree 'feature-auth' removed successfully
```

---

## Notes

- All paths displayed as forward slashes (normalized across platforms)
- Human output includes emojis for visual distinction (✓, ✗, ⚠)
- JSON output is valid RFC 4627 compliant
- Help messages include practical examples
- Error messages include actionable suggestions
