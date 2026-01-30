namespace Kuju63.WorkTree.CommandLine.Models;

/// <summary>
/// Options for the remove worktree command.
/// </summary>
public class RemoveWorktreeOptions
{
    /// <summary>
    /// Gets or sets the worktree identifier (branch name or path).
    /// </summary>
    public required string WorktreeIdentifier { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to force removal even with uncommitted changes or locks.
    /// </summary>
    public bool Force { get; set; }

    /// <summary>
    /// Gets or sets the output format (human or json).
    /// </summary>
    public OutputFormat OutputFormat { get; set; } = OutputFormat.Human;

    /// <summary>
    /// Gets or sets a value indicating whether to show verbose output.
    /// </summary>
    public bool Verbose { get; set; }
}
