using Kuju63.WorkTree.CommandLine.Models;
using Shouldly;

namespace Kuju63.WorkTree.Tests.Models;

public class RemoteBranchInfoTests
{
    [Fact]
    public void RemoteBranchInfo_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var info = new RemoteBranchInfo("origin", "feature/my-branch", "origin/feature/my-branch");

        // Assert
        info.RemoteName.ShouldBe("origin");
        info.BranchName.ShouldBe("feature/my-branch");
        info.FullRef.ShouldBe("origin/feature/my-branch");
    }

    [Fact]
    public void RemoteBranchInfo_FullRef_ShouldBeRemoteSlashBranch()
    {
        // Arrange & Act
        var info = new RemoteBranchInfo("upstream", "main", "upstream/main");

        // Assert
        info.FullRef.ShouldBe("upstream/main");
    }

    [Fact]
    public void RemoteBranchInfo_WithSlashInBranchName_ShouldPreserveFullRef()
    {
        // Arrange & Act
        var info = new RemoteBranchInfo("origin", "feature/sub/deep", "origin/feature/sub/deep");

        // Assert
        info.RemoteName.ShouldBe("origin");
        info.BranchName.ShouldBe("feature/sub/deep");
        info.FullRef.ShouldBe("origin/feature/sub/deep");
    }

    [Fact]
    public void RemoteBranchInfo_Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var info1 = new RemoteBranchInfo("origin", "main", "origin/main");
        var info2 = new RemoteBranchInfo("origin", "main", "origin/main");

        // Assert
        info1.ShouldBe(info2);
        (info1 == info2).ShouldBeTrue();
    }

    [Fact]
    public void RemoteBranchInfo_Equality_DifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var info1 = new RemoteBranchInfo("origin", "main", "origin/main");
        var info2 = new RemoteBranchInfo("upstream", "main", "upstream/main");

        // Assert
        info1.ShouldNotBe(info2);
        (info1 == info2).ShouldBeFalse();
    }
}
