using System.IO.Abstractions;
using Kuju63.WorkTree.CommandLine.Models;
using Kuju63.WorkTree.CommandLine.Utils;

namespace Kuju63.WorkTree.CommandLine.Services.Git;

/// <summary>
/// Provides functionality for interacting with Git repositories.
/// </summary>
public class GitService : IGitService
{
    private readonly IProcessRunner _processRunner;
    private readonly IFileSystem _fileSystem;

    public GitService(IProcessRunner processRunner, IFileSystem fileSystem)
    {
        _processRunner = processRunner;
        _fileSystem = fileSystem;
    }

    /// <summary>
    /// Checks whether the current directory is a Git repository asynchronously.
    /// This overload does not accept a cancellation token.
    /// </summary>
    /// <returns>A <see cref="CommandResult{T}"/> containing <see langword="true"/> if in a Git repository; otherwise, <see langword="false"/>.</returns>
    public Task<CommandResult<bool>> IsGitRepositoryAsync()
        => IsGitRepositoryAsync(CancellationToken.None);

    /// <summary>
    /// Checks whether the current directory is a Git repository asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing <see langword="true"/> if in a Git repository; otherwise, <see langword="false"/>.</returns>
    public async Task<CommandResult<bool>> IsGitRepositoryAsync(CancellationToken cancellationToken)
    {
        var result = await _processRunner.RunAsync("git", "rev-parse --git-dir", null, cancellationToken);
        return CommandResult<bool>.Success(result.ExitCode == 0);
    }

    /// <summary>
    /// Gets the name of the current branch asynchronously. This overload does not accept a cancellation token.
    /// </summary>
    /// <returns>A <see cref="CommandResult{T}"/> containing the current branch name.</returns>
    public Task<CommandResult<string>> GetCurrentBranchAsync()
        => GetCurrentBranchAsync(CancellationToken.None);

    /// <summary>
    /// Gets the name of the current branch asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing the current branch name.</returns>
    public async Task<CommandResult<string>> GetCurrentBranchAsync(CancellationToken cancellationToken)
    {
        var result = await _processRunner.RunAsync("git", "branch --show-current", null, cancellationToken);

        if (result.ExitCode != 0)
        {
            return CommandResult<string>.Failure(
                ErrorCodes.GitCommandFailed,
                "Failed to get current branch",
                result.StandardError);
        }

        var branchName = result.StandardOutput.Trim();
        if (string.IsNullOrEmpty(branchName))
        {
            return CommandResult<string>.Failure(
                ErrorCodes.GitCommandFailed,
                "Currently in detached HEAD state",
                "Cannot create worktree from detached HEAD. Please checkout a branch first.");
        }

        return CommandResult<string>.Success(branchName);
    }

    /// <summary>
    /// Checks whether a branch exists (local or remote) asynchronously. This overload does not accept a cancellation token.
    /// </summary>
    /// <param name="branchName">The name of the branch to check.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing <see langword="true"/> if the branch exists; otherwise, <see langword="false"/>.</returns>
    public Task<CommandResult<bool>> BranchExistsAsync(string branchName)
        => BranchExistsAsync(branchName, CancellationToken.None);

    /// <summary>
    /// Checks whether a branch exists (local or remote) asynchronously.
    /// </summary>
    /// <param name="branchName">The name of the branch to check.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing <see langword="true"/> if the branch exists; otherwise, <see langword="false"/>.</returns>
    public async Task<CommandResult<bool>> BranchExistsAsync(string branchName, CancellationToken cancellationToken)
    {
        var result = await _processRunner.RunAsync("git", $"rev-parse --verify {branchName}", null, cancellationToken);
        return CommandResult<bool>.Success(result.ExitCode == 0);
    }

    /// <summary>
    /// Creates a new branch from a base branch asynchronously. This overload does not accept a cancellation token.
    /// </summary>
    /// <param name="branchName">The name of the branch to create.</param>
    /// <param name="baseBranch">The base branch to branch from.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing information about the created branch.</returns>
    public Task<CommandResult<BranchInfo>> CreateBranchAsync(string branchName, string baseBranch)
        => CreateBranchAsync(branchName, baseBranch, CancellationToken.None);

    /// <summary>
    /// Creates a new branch from a base branch asynchronously.
    /// </summary>
    /// <param name="branchName">The name of the branch to create.</param>
    /// <param name="baseBranch">The base branch to branch from.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing information about the created branch.</returns>
    public async Task<CommandResult<BranchInfo>> CreateBranchAsync(string branchName, string baseBranch, CancellationToken cancellationToken)
    {
        var result = await _processRunner.RunAsync("git", $"branch {branchName} {baseBranch}", null, cancellationToken);

        if (result.ExitCode != 0)
        {
            if (result.StandardError.Contains("already exists"))
            {
                return CommandResult<BranchInfo>.Failure(
                    ErrorCodes.BranchAlreadyExists,
                    $"Branch '{branchName}' already exists",
                    result.StandardError);
            }

            return CommandResult<BranchInfo>.Failure(
                ErrorCodes.GitCommandFailed,
                "Failed to create branch",
                result.StandardError);
        }

        var branchInfo = new BranchInfo(branchName, baseBranch, true, false);
        return CommandResult<BranchInfo>.Success(branchInfo);
    }

    /// <summary>
    /// Adds a new worktree at the specified path asynchronously. This overload does not accept a cancellation token.
    /// </summary>
    /// <param name="path">The path where the worktree should be created.</param>
    /// <param name="branchName">The branch to checkout in the new worktree.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing <see langword="true"/> if the worktree was added successfully; otherwise, <see langword="false"/>.</returns>
    public Task<CommandResult<bool>> AddWorktreeAsync(string path, string branchName)
        => AddWorktreeAsync(path, branchName, CancellationToken.None);

    /// <summary>
    /// Adds a new worktree at the specified path asynchronously.
    /// </summary>
    /// <param name="path">The path where the worktree should be created.</param>
    /// <param name="branchName">The branch to checkout in the new worktree.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing <see langword="true"/> if the worktree was added successfully; otherwise, <see langword="false"/>.</returns>
    public async Task<CommandResult<bool>> AddWorktreeAsync(string path, string branchName, CancellationToken cancellationToken)
    {
        var result = await _processRunner.RunAsync("git", $"worktree add \"{path}\" {branchName}", null, cancellationToken);

        if (result.ExitCode != 0)
        {
            if (result.StandardError.Contains("already exists"))
            {
                return CommandResult<bool>.Failure(
                    ErrorCodes.WorktreeAlreadyExists,
                    $"Worktree at '{path}' already exists",
                    result.StandardError);
            }

            if (result.StandardError.Contains("is already checked out"))
            {
                return CommandResult<bool>.Failure(
                    ErrorCodes.BranchAlreadyInUse,
                    $"Branch '{branchName}' is already checked out in another worktree",
                    result.StandardError);
            }

            return CommandResult<bool>.Failure(
                ErrorCodes.GitCommandFailed,
                "Failed to add worktree",
                result.StandardError);
        }

        return CommandResult<bool>.Success(true);
    }

    /// <summary>
    /// Lists all worktrees in the repository asynchronously. This overload does not accept a cancellation token.
    /// </summary>
    /// <returns>A <see cref="CommandResult{T}"/> containing a list of worktree information.</returns>
    public Task<CommandResult<List<WorktreeInfo>>> ListWorktreesAsync()
        => ListWorktreesAsync(CancellationToken.None);

    /// <summary>
    /// Lists all worktrees in the repository asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing a list of worktree information.</returns>
    public async Task<CommandResult<List<WorktreeInfo>>> ListWorktreesAsync(CancellationToken cancellationToken)
    {
        var result = await _processRunner.RunAsync("git", "worktree list --porcelain", null, cancellationToken);

        if (result.ExitCode != 0)
        {
            return CommandResult<List<WorktreeInfo>>.Failure(
                ErrorCodes.GitCommandFailed,
                "Failed to list worktrees",
                result.StandardError);
        }

        var worktrees = ParseWorktreesFromPorcelain(result.StandardOutput);
        return CommandResult<List<WorktreeInfo>>.Success(worktrees);
    }

    /// <inheritdoc/>
    public Task<CommandResult<bool>> HasUncommittedChangesAsync(string worktreePath)
        => HasUncommittedChangesAsync(worktreePath, CancellationToken.None);

    /// <inheritdoc/>
    public async Task<CommandResult<bool>> HasUncommittedChangesAsync(string worktreePath, CancellationToken cancellationToken)
    {
        var result = await _processRunner.RunAsync("git", $"-C \"{worktreePath}\" status --porcelain", null, cancellationToken);

        if (result.ExitCode != 0)
        {
            return CommandResult<bool>.Failure(
                ErrorCodes.GitCommandFailed,
                $"Failed to check uncommitted changes in '{worktreePath}'",
                result.StandardError);
        }

        var lines = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var hasUncommittedChanges = lines.Any(line => !line.TrimStart().StartsWith("??"));
        return CommandResult<bool>.Success(hasUncommittedChanges);
    }

    /// <inheritdoc/>
    public Task<CommandResult<bool>> IsWorktreeLockedAsync(string worktreePath)
        => IsWorktreeLockedAsync(worktreePath, CancellationToken.None);

    /// <inheritdoc/>
    public async Task<CommandResult<bool>> IsWorktreeLockedAsync(string worktreePath, CancellationToken cancellationToken)
    {
        try
        {
            var gitDir = await GetMainGitDirAsync(cancellationToken);
            if (gitDir == null)
            {
                return CommandResult<bool>.Failure(
                    ErrorCodes.NotGitRepository,
                    "Could not determine git directory",
                    null);
            }

            var worktreeName = Path.GetFileName(worktreePath);
            var lockFilePath = Path.Combine(gitDir, "worktrees", worktreeName, "locked");
            var isLocked = _fileSystem.File.Exists(lockFilePath);
            return CommandResult<bool>.Success(isLocked);
        }
        catch (IOException ex)
        {
            return CommandResult<bool>.Failure(
                ErrorCodes.GitCommandFailed,
                $"Failed to check lock status for '{worktreePath}'",
                ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return CommandResult<bool>.Failure(
                ErrorCodes.GitCommandFailed,
                $"Access denied when checking lock status for '{worktreePath}'",
                ex.Message);
        }
    }

    /// <inheritdoc/>
    public Task<CommandResult<bool>> RemoveWorktreeAsync(string worktreePath, bool force)
        => RemoveWorktreeAsync(worktreePath, force, CancellationToken.None);

    /// <inheritdoc/>
    public async Task<CommandResult<bool>> RemoveWorktreeAsync(string worktreePath, bool force, CancellationToken cancellationToken)
    {
        var forceFlag = force ? "--force " : "";
        var result = await _processRunner.RunAsync("git", $"worktree remove {forceFlag}\"{worktreePath}\"", null, cancellationToken);

        if (result.ExitCode != 0)
        {
            if (result.StandardError.Contains("is not a working tree"))
            {
                return CommandResult<bool>.Failure(
                    "WT-RM-001",
                    $"Worktree at '{worktreePath}' not found",
                    "Use 'wt list' to see available worktrees");
            }

            if (result.StandardError.Contains("contains modified or untracked files"))
            {
                return CommandResult<bool>.Failure(
                    "WT-RM-003",
                    $"Worktree at '{worktreePath}' has uncommitted changes",
                    "Commit or stash changes, or use --force to override");
            }

            if (result.StandardError.Contains("is locked"))
            {
                return CommandResult<bool>.Failure(
                    "WT-RM-005",
                    $"Worktree at '{worktreePath}' is locked",
                    "Use --force to override lock, or wait for process to finish");
            }

            return CommandResult<bool>.Failure(
                ErrorCodes.GitCommandFailed,
                $"Failed to remove worktree at '{worktreePath}'",
                result.StandardError);
        }

        return CommandResult<bool>.Success(true);
    }

    private async Task<string?> GetMainGitDirAsync(CancellationToken cancellationToken)
    {
        var result = await _processRunner.RunAsync("git", "rev-parse --git-dir", null, cancellationToken);
        if (result.ExitCode != 0)
        {
            return null;
        }

        var gitDir = result.StandardOutput.Trim();
        if (string.IsNullOrEmpty(gitDir))
        {
            return null;
        }

        var fullPath = Path.GetFullPath(gitDir);
        if (fullPath.Contains("/worktrees/") || fullPath.Contains("\\worktrees\\"))
        {
            var worktreesIndex = fullPath.LastIndexOf("worktrees", StringComparison.OrdinalIgnoreCase);
            return fullPath.Substring(0, worktreesIndex - 1);
        }

        return fullPath;
    }

    private List<WorktreeInfo> ParseWorktreesFromPorcelain(string porcelainOutput)
    {
        var worktrees = new List<WorktreeInfo>();
        var lines = porcelainOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var currentWorktree = new WorktreeData();

        foreach (var line in lines)
        {
            if (line.StartsWith("worktree "))
            {
                AddWorktreeIfValid(worktrees, currentWorktree);
                currentWorktree = ParseWorktreeLine(line);
            }
            else
            {
                ParseWorktreeAttribute(line, ref currentWorktree);
            }
        }

        AddWorktreeIfValid(worktrees, currentWorktree);
        return worktrees;
    }

    private WorktreeData ParseWorktreeLine(string line)
    {
        if (string.IsNullOrEmpty(line) || line.Length <= 9)
        {
            return new WorktreeData();
        }

        var rawPath = line.Substring(9);
        // Normalize the path for consistent comparisons.
        // Note: Path.GetFullPath resolves relative segments, and on Windows it also
        // resolves certain symlinks/junctions. On Unix-like systems it does not fully
        // resolve symlinks (e.g., it may leave /var instead of /private/var on macOS).
        // Tests or callers that use GetRealPath will typically see more fully resolved
        // paths than this method returns, so any comparisons should account for this.
        var normalizedPath = Path.GetFullPath(rawPath);

        return new WorktreeData
        {
            Path = normalizedPath
        };
    }

    private void ParseWorktreeAttribute(string line, ref WorktreeData worktree)
    {
        if (line.StartsWith("HEAD "))
        {
            if (line.Length >= 5)
            {
                worktree.Head = line.Substring(5);
            }
        }
        else if (line.StartsWith("branch "))
        {
            if (line.Length >= 7)
            {
                worktree.Branch = NormalizeBranchName(line.Substring(7));
            }
        }
        else if (line.Trim() == "detached")
        {
            worktree.IsDetached = true;
        }
    }

    private string NormalizeBranchName(string branch)
    {
        return branch.StartsWith("refs/heads/") ? branch.Substring(11) : branch;
    }

    private void AddWorktreeIfValid(List<WorktreeInfo> worktrees, WorktreeData data)
    {
        if (data.Path != null && data.Head != null)
        {
            worktrees.Add(CreateWorktreeInfo(
                data.Path,
                data.Branch ?? data.Head,
                data.IsDetached,
                data.Head));
        }
    }

    private class WorktreeData
    {
        public string? Path { get; set; }
        public string? Head { get; set; }
        public string? Branch { get; set; }
        public bool IsDetached { get; set; }
    }

    private WorktreeInfo CreateWorktreeInfo(string path, string branch, bool isDetached, string commitHash)
    {
        var createdAt = GetWorktreeCreationTime(path);
        var exists = _fileSystem.Directory.Exists(path);
        return new WorktreeInfo(path, branch, isDetached, commitHash, createdAt, exists);
    }

    private DateTime GetWorktreeCreationTime(string path)
    {
        try
        {
            var gitDir = ResolveGitDirectory();
            var worktreeName = Path.GetFileName(path);
            var gitWorktreePath = Path.Combine(gitDir, "worktrees", worktreeName, "gitdir");

            if (_fileSystem.File.Exists(gitWorktreePath))
            {
                return _fileSystem.File.GetCreationTime(gitWorktreePath);
            }
        }
        catch (IOException ex)
        {
            System.Console.Error.WriteLine($"[GitService] Error reading creation time for worktree at '{path}': {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            System.Console.Error.WriteLine($"[GitService] Access denied when reading creation time for worktree at '{path}': {ex.Message}");
        }

        return DateTime.Now;
    }

    private string ResolveGitDirectory()
    {
        var gitDir = ".git";
        if (!_fileSystem.File.Exists(gitDir))
        {
            return gitDir;
        }

        try
        {
            var gitDirPath = ReadGitDirPath(gitDir);
            if (!string.IsNullOrWhiteSpace(gitDirPath) && IsValidGitDirectory(gitDirPath))
            {
                return gitDirPath;
            }
        }
        catch (IOException ex)
        {
            System.Console.Error.WriteLine($"[GitService] Error reading .git file: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            System.Console.Error.WriteLine($"[GitService] Access denied reading .git file: {ex.Message}");
        }

        return gitDir;
    }

    private string? ReadGitDirPath(string gitFilePath)
    {
        var lines = _fileSystem.File.ReadAllLines(gitFilePath);
        if (lines.Length == 0)
        {
            return null;
        }

        const string prefix = "gitdir:";
        var firstLine = lines[0];
        if (!firstLine.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var gitDirPath = firstLine.Substring(prefix.Length).Trim();
        return Path.IsPathRooted(gitDirPath)
            ? Path.GetFullPath(gitDirPath)
            : Path.GetFullPath(Path.Combine(_fileSystem.Directory.GetCurrentDirectory(), gitDirPath));
    }

    private bool IsValidGitDirectory(string gitDirPath)
    {
        return _fileSystem.Directory.Exists(gitDirPath) &&
               (_fileSystem.Directory.Exists(Path.Combine(gitDirPath, "worktrees")) ||
                _fileSystem.File.Exists(Path.Combine(gitDirPath, "config")));
    }
}

