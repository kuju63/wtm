using Kuju63.WorkTree.CommandLine.Models;

namespace Kuju63.WorkTree.CommandLine.Services.Git;

/// <summary>
/// Defines methods for interacting with Git repositories.
/// </summary>
public interface IGitService
{
    /// <summary>
    /// Checks whether the current directory is a Git repository asynchronously.
    /// </summary>
    /// <returns>A <see cref="CommandResult{T}"/> containing <see langword="true"/> if in a Git repository; otherwise, <see langword="false"/>.</returns>
    Task<CommandResult<bool>> IsGitRepositoryAsync();

    /// <summary>
    /// Checks whether the current directory is a Git repository asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing <see langword="true"/> if in a Git repository; otherwise, <see langword="false"/>.</returns>
    Task<CommandResult<bool>> IsGitRepositoryAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the name of the current branch asynchronously.
    /// </summary>
    /// <returns>A <see cref="CommandResult{T}"/> containing the current branch name.</returns>
    Task<CommandResult<string>> GetCurrentBranchAsync();

    /// <summary>
    /// Gets the name of the current branch asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing the current branch name.</returns>
    Task<CommandResult<string>> GetCurrentBranchAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Checks whether a branch exists (local or remote) asynchronously.
    /// </summary>
    /// <param name="branchName">The name of the branch to check.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing <see langword="true"/> if the branch exists; otherwise, <see langword="false"/>.</returns>
    Task<CommandResult<bool>> BranchExistsAsync(string branchName);

    /// <summary>
    /// Checks whether a branch exists (local or remote) asynchronously.
    /// </summary>
    /// <param name="branchName">The name of the branch to check.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing <see langword="true"/> if the branch exists; otherwise, <see langword="false"/>.</returns>
    Task<CommandResult<bool>> BranchExistsAsync(string branchName, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new branch from a base branch asynchronously.
    /// </summary>
    /// <param name="branchName">The name of the branch to create.</param>
    /// <param name="baseBranch">The base branch to branch from.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing information about the created branch.</returns>
    Task<CommandResult<BranchInfo>> CreateBranchAsync(string branchName, string baseBranch);

    /// <summary>
    /// Creates a new branch from a base branch asynchronously.
    /// </summary>
    /// <param name="branchName">The name of the branch to create.</param>
    /// <param name="baseBranch">The base branch to branch from.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing information about the created branch.</returns>
    Task<CommandResult<BranchInfo>> CreateBranchAsync(string branchName, string baseBranch, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new worktree at the specified path asynchronously.
    /// </summary>
    /// <param name="path">The path where the worktree should be created.</param>
    /// <param name="branchName">The branch to checkout in the new worktree.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing <see langword="true"/> if the worktree was added successfully; otherwise, <see langword="false"/>.</returns>
    Task<CommandResult<bool>> AddWorktreeAsync(string path, string branchName);

    /// <summary>
    /// Adds a new worktree at the specified path asynchronously.
    /// </summary>
    /// <param name="path">The path where the worktree should be created.</param>
    /// <param name="branchName">The branch to checkout in the new worktree.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing <see langword="true"/> if the worktree was added successfully; otherwise, <see langword="false"/>.</returns>
    Task<CommandResult<bool>> AddWorktreeAsync(string path, string branchName, CancellationToken cancellationToken);

    /// <summary>
    /// Lists all worktrees in the repository asynchronously.
    /// </summary>
    /// <returns>A <see cref="CommandResult{T}"/> containing a list of worktree information.</returns>
    Task<CommandResult<List<WorktreeInfo>>> ListWorktreesAsync();

    /// <summary>
    /// Lists all worktrees in the repository asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing a list of worktree information.</returns>
    Task<CommandResult<List<WorktreeInfo>>> ListWorktreesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Checks whether the specified worktree has uncommitted changes asynchronously.
    /// </summary>
    /// <param name="worktreePath">The path to the worktree.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing <see langword="true"/> if there are uncommitted changes; otherwise, <see langword="false"/>.</returns>
    Task<CommandResult<bool>> HasUncommittedChangesAsync(string worktreePath);

    /// <summary>
    /// Checks whether the specified worktree has uncommitted changes asynchronously.
    /// </summary>
    /// <param name="worktreePath">The path to the worktree.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing <see langword="true"/> if there are uncommitted changes; otherwise, <see langword="false"/>.</returns>
    Task<CommandResult<bool>> HasUncommittedChangesAsync(string worktreePath, CancellationToken cancellationToken);

    /// <summary>
    /// Checks whether the specified worktree is locked asynchronously.
    /// </summary>
    /// <param name="worktreePath">The path to the worktree.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing <see langword="true"/> if the worktree is locked; otherwise, <see langword="false"/>.</returns>
    Task<CommandResult<bool>> IsWorktreeLockedAsync(string worktreePath);

    /// <summary>
    /// Checks whether the specified worktree is locked asynchronously.
    /// </summary>
    /// <param name="worktreePath">The path to the worktree.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing <see langword="true"/> if the worktree is locked; otherwise, <see langword="false"/>.</returns>
    Task<CommandResult<bool>> IsWorktreeLockedAsync(string worktreePath, CancellationToken cancellationToken);

    /// <summary>
    /// Removes a worktree from the git repository asynchronously.
    /// </summary>
    /// <param name="worktreePath">The path to the worktree to remove.</param>
    /// <param name="force">If true, force removal even if the worktree is locked or has uncommitted changes.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing <see langword="true"/> if the worktree was removed successfully; otherwise, <see langword="false"/>.</returns>
    Task<CommandResult<bool>> RemoveWorktreeAsync(string worktreePath, bool force);

    /// <summary>
    /// Removes a worktree from the git repository asynchronously.
    /// </summary>
    /// <param name="worktreePath">The path to the worktree to remove.</param>
    /// <param name="force">If true, force removal even if the worktree is locked or has uncommitted changes.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing <see langword="true"/> if the worktree was removed successfully; otherwise, <see langword="false"/>.</returns>
    Task<CommandResult<bool>> RemoveWorktreeAsync(string worktreePath, bool force, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the list of configured remotes from git remote.
    /// </summary>
    /// <returns>A <see cref="CommandResult{T}"/> containing the list of remote names.</returns>
    Task<CommandResult<IReadOnlyList<string>>> GetRemotesAsync()
        => GetRemotesAsync(CancellationToken.None);

    /// <summary>
    /// Gets the list of configured remotes from git remote.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing the list of remote names.</returns>
    Task<CommandResult<IReadOnlyList<string>>> GetRemotesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets remote tracking branches from git branch -r output.
    /// </summary>
    /// <returns>A <see cref="CommandResult{T}"/> containing the list of remote branch info.</returns>
    Task<CommandResult<IReadOnlyList<RemoteBranchInfo>>> GetRemoteTrackingBranchesAsync()
        => GetRemoteTrackingBranchesAsync(null, CancellationToken.None);

    /// <summary>
    /// Gets remote tracking branches from git branch -r output.
    /// If branchName is specified, returns only references with that branch name.
    /// </summary>
    /// <param name="branchName">Optional branch name filter.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing the list of remote branch info.</returns>
    Task<CommandResult<IReadOnlyList<RemoteBranchInfo>>> GetRemoteTrackingBranchesAsync(string? branchName)
        => GetRemoteTrackingBranchesAsync(branchName, CancellationToken.None);

    /// <summary>
    /// Gets remote tracking branches from git branch -r output.
    /// If branchName is specified, returns only references with that branch name.
    /// </summary>
    /// <param name="branchName">Optional branch name filter.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing the list of remote branch info.</returns>
    Task<CommandResult<IReadOnlyList<RemoteBranchInfo>>> GetRemoteTrackingBranchesAsync(
        string? branchName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes git fetch for the specified remote.
    /// </summary>
    /// <param name="remote">The remote name to fetch from.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing <see cref="Unit"/> on success.</returns>
    Task<CommandResult<Unit>> FetchFromRemoteAsync(string remote)
        => FetchFromRemoteAsync(remote, CancellationToken.None);

    /// <summary>
    /// Executes git fetch for the specified remote.
    /// </summary>
    /// <param name="remote">The remote name to fetch from.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing <see cref="Unit"/> on success.</returns>
    Task<CommandResult<Unit>> FetchFromRemoteAsync(string remote, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the upstream remote configured for a local branch.
    /// Returns null if no upstream is configured (not an error).
    /// </summary>
    /// <param name="branchName">The local branch name.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing the remote name or null if not configured.</returns>
    Task<CommandResult<string?>> GetBranchUpstreamRemoteAsync(string branchName)
        => GetBranchUpstreamRemoteAsync(branchName, CancellationToken.None);

    /// <summary>
    /// Gets the upstream remote configured for a local branch.
    /// Returns null if no upstream is configured (not an error).
    /// </summary>
    /// <param name="branchName">The local branch name.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing the remote name or null if not configured.</returns>
    Task<CommandResult<string?>> GetBranchUpstreamRemoteAsync(string branchName, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a worktree from a remote tracking branch using:
    /// git worktree add --track -b &lt;branch&gt; &lt;path&gt; &lt;remote&gt;/&lt;branch&gt;
    /// </summary>
    /// <param name="worktreePath">The path where the worktree should be created.</param>
    /// <param name="branchName">The branch name to create locally.</param>
    /// <param name="remoteName">The remote name.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing <see cref="Unit"/> on success.</returns>
    Task<CommandResult<Unit>> AddWorktreeFromRemoteAsync(
        string worktreePath,
        string branchName,
        string remoteName,
        CancellationToken cancellationToken = default);
}
