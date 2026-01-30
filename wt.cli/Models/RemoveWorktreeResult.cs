namespace Kuju63.WorkTree.CommandLine.Models;

/// <summary>
/// Result data for a worktree removal operation.
/// </summary>
public class RemoveWorktreeResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the removal was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the worktree identifier that was removed.
    /// </summary>
    public required string WorktreeId { get; set; }

    /// <summary>
    /// Gets or sets the full path to the removed worktree.
    /// </summary>
    public required string RemovedPath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the git worktree metadata was removed.
    /// </summary>
    public bool WorktreeMetadataRemoved { get; set; }

    /// <summary>
    /// Gets or sets the number of files successfully deleted from disk.
    /// </summary>
    public int FilesDeleted { get; set; }

    /// <summary>
    /// Gets or sets the list of files that could not be deleted.
    /// </summary>
    public List<DeletionFailure> DeletionFailures { get; set; } = [];

    /// <summary>
    /// Gets or sets the duration of the removal operation.
    /// </summary>
    public TimeSpan Duration { get; set; }
}
