namespace Kuju63.WorkTree.CommandLine.Models;

/// <summary>
/// Represents a file or directory that could not be deleted during worktree removal.
/// </summary>
public class DeletionFailure
{
    /// <summary>
    /// Gets or sets the path to the file or directory that could not be deleted.
    /// </summary>
    public required string FilePath { get; set; }

    /// <summary>
    /// Gets or sets the reason for the deletion failure.
    /// </summary>
    public required string Reason { get; set; }

    /// <summary>
    /// Gets or sets the exception that caused the deletion failure, if any.
    /// </summary>
    public Exception? Exception { get; set; }
}
