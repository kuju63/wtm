# Data Model: Remove Git Worktree

**Date**: 2026-01-24 | **Feature**: 005-remove-worktree

## Entities

### RemoveWorktreeOptions

**Purpose**: CLI options for the remove command

**Fields**:

```csharp
public class RemoveWorktreeOptions
{
    /// <summary>
    /// Worktree identifier: branch name or path
    /// </summary>
    public required string WorktreeId { get; set; }

    /// <summary>
    /// Force removal even with uncommitted changes or locks
    /// </summary>
    public bool Force { get; set; } = false;

    /// <summary>
    /// Detailed diagnostics output
    /// </summary>
    public bool Verbose { get; set; } = false;

    /// <summary>
    /// Output format: "human" or "json"
    /// </summary>
    public string OutputFormat { get; set; } = "human";
}
```

**Validation Rules**:

- WorktreeId: Non-null, non-empty string (max 255 characters)
- Force: Boolean flag (no validation)
- OutputFormat: Must be "human" or "json" (enum recommended)

**Usage**: Bound from System.CommandLine command arguments/options

---

### RemoveWorktreeResult

**Purpose**: Structured result of a remove operation

**Fields**:

```csharp
public class RemoveWorktreeResult : CommandResult<RemoveWorktreeData>
{
    /// <summary>
    /// Removal result details
    /// </summary>
    public RemoveWorktreeData Data { get; set; } = new();
}

public class RemoveWorktreeData
{
    /// <summary>
    /// Whether removal completed successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Worktree identifier that was removed
    /// </summary>
    public required string WorktreeId { get; set; }

    /// <summary>
    /// Full path to the removed worktree
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    /// Whether the git worktree metadata was removed
    /// </summary>
    public bool WorktreeMetadataRemoved { get; set; }

    /// <summary>
    /// Number of files successfully deleted from disk
    /// </summary>
    public int FilesDeleted { get; set; }

    /// <summary>
    /// Files that could not be deleted and their reasons
    /// </summary>
    public List<DeletionFailure> UndeleteableItems { get; set; } = new();

    /// <summary>
    /// Total duration of removal operation
    /// </summary>
    public TimeSpan Duration { get; set; }
}

public class DeletionFailure
{
    /// <summary>
    /// Path to the file/directory that could not be deleted
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    /// Reason for failure (permission denied, file locked, etc.)
    /// </summary>
    public required string Reason { get; set; }
}
```

**Relationships**:

- Extends CommandResult<T> (existing pattern)
- Contains DeletionFailure items
- Used by RemoveCommand for formatting output

---

### WorktreeValidationError

**Purpose**: Validation error details for pre-removal checks

**Fields**:

```csharp
public enum RemovalValidationError
{
    None = 0,
    NotFound = 1,
    IsMainWorktree = 2,
    IsCurrentWorktree = 3,
    UncommittedChanges = 4,
    LockedByAnotherProcess = 5,
    PermissionDenied = 6,
    UnknownError = 7
}
```

**State Transitions**:

- Worktree → CheckExists (NotFound if missing)
- Worktree → CheckMainWorktree (IsMainWorktree if git root)
- Worktree → CheckCurrentWorktree (IsCurrentWorktree if pwd matches)
- Worktree → CheckUncommittedChanges (UncommittedChanges if modified, unless Force=true)
- Worktree → CheckLocked (LockedByAnotherProcess if git lock present)
- ✓ → RemoveGitMetadata → RemoveDirectory → ✓ Success

**Usage**: Returned by WorktreeService validation methods to determine if removal can proceed

---

## Service Layer Integration

### IWorktreeService Extension

**New Methods**:

```csharp
public interface IWorktreeService
{
    // Existing methods...

    /// <summary>
    /// Validates a worktree for removal
    /// </summary>
    /// <param name="worktreeId">Worktree identifier (branch name or path)</param>
    /// <param name="force">If true, skip uncommitted changes check</param>
    /// <returns>Validation error code; None if valid</returns>
    RemovalValidationError ValidateForRemoval(string worktreeId, bool force);

    /// <summary>
    /// Removes a worktree and its working directory
    /// </summary>
    /// <param name="options">Removal options</param>
    /// <returns>Removal operation result with detailed information</returns>
    Task<RemoveWorktreeResult> RemoveWorktreeAsync(RemoveWorktreeOptions options);
}
```

**Behavior**:

- ValidateForRemoval: Performs all safety checks; returns first error or None
- RemoveWorktreeAsync: Orchestrates removal workflow; handles errors gracefully
  1. Validate worktree exists and can be removed
  2. Check for uncommitted changes (unless Force)
  3. Remove git metadata via git worktree remove
  4. Remove working directory recursively
  5. Report any partial failures
  6. Return structured result

---

## Data Validation Rules

### WorktreeId Validation

- Non-empty string (max 255 chars)
- Supports alphanumeric, hyphens, underscores, slashes (path separators)
- Resolved to absolute path via existing PathHelper

### Force Flag Validation

- Boolean; no specific validation needed
- When true: bypasses UncommittedChanges check
- When true: allows removal of locked worktrees

### Path Validation

- Absolute path required (resolved by PathHelper.ResolvePath)
- Must be within git repository
- Must exist on filesystem (verified via IFileSystem)

---

## Error Handling & Edge Cases

### Handled Edge Cases

1. **Worktree not found**
   - Result: RemovalValidationError.NotFound
   - Error code: WT-RM-001
   - Message: "Worktree 'X' not found"

2. **Main/primary worktree removal attempt**
   - Result: RemovalValidationError.IsMainWorktree
   - Error code: WT-RM-002
   - Message: "Cannot remove main worktree"

3. **Current worktree removal attempt (pwd is inside)**
   - Result: RemovalValidationError.IsCurrentWorktree
   - Error code: WT-RM-002
   - Message: "Cannot remove the currently checked-out worktree"

4. **Uncommitted changes present**
   - Result: RemovalValidationError.UncommittedChanges (unless force=true)
   - Error code: WT-RM-003
   - Suggestion: "Commit or stash changes, or use --force to override"

5. **Worktree locked by another process**
   - Result: RemovalValidationError.LockedByAnotherProcess
   - Error code: WT-RM-005
   - Suggestion: "Use --force to override lock, or wait for process to finish"

6. **Permission denied during directory deletion**
   - Result: Success=false but WorktreeMetadataRemoved=true
   - Error code: WT-RM-004
   - Partial deletion reported; user can manually clean up

7. **Directory already deleted but worktree entry exists**
   - Git metadata is removed
   - FilesDeleted = 0, UndeleteableItems = empty
   - Success = true (goal achieved)

8. **Partial directory deletion (some files locked)**
   - Result: Success=false (FR-010 behavior)
   - WorktreeMetadataRemoved=true
   - UndeleteableItems list populated with locked files
   - Message indicates which files remain on disk

---

## Lifecycle & State Transitions

```
Initial State: Worktree exists in git, directory on disk

1. Validation Phase
   ├─ Check exists?
   ├─ Check main worktree?
   ├─ Check current worktree?
   ├─ Check uncommitted changes (if not force)?
   └─ Check locked by process?

2. Removal Phase
   ├─ Remove git metadata (git worktree remove)
   └─ Remove directory from disk (recursive delete)

3. Report Phase
   ├─ Count deleted files
   ├─ List undeleteable items
   └─ Return RemoveWorktreeResult

Final State: Worktree removed from git, directory deleted (or partially)
```

---

## Testing Strategy

### Unit Tests (This Scope)

- RemoveWorktreeOptions validation
- RemovalValidationError classification
- Path resolution and normalization
- Uncommitted changes detection (mocked git)
- Validation logic (mocked GitService, IFileSystem)
- Error message formatting
- Result object creation

### Mocking & Test Helpers

- Mock IGitService for git command execution
- Mock IFileSystem using Moq for file operations
- Mock IProcessRunner for git process calls

### Future Scope (Out of Scope for This Feature)

- Integration tests (require integration test infrastructure)
- E2E tests (require test harness and temporary worktree creation)
- These will be addressed in a follow-up feature when CI pipeline supports them

---

## Notes

- All classes follow existing project patterns (CommandResult<T>, Options classes)
- No new dependencies required (uses System.IO.Abstractions, existing git service)
- Error codes extend existing WT-RM prefix (consistent with other operations)
- Supports both simple branch names and full paths for worktree identification
- Unit tests only; integration/E2E deferred to future infrastructure work
