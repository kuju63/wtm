using Kuju63.WorkTree.CommandLine.Utils;

namespace Kuju63.WorkTree.CommandLine.Models;

/// <summary>
/// Represents the options for checking out an existing branch as a worktree.
/// </summary>
public class CheckoutWorktreeOptions
{
    /// <summary>
    /// Gets the branch name to check out (local or remote).
    /// </summary>
    public required string BranchName { get; init; }

    /// <summary>
    /// Gets the remote name explicitly specified with --remote flag.
    /// Null means auto-selection.
    /// </summary>
    public string? Remote { get; init; }

    /// <summary>
    /// Gets a value indicating whether to fetch from remote before creating the worktree.
    /// </summary>
    public bool Fetch { get; init; }

    /// <summary>
    /// Gets the editor type to launch after creating the worktree.
    /// </summary>
    public EditorType? EditorType { get; init; }

    /// <summary>
    /// Gets the output format.
    /// </summary>
    public OutputFormat OutputFormat { get; init; } = OutputFormat.Human;

    /// <summary>
    /// Gets a value indicating whether to show verbose output.
    /// </summary>
    public bool Verbose { get; init; }

    /// <summary>
    /// Validates the checkout worktree options.
    /// </summary>
    /// <returns>A <see cref="ValidationResult"/> indicating whether the options are valid.</returns>
    public ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(BranchName))
        {
            return new ValidationResult(false, "Branch name is required");
        }

        var branchValidation = Validators.ValidateBranchName(BranchName);
        if (!branchValidation.IsValid)
        {
            return branchValidation;
        }

        if (Remote != null)
        {
            var remoteValidation = Validators.ValidateRemoteName(Remote);
            if (!remoteValidation.IsValid)
            {
                return remoteValidation;
            }
        }

        return new ValidationResult(true);
    }
}
