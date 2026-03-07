namespace Kuju63.WorkTree.CommandLine.Models;

/// <summary>
/// Represents a remote tracking branch reference parsed from git branch -r output.
/// </summary>
/// <param name="RemoteName">The name of the remote (e.g., "origin").</param>
/// <param name="BranchName">The branch name (e.g., "feature/review-me").</param>
/// <param name="FullRef">The full reference string (e.g., "origin/feature/review-me").</param>
public record RemoteBranchInfo(string RemoteName, string BranchName, string FullRef);
