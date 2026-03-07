namespace Kuju63.WorkTree.CommandLine.Models;

/// <summary>
/// Defines error codes used throughout the application.
/// </summary>
public static class ErrorCodes
{
    // Git関連エラー (GIT001-003)
    public const string GitNotFound = "GIT001";
    public const string NotGitRepository = "GIT002";
    public const string GitCommandFailed = "GIT003";

    // ブランチ関連エラー (BR001-003)
    public const string InvalidBranchName = "BR001";
    public const string BranchAlreadyExists = "BR002";
    public const string BranchAlreadyInUse = "BR003";

    // Worktree関連エラー (WT001-002)
    public const string WorktreeAlreadyExists = "WT001";
    public const string WorktreeCreationFailed = "WT002";

    // ファイルシステム関連エラー (FS001-003)
    public const string InvalidPath = "FS001";
    public const string PathNotWritable = "FS002";
    public const string DiskSpaceLow = "FS003";

    // エディター関連エラー (ED001)
    public const string EditorNotFound = "ED001";

    // リモート関連エラー (RM001-003)
    public const string RemoteNotFound = "RM001";
    public const string RemoteFetchFailed = "RM002";
    public const string BranchNotFoundAnywhere = "RM003";

    // ユーザー操作 (USR001)
    public const string UserCancelled = "USR001";

    /// <summary>
    /// Gets a solution message for the specified error code.
    /// </summary>
    /// <param name="errorCode">The error code to get the solution for.</param>
    /// <returns>A string containing the recommended solution for the error.</returns>
    public static string GetSolution(string errorCode)
    {
        return errorCode switch
        {
            GitNotFound => "Install Git 2.5 or later and ensure it's in your PATH",
            NotGitRepository => "Navigate to a Git repository directory or initialize one with 'git init'",
            GitCommandFailed => "Check Git configuration and repository state",
            InvalidBranchName => "Use only alphanumeric characters, '-', '_', '/' in branch names",
            BranchAlreadyExists => "Use a different branch name or use --checkout-existing flag",
            BranchAlreadyInUse => "The branch is already checked out in another worktree. Choose a different branch or remove the other worktree",
            WorktreeAlreadyExists => "Remove the existing worktree or choose a different path",
            WorktreeCreationFailed => "Check file permissions and disk space",
            InvalidPath => "Provide a valid file system path",
            PathNotWritable => "Ensure you have write permissions to the target directory",
            DiskSpaceLow => "Free up disk space and try again",
            EditorNotFound => "Install the editor or specify a custom editor command",
            RemoteNotFound => "Check configured remotes with 'git remote -v' and specify the correct remote name with --remote flag",
            RemoteFetchFailed => "Check network connection and authentication, then run 'git fetch <remote>' manually",
            BranchNotFoundAnywhere => "Check branch list with 'git branch -a'. You can also retry with --fetch flag to update remote tracking refs",
            UserCancelled => "Operation was cancelled by the user",
            _ => "Unknown error occurred"
        };
    }
}
