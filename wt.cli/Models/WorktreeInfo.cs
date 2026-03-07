namespace Kuju63.WorkTree.CommandLine.Models;

/// <summary>
/// Represents information about a Git worktree.
/// </summary>
/// <param name="Path">The file system path of the worktree.</param>
/// <param name="Branch">The branch name associated with this worktree.</param>
/// <param name="IsDetached">Indicates whether the worktree is in a detached HEAD state.</param>
/// <param name="CommitHash">The full commit hash of the current HEAD.</param>
/// <param name="CreatedAt">The date and time when the worktree was created.</param>
/// <param name="Exists">Indicates whether the worktree exists on the filesystem.</param>
public record WorktreeInfo(
    string Path,
    string Branch,
    bool IsDetached,
    string CommitHash,
    DateTime CreatedAt,
    bool Exists
)
{
    /// <summary>
    /// Gets the remote name if this worktree was checked out from a remote tracking branch.
    /// </summary>
    public string? Remote { get; init; }

    /// <summary>
    /// Gets a display-friendly branch string.
    /// </summary>
    /// <returns>
    /// For normal branches: the branch name.
    /// For detached HEAD: "abc1234 (detached)" format.
    /// </returns>
    public string GetDisplayBranch()
    {
        if (IsDetached)
        {
            var safeHash = CommitHash ?? string.Empty;
            var shortHash = safeHash[..Math.Min(7, safeHash.Length)];
            return $"{shortHash} (detached)";
        }
        return Branch;
    }

    /// <summary>
    /// Gets a display-friendly status string.
    /// </summary>
    /// <returns>
    /// "active" if the worktree exists on disk.
    /// "missing" if the worktree does not exist on disk.
    /// </returns>
    public string GetDisplayStatus()
    {
        return Exists ? "active" : "missing";
    }
}
