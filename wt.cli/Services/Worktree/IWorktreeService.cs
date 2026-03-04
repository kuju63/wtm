using Kuju63.WorkTree.CommandLine.Models;
using Kuju63.WorkTree.CommandLine.Services.Interaction;

namespace Kuju63.WorkTree.CommandLine.Services.Worktree;

/// <summary>
/// Defines methods for creating and managing Git worktrees.
/// </summary>
public interface IWorktreeService
{
    /// <summary>
    /// Creates a new worktree with the specified options asynchronously.
    /// </summary>
    /// <param name="options">The options for creating the worktree.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing the created worktree information.</returns>
    Task<CommandResult<WorktreeInfo>> CreateWorktreeAsync(CreateWorktreeOptions options);

    /// <summary>
    /// Creates a new worktree with the specified options asynchronously.
    /// </summary>
    /// <param name="options">The options for creating the worktree.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing the created worktree information.</returns>
    Task<CommandResult<WorktreeInfo>> CreateWorktreeAsync(CreateWorktreeOptions options, CancellationToken cancellationToken);

    /// <summary>
    /// Lists all worktrees in the repository asynchronously.
    /// </summary>
    /// <returns>A <see cref="CommandResult{T}"/> containing a list of worktree information, sorted by creation date (newest first).</returns>
    Task<CommandResult<List<WorktreeInfo>>> ListWorktreesAsync();

    /// <summary>
    /// Lists all worktrees in the repository asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing a list of worktree information, sorted by creation date (newest first).</returns>
    Task<CommandResult<List<WorktreeInfo>>> ListWorktreesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Validates whether a worktree can be removed asynchronously.
    /// </summary>
    /// <param name="worktreeIdentifier">The worktree identifier (branch name or path).</param>
    /// <param name="force">If true, skip uncommitted changes and lock checks.</param>
    /// <returns>A <see cref="RemovalValidationError"/> indicating the validation result.</returns>
    Task<RemovalValidationError> ValidateForRemovalAsync(string worktreeIdentifier, bool force);

    /// <summary>
    /// Validates whether a worktree can be removed asynchronously.
    /// </summary>
    /// <param name="worktreeIdentifier">The worktree identifier (branch name or path).</param>
    /// <param name="force">If true, skip uncommitted changes and lock checks.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="RemovalValidationError"/> indicating the validation result.</returns>
    Task<RemovalValidationError> ValidateForRemovalAsync(string worktreeIdentifier, bool force, CancellationToken cancellationToken);

    /// <summary>
    /// Removes a worktree and deletes its working directory asynchronously.
    /// </summary>
    /// <param name="options">The options for removing the worktree.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing the removal result.</returns>
    Task<CommandResult<RemoveWorktreeResult>> RemoveWorktreeAsync(RemoveWorktreeOptions options);

    /// <summary>
    /// Removes a worktree and deletes its working directory asynchronously.
    /// </summary>
    /// <param name="options">The options for removing the worktree.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing the removal result.</returns>
    Task<CommandResult<RemoveWorktreeResult>> RemoveWorktreeAsync(RemoveWorktreeOptions options, CancellationToken cancellationToken);

    /// <summary>
    /// Checks out an existing branch as a worktree.
    /// Prefers local branches; falls back to remote tracking refs if not found locally.
    /// </summary>
    /// <param name="options">The options for checking out the worktree.</param>
    /// <param name="interactionService">Service for interactive user selection.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing the created worktree information.</returns>
    Task<CommandResult<WorktreeInfo>> CheckoutWorktreeAsync(
        CheckoutWorktreeOptions options,
        IInteractionService interactionService,
        CancellationToken cancellationToken = default);
}
