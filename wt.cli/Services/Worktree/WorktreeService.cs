using Kuju63.WorkTree.CommandLine.Models;
using Kuju63.WorkTree.CommandLine.Services.Editor;
using Kuju63.WorkTree.CommandLine.Services.Git;
using Kuju63.WorkTree.CommandLine.Utils;

namespace Kuju63.WorkTree.CommandLine.Services.Worktree;

/// <summary>
/// Represents a unit type (void) for generic command results.
/// </summary>
internal sealed class Unit
{
    /// <summary>
    /// Gets the singleton instance of Unit.
    /// </summary>
    public static Unit Value => new();

    private Unit()
    {
    }
}

/// <summary>
/// Represents the result of path preparation.
/// </summary>
internal sealed class PathPrepareResult
{
    /// <summary>
    /// Gets a value indicating whether the path is valid.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Gets the error message if the path is invalid.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the prepared path if valid.
    /// </summary>
    public string Path { get; private set; } = string.Empty;

    /// <summary>
    /// Creates an invalid result with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>An invalid PathPrepareResult.</returns>
    public static PathPrepareResult Invalid(string errorMessage)
        => new() { IsValid = false, ErrorMessage = errorMessage };

    /// <summary>
    /// Creates a valid result with a path.
    /// </summary>
    /// <param name="path">The prepared path.</param>
    /// <returns>A valid PathPrepareResult.</returns>
    public static PathPrepareResult Valid(string path)
        => new() { IsValid = true, Path = path };
}

/// <summary>
/// Provides functionality for creating and managing Git worktrees.
/// </summary>
public class WorktreeService : IWorktreeService
{
    private readonly IGitService _gitService;
    private readonly IPathHelper _pathHelper;
    private readonly IEditorService? _editorService;

    public WorktreeService(IGitService gitService, IPathHelper pathHelper, IEditorService? editorService)
    {
        _gitService = gitService;
        _pathHelper = pathHelper;
        _editorService = editorService;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="WorktreeService"/> without an <see cref="IEditorService"/>.
    /// </summary>
    /// <param name="gitService">The <see cref="IGitService"/> instance.</param>
    /// <param name="pathHelper">The <see cref="IPathHelper"/> instance.</param>
    public WorktreeService(IGitService gitService, IPathHelper pathHelper)
        : this(gitService, pathHelper, null)
    {
    }

    /// <summary>
    /// Creates a new worktree with the specified options. This overload does not accept a cancellation token.
    /// </summary>
    /// <param name="options">The options for creating the worktree.</param>
    /// <returns>A <see cref="CommandResult{T}"/> representing the result.</returns>
    public Task<CommandResult<WorktreeInfo>> CreateWorktreeAsync(CreateWorktreeOptions options)
        => CreateWorktreeAsync(options, CancellationToken.None);

    /// <summary>
    /// Creates a new worktree with the specified options asynchronously.
    /// </summary>
    /// <param name="options">The options for creating the worktree.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> representing the result.</returns>
    public async Task<CommandResult<WorktreeInfo>> CreateWorktreeAsync(CreateWorktreeOptions options, CancellationToken cancellationToken)
    {
        // Validate options
        var validationResult = options.Validate();
        if (!validationResult.IsValid)
        {
            return CommandResult<WorktreeInfo>.Failure(
                ErrorCodes.InvalidBranchName,
                "Invalid options",
                validationResult.ErrorMessage);
        }

        // Check if in Git repository
        var isGitRepoResult = await _gitService.IsGitRepositoryAsync(cancellationToken);
        if (!isGitRepoResult.IsSuccess || !isGitRepoResult.Data)
        {
            return CommandResult<WorktreeInfo>.Failure(
                ErrorCodes.NotGitRepository,
                "Not in a Git repository",
                ErrorCodes.GetSolution(ErrorCodes.NotGitRepository));
        }

        return await CreateWorktreeInternalAsync(options, cancellationToken);
    }

    private async Task<CommandResult<WorktreeInfo>> CreateWorktreeInternalAsync(CreateWorktreeOptions options, CancellationToken cancellationToken)
    {
        var baseBranchResult = await GetBaseBranchAsync(options, cancellationToken);
        if (!baseBranchResult.IsSuccess)
        {
            return ToWorktreeFailure(baseBranchResult);
        }

        var branchResult = await EnsureBranchExistsAsync(options.BranchName, baseBranchResult.Data!, cancellationToken);
        if (!branchResult.IsSuccess)
        {
            return ToWorktreeFailure(branchResult);
        }

        var pathResult = PrepareWorktreePath(options);
        if (!pathResult.IsValid)
        {
            return CommandResult<WorktreeInfo>.Failure(ErrorCodes.InvalidPath, "Invalid worktree path", pathResult.ErrorMessage);
        }

        var addWorktreeResult = await _gitService.AddWorktreeAsync(pathResult.Path, options.BranchName, cancellationToken);
        if (!addWorktreeResult.IsSuccess)
        {
            return CommandResult<WorktreeInfo>.Failure(addWorktreeResult.ErrorCode!, addWorktreeResult.ErrorMessage!, addWorktreeResult.Solution);
        }

        return await CreateAndLaunchWorktreeAsync(options, pathResult.Path, cancellationToken);
    }

    private CommandResult<WorktreeInfo> ToWorktreeFailure<T>(CommandResult<T> result)
    {
        return CommandResult<WorktreeInfo>.Failure(
            result.ErrorCode ?? ErrorCodes.GitCommandFailed,
            result.ErrorMessage ?? "Operation failed",
            result.Solution);
    }

    private async Task<CommandResult<WorktreeInfo>> CreateAndLaunchWorktreeAsync(
        CreateWorktreeOptions options,
        string normalizedPath,
        CancellationToken cancellationToken)
    {
        var worktreeInfo = new WorktreeInfo(
            normalizedPath,
            options.BranchName,
            false, // IsDetached - newly created worktrees are never detached
            string.Empty, // CommitHash - not needed for create command
            DateTime.UtcNow,
            true); // Exists - just created, so it exists

        return await LaunchEditorIfSpecifiedAsync(options, worktreeInfo, cancellationToken);
    }

    private async Task<CommandResult<string>> GetBaseBranchAsync(CreateWorktreeOptions options, CancellationToken cancellationToken)
    {
        var baseBranch = options.BaseBranch;
        if (!string.IsNullOrEmpty(baseBranch))
        {
            return CommandResult<string>.Success(baseBranch);
        }

        var currentBranchResult = await _gitService.GetCurrentBranchAsync(cancellationToken);
        if (!currentBranchResult.IsSuccess)
        {
            return CommandResult<string>.Failure(
                currentBranchResult.ErrorCode!,
                currentBranchResult.ErrorMessage!,
                currentBranchResult.Solution);
        }

        return CommandResult<string>.Success(currentBranchResult.Data ?? string.Empty);
    }

    private async Task<CommandResult<Unit>> EnsureBranchExistsAsync(string branchName, string baseBranch, CancellationToken cancellationToken)
    {
        var branchExistsResult = await _gitService.BranchExistsAsync(branchName, cancellationToken);
        if (!branchExistsResult.IsSuccess)
        {
            return CommandResult<Unit>.Failure(
                branchExistsResult.ErrorCode!,
                branchExistsResult.ErrorMessage!,
                branchExistsResult.Solution);
        }

        if (branchExistsResult.Data)
        {
            return CommandResult<Unit>.Failure(
                ErrorCodes.BranchAlreadyExists,
                $"Branch '{branchName}' already exists",
                ErrorCodes.GetSolution(ErrorCodes.BranchAlreadyExists));
        }

        var createBranchResult = await _gitService.CreateBranchAsync(branchName, baseBranch, cancellationToken);
        if (!createBranchResult.IsSuccess)
        {
            return CommandResult<Unit>.Failure(
                createBranchResult.ErrorCode!,
                createBranchResult.ErrorMessage!,
                createBranchResult.Solution);
        }

        return CommandResult<Unit>.Success(Unit.Value);
    }

    private PathPrepareResult PrepareWorktreePath(CreateWorktreeOptions options)
    {
        var worktreePath = options.WorktreePath ?? $"../wt-{options.BranchName}";
        var resolvedPath = _pathHelper.ResolvePath(worktreePath, Environment.CurrentDirectory);
        var normalizedPath = _pathHelper.NormalizePath(resolvedPath);

        var pathValidation = _pathHelper.ValidatePath(normalizedPath);
        if (!pathValidation.IsValid)
        {
            return PathPrepareResult.Invalid(pathValidation.ErrorMessage ?? "Invalid path");
        }

        try
        {
            _pathHelper.EnsureParentDirectoryExists(normalizedPath);
        }
        catch (Exception ex)
        {
            return PathPrepareResult.Invalid(ex.Message ?? "Failed to create parent directory");
        }

        return PathPrepareResult.Valid(normalizedPath);
    }

    private async Task<CommandResult<WorktreeInfo>> LaunchEditorIfSpecifiedAsync(
        CreateWorktreeOptions options,
        WorktreeInfo worktreeInfo,
        CancellationToken cancellationToken)
    {
        if (!options.EditorType.HasValue || _editorService == null)
        {
            return CommandResult<WorktreeInfo>.Success(worktreeInfo);
        }

        var editorResult = await _editorService.LaunchEditorAsync(
            worktreeInfo.Path,
            options.EditorType.Value,
            cancellationToken);

        if (!editorResult.IsSuccess)
        {
            return CommandResult<WorktreeInfo>.Success(
                worktreeInfo,
                new List<string> { $"Warning: {editorResult.ErrorMessage ?? "Failed to launch editor"}" });
        }

        return CommandResult<WorktreeInfo>.Success(worktreeInfo);
    }

    /// <summary>
    /// Lists all worktrees in the repository. This overload does not accept a cancellation token.
    /// </summary>
    /// <returns>A <see cref="CommandResult{T}"/> containing a list of worktree information, sorted by creation date (newest first).</returns>
    public Task<CommandResult<List<WorktreeInfo>>> ListWorktreesAsync()
        => ListWorktreesAsync(CancellationToken.None);

    /// <summary>
    /// Lists all worktrees in the repository asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing a list of worktree information, sorted by creation date (newest first).</returns>
    public async Task<CommandResult<List<WorktreeInfo>>> ListWorktreesAsync(CancellationToken cancellationToken)
    {
        // Check if in Git repository
        var isGitRepoResult = await _gitService.IsGitRepositoryAsync(cancellationToken);
        if (!isGitRepoResult.IsSuccess || !isGitRepoResult.Data)
        {
            return CommandResult<List<WorktreeInfo>>.Failure(
                ErrorCodes.NotGitRepository,
                "Not in a Git repository",
                ErrorCodes.GetSolution(ErrorCodes.NotGitRepository));
        }

        // Get worktree list from Git
        var listResult = await _gitService.ListWorktreesAsync(cancellationToken);
        if (!listResult.IsSuccess)
        {
            return listResult;
        }

        // Sort by creation date (newest first)
        var sortedWorktrees = listResult.Data!
            .OrderByDescending(w => w.CreatedAt)
            .ToList();

        return CommandResult<List<WorktreeInfo>>.Success(sortedWorktrees);
    }

    /// <inheritdoc/>
    public Task<RemovalValidationError> ValidateForRemovalAsync(string worktreeIdentifier, bool force)
        => ValidateForRemovalAsync(worktreeIdentifier, force, CancellationToken.None);

    /// <inheritdoc/>
    public async Task<RemovalValidationError> ValidateForRemovalAsync(string worktreeIdentifier, bool force, CancellationToken cancellationToken)
    {
        var listResult = await _gitService.ListWorktreesAsync(cancellationToken);
        if (!listResult.IsSuccess || listResult.Data == null)
        {
            return RemovalValidationError.NotFound;
        }

        var worktrees = listResult.Data;
        var targetWorktree = FindWorktree(worktrees, worktreeIdentifier);

        if (targetWorktree == null)
        {
            return RemovalValidationError.NotFound;
        }

        if (IsMainWorktree(worktrees, targetWorktree))
        {
            return RemovalValidationError.IsMainWorktree;
        }

        if (IsCurrentWorktree(targetWorktree))
        {
            return RemovalValidationError.IsCurrentWorktree;
        }

        if (!force)
        {
            return await ValidateWorktreeStateAsync(targetWorktree, cancellationToken);
        }

        return RemovalValidationError.None;
    }

    private async Task<RemovalValidationError> ValidateWorktreeStateAsync(WorktreeInfo targetWorktree, CancellationToken cancellationToken)
    {
        var hasChangesResult = await _gitService.HasUncommittedChangesAsync(targetWorktree.Path, cancellationToken);
        if (hasChangesResult.IsSuccess && hasChangesResult.Data)
        {
            return RemovalValidationError.HasUncommittedChanges;
        }

        var isLockedResult = await _gitService.IsWorktreeLockedAsync(targetWorktree.Path, cancellationToken);
        if (isLockedResult.IsSuccess && isLockedResult.Data)
        {
            return RemovalValidationError.IsLocked;
        }

        return RemovalValidationError.None;
    }

    /// <inheritdoc/>
    public Task<CommandResult<RemoveWorktreeResult>> RemoveWorktreeAsync(RemoveWorktreeOptions options)
        => RemoveWorktreeAsync(options, CancellationToken.None);

    /// <inheritdoc/>
    public async Task<CommandResult<RemoveWorktreeResult>> RemoveWorktreeAsync(RemoveWorktreeOptions options, CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var listResult = await _gitService.ListWorktreesAsync(cancellationToken);
        if (!listResult.IsSuccess || listResult.Data == null)
        {
            return CommandResult<RemoveWorktreeResult>.Failure(
                "WT-RM-001",
                $"Worktree '{options.WorktreeIdentifier}' not found",
                "Use 'wt list' to see available worktrees");
        }

        var worktrees = listResult.Data;
        var targetWorktree = FindWorktree(worktrees, options.WorktreeIdentifier);

        if (targetWorktree == null)
        {
            return CommandResult<RemoveWorktreeResult>.Failure(
                "WT-RM-001",
                $"Worktree '{options.WorktreeIdentifier}' not found",
                "Use 'wt list' to see available worktrees");
        }

        var validation = await ValidateForRemovalAsync(options.WorktreeIdentifier, options.Force, cancellationToken);
        if (validation != RemovalValidationError.None)
        {
            return CreateValidationErrorResult(validation, options.WorktreeIdentifier);
        }

        var removeResult = await _gitService.RemoveWorktreeAsync(targetWorktree.Path, options.Force, cancellationToken);
        if (!removeResult.IsSuccess)
        {
            return CommandResult<RemoveWorktreeResult>.Failure(
                removeResult.ErrorCode!,
                removeResult.ErrorMessage!,
                removeResult.Solution);
        }

        sw.Stop();
        var result = new RemoveWorktreeResult
        {
            Success = true,
            WorktreeId = options.WorktreeIdentifier,
            RemovedPath = targetWorktree.Path,
            WorktreeMetadataRemoved = true,
            FilesDeleted = 0,
            DeletionFailures = [],
            Duration = sw.Elapsed
        };

        return CommandResult<RemoveWorktreeResult>.Success(result);
    }

    private static WorktreeInfo? FindWorktree(List<WorktreeInfo> worktrees, string identifier)
    {
        return worktrees.FirstOrDefault(w =>
            w.Branch.Equals(identifier, StringComparison.OrdinalIgnoreCase) ||
            w.Path.Equals(identifier, StringComparison.OrdinalIgnoreCase) ||
            w.Path.EndsWith(identifier, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsMainWorktree(List<WorktreeInfo> worktrees, WorktreeInfo targetWorktree)
    {
        var mainWorktree = worktrees.FirstOrDefault();
        return mainWorktree != null && mainWorktree.Path == targetWorktree.Path;
    }

    private static bool IsCurrentWorktree(WorktreeInfo targetWorktree)
    {
        var currentDir = Environment.CurrentDirectory;
        return currentDir.StartsWith(targetWorktree.Path, StringComparison.OrdinalIgnoreCase) ||
               targetWorktree.Path.Equals(currentDir, StringComparison.OrdinalIgnoreCase);
    }

    private static CommandResult<RemoveWorktreeResult> CreateValidationErrorResult(RemovalValidationError error, string worktreeId)
    {
        return error switch
        {
            RemovalValidationError.NotFound => CommandResult<RemoveWorktreeResult>.Failure(
                "WT-RM-001",
                $"Worktree '{worktreeId}' not found",
                "Use 'wt list' to see available worktrees"),
            RemovalValidationError.IsMainWorktree => CommandResult<RemoveWorktreeResult>.Failure(
                "WT-RM-002",
                "Cannot remove main worktree",
                "The main working directory is protected from deletion"),
            RemovalValidationError.IsCurrentWorktree => CommandResult<RemoveWorktreeResult>.Failure(
                "WT-RM-002",
                "Cannot remove the currently checked-out worktree",
                "Switch to a different directory and try again"),
            RemovalValidationError.HasUncommittedChanges => CommandResult<RemoveWorktreeResult>.Failure(
                "WT-RM-003",
                $"Worktree '{worktreeId}' has uncommitted changes",
                "Commit or stash changes, or use --force to override"),
            RemovalValidationError.IsLocked => CommandResult<RemoveWorktreeResult>.Failure(
                "WT-RM-005",
                $"Worktree '{worktreeId}' is locked",
                "Use --force to override lock, or wait for process to finish"),
            _ => CommandResult<RemoveWorktreeResult>.Failure(
                "WT-RM-099",
                "Unknown validation error",
                null)
        };
    }
}
