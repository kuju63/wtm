# ADR 0006: Force Flag Behavior for Worktree Removal

**Status**: Accepted
**Date**: 2026-01-25
**Context**: Feature 005 - Remove Git Worktree
**Related**: [spec.md](../../specs/005-remove-worktree/spec.md), [plan.md](../../specs/005-remove-worktree/plan.md)

## Context

The `wt remove` command needs to safely remove Git worktrees while preventing accidental data loss. However, users may need to override safety checks in certain scenarios:

1. Remove a worktree with uncommitted changes (intentionally discarding work)
2. Remove a locked worktree (process no longer running)
3. Batch cleanup scripts that need to proceed without prompts

The project constitution requires explicit user action for destructive operations (Constitution IV.2).

## Decision

### Force Flag Semantics

The `--force` flag bypasses the following safety checks:

| Check                  | Without --force      | With --force        |
| ---------------------- | -------------------- | ------------------- |
| Uncommitted changes    | Error WT-RM-003      | Proceeds            |
| Locked worktree        | Error WT-RM-005      | Proceeds            |
| Main worktree          | Error WT-RM-002      | Error (never bypass)|
| Current directory      | Error WT-RM-002      | Error (never bypass)|

### Protected Operations (Cannot Be Forced)

Two operations are **never** bypassable, even with `--force`:

1. **Main worktree removal**: The repository's primary working directory is protected
2. **Current directory removal**: Cannot remove the worktree containing the current working directory

**Rationale**: These protections prevent repository corruption and shell environment issues.

### Implementation

```csharp
public async Task<RemovalValidationError> ValidateForRemovalAsync(
    string worktreeIdentifier, bool force, CancellationToken cancellationToken)
{
    // Protected checks - always enforced
    if (IsMainWorktree(worktrees, targetWorktree))
        return RemovalValidationError.IsMainWorktree;

    if (IsCurrentWorktree(targetWorktree))
        return RemovalValidationError.IsCurrentWorktree;

    // Bypassable checks - skipped when force=true
    if (!force)
    {
        if (await HasUncommittedChangesAsync(path))
            return RemovalValidationError.HasUncommittedChanges;

        if (await IsWorktreeLockedAsync(path))
            return RemovalValidationError.IsLocked;
    }

    return RemovalValidationError.None;
}
```

### Git Integration

The `--force` flag maps directly to Git's `--force` flag:

```bash
git worktree remove <path>           # Normal removal
git worktree remove --force <path>   # Force removal
```

## Alternatives Considered

### Alternative 1: Confirmation Prompt

**Approach**: Interactive confirmation for destructive operations

**Pros**:

- Provides explicit user consent
- Familiar pattern from other tools

**Cons**:

- Breaks automation/scripting
- Requires TTY detection
- Not suitable for CI/CD environments

**Rejected**: Project targets CLI power users and automation scenarios.

### Alternative 2: Separate Flags

**Approach**: `--force-changes` and `--force-unlock` as separate flags

**Pros**:

- Fine-grained control
- Explicit about what is being bypassed

**Cons**:

- Cognitive overhead (multiple flags)
- Deviation from git conventions (`git worktree remove --force`)
- Complicates command parsing

**Rejected**: Git compatibility and simplicity preferred.

### Alternative 3: No Force Option

**Approach**: Always require clean state before removal

**Pros**:

- Maximum safety
- No accidental data loss

**Cons**:

- Impractical for cleanup scripts
- Frustrating UX when intentionally discarding changes
- Deviates from git behavior

**Rejected**: Does not meet user story requirements (US2).

## Consequences

### Positive

- **Git Compatibility**: Behavior mirrors `git worktree remove --force`
- **Scriptability**: Single flag enables batch operations
- **Safety by Default**: Without --force, all safety checks enforced
- **Explicit Intent**: User must explicitly request destructive operation

### Negative

- **Data Loss Risk**: --force can discard uncommitted changes
- **No Undo**: Removed worktrees cannot be recovered (git prune may clear references)
- **Lock Override**: May terminate processes still using the worktree

### Mitigations

1. **Error Messages**: Clear actionable messages explain what --force bypasses
2. **Verbose Mode**: `--verbose` shows exactly which checks were skipped
3. **Documentation**: Usage guide warns about --force implications

## Error Messages

| Error Code | Condition            | Solution Provided                                  |
| ---------- | -------------------- | -------------------------------------------------- |
| WT-RM-001  | Worktree not found   | "Use 'wt list' to see available worktrees"         |
| WT-RM-002  | Main/current worktree| Specific message per case (cannot bypass)          |
| WT-RM-003  | Uncommitted changes  | "Commit or stash changes, or use --force"          |
| WT-RM-005  | Locked worktree      | "Use --force to override, or wait for process"     |

## References

- [Git Worktree Documentation](https://git-scm.com/docs/git-worktree)
- [Project Constitution - Section IV: Safety](../../CONSTITUTION.md)
- [Feature Specification](../../specs/005-remove-worktree/spec.md)

## Review Schedule

This ADR should be reviewed:

- If additional safety checks are added to removal
- If git worktree behavior changes significantly
- If user feedback indicates confusion about --force behavior
