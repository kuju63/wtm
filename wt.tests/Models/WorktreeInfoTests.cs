using Kuju63.WorkTree.CommandLine.Models;
using Shouldly;

namespace Kuju63.WorkTree.Tests.Models;

public class WorktreeInfoTests
{
    [Fact]
    public void WorktreeInfo_WithValidProperties_ShouldCreate()
    {
        // Arrange
        var path = "/Users/dev/worktrees/feature-x";
        var branch = "feature-x";
        var isDetached = false;
        var commitHash = "abc1234567890abcdef1234567890abcdef1234";
        var createdAt = DateTime.UtcNow;
        var exists = true;

        // Act
        var info = new WorktreeInfo(path, branch, isDetached, commitHash, createdAt, exists);

        // Assert
        info.Path.ShouldBe(path);
        info.Branch.ShouldBe(branch);
        info.IsDetached.ShouldBe(isDetached);
        info.CommitHash.ShouldBe(commitHash);
        info.CreatedAt.ShouldBe(createdAt);
        info.Exists.ShouldBe(exists);
    }

    [Fact]
    public void WorktreeInfo_WithRecordEquality_ShouldCompareCorrectly()
    {
        // Arrange
        var createdAt = DateTime.UtcNow;
        var info1 = new WorktreeInfo("/path/to/worktree", "feature-x", false, "abc123", createdAt, true);
        var info2 = new WorktreeInfo("/path/to/worktree", "feature-x", false, "abc123", createdAt, true);
        var info3 = new WorktreeInfo("/different/path", "feature-x", false, "abc123", createdAt, true);

        // Assert
        info1.ShouldBe(info2);
        info1.ShouldNotBe(info3);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void WorktreeInfo_WithEmptyPath_ShouldAllowCreation(string? path)
    {
        // Act
        var info = new WorktreeInfo(path!, "branch", false, "abc123", DateTime.UtcNow, true);

        // Assert
        info.Path.ShouldBe(path);
    }

    [Fact]
    public void GetDisplayBranch_NormalBranch_ReturnsBranchName()
    {
        // Arrange
        var info = new WorktreeInfo("/path/to/worktree", "feature-x", false, "abc1234567890abcdef", DateTime.UtcNow, true);

        // Act
        var result = info.GetDisplayBranch();

        // Assert
        result.ShouldBe("feature-x");
    }

    [Fact]
    public void GetDisplayBranch_DetachedHead_ReturnsShortHashWithLabel()
    {
        // Arrange
        var info = new WorktreeInfo("/path/to/worktree", "abc1234567890abcdef", true, "abc1234567890abcdef", DateTime.UtcNow, true);

        // Act
        var result = info.GetDisplayBranch();

        // Assert
        result.ShouldBe("abc1234 (detached)");
    }

    [Fact]
    public void GetDisplayStatus_ExistingWorktree_ReturnsActive()
    {
        // Arrange
        var info = new WorktreeInfo("/path/to/worktree", "feature-x", false, "abc123", DateTime.UtcNow, true);

        // Act
        var result = info.GetDisplayStatus();

        // Assert
        result.ShouldBe("active");
    }

    [Fact]
    public void GetDisplayStatus_MissingWorktree_ReturnsMissing()
    {
        // Arrange
        var info = new WorktreeInfo("/path/to/worktree", "feature-x", false, "abc123", DateTime.UtcNow, false);

        // Act
        var result = info.GetDisplayStatus();

        // Assert
        result.ShouldBe("missing");
    }

    [Fact]
    public void WorktreeInfo_Remote_DefaultsToNull()
    {
        // Arrange & Act
        var info = new WorktreeInfo("/path", "feature-x", false, "abc123", DateTime.UtcNow, true);

        // Assert
        info.Remote.ShouldBeNull();
    }

    [Fact]
    public void WorktreeInfo_Remote_CanBeSetViaInitializer()
    {
        // Arrange & Act
        var info = new WorktreeInfo("/path", "feature-x", false, "abc123", DateTime.UtcNow, true)
        { Remote = "origin" };

        // Assert
        info.Remote.ShouldBe("origin");
    }
}
