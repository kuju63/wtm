namespace Kuju63.WorkTree.CommandLine.Models;

/// <summary>
/// Enumeration of validation errors for worktree removal operations.
/// </summary>
public enum RemovalValidationError
{
    /// <summary>
    /// No validation error; worktree can be removed.
    /// </summary>
    None = 0,

    /// <summary>
    /// The specified worktree was not found.
    /// </summary>
    NotFound = 1,

    /// <summary>
    /// Cannot remove the main worktree.
    /// </summary>
    IsMainWorktree = 2,

    /// <summary>
    /// Cannot remove the currently checked-out worktree.
    /// </summary>
    IsCurrentWorktree = 3,

    /// <summary>
    /// Worktree has uncommitted changes and --force was not specified.
    /// </summary>
    HasUncommittedChanges = 4,

    /// <summary>
    /// Worktree is locked (lock file exists).
    /// </summary>
    IsLocked = 5
}
