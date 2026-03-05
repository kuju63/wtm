using Kuju63.WorkTree.CommandLine.Models;
using Kuju63.WorkTree.CommandLine.Services.Editor;
using Kuju63.WorkTree.CommandLine.Services.Git;
using Kuju63.WorkTree.CommandLine.Services.Interaction;
using Kuju63.WorkTree.CommandLine.Services.Worktree;
using Kuju63.WorkTree.CommandLine.Utils;
using Moq;
using Shouldly;

namespace Kuju63.WorkTree.Tests.Services.Worktree;

public class WorktreeServiceCheckoutTests
{
    private readonly Mock<IGitService> _mockGitService;
    private readonly Mock<IPathHelper> _mockPathHelper;
    private readonly Mock<IEditorService> _mockEditorService;
    private readonly Mock<IInteractionService> _mockInteractionService;
    private readonly WorktreeService _worktreeService;

    public WorktreeServiceCheckoutTests()
    {
        _mockGitService = new Mock<IGitService>();
        _mockPathHelper = new Mock<IPathHelper>();
        _mockEditorService = new Mock<IEditorService>();
        _mockInteractionService = new Mock<IInteractionService>();

        _worktreeService = new WorktreeService(
            _mockGitService.Object,
            _mockPathHelper.Object,
            _mockEditorService.Object);

        SetupDefaultPathHelper();
    }

    private void SetupDefaultPathHelper()
    {
        _mockPathHelper
            .Setup(p => p.ResolvePath(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((path, _) => $"/resolved{path}");
        _mockPathHelper
            .Setup(p => p.NormalizePath(It.IsAny<string>()))
            .Returns<string>(p => p);
        _mockPathHelper
            .Setup(p => p.ValidatePath(It.IsAny<string>()))
            .Returns(new PathValidationResult(true));
        _mockPathHelper
            .Setup(p => p.EnsureParentDirectoryExists(It.IsAny<string>()));
    }

    private void SetupGitRepo(bool isRepo = true)
    {
        _mockGitService
            .Setup(g => g.IsGitRepositoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(isRepo));
    }

    private void SetupLocalBranchExists(string branchName, bool exists)
    {
        _mockGitService
            .Setup(g => g.BranchExistsAsync(branchName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(exists));
    }

    private void SetupListWorktrees(List<WorktreeInfo>? worktrees = null)
    {
        _mockGitService
            .Setup(g => g.ListWorktreesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Success(worktrees ?? new List<WorktreeInfo>()));
    }

    private void SetupAddWorktree(bool success = true)
    {
        _mockGitService
            .Setup(g => g.AddWorktreeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success
                ? CommandResult<bool>.Success(true)
                : CommandResult<bool>.Failure(ErrorCodes.WorktreeCreationFailed, "Failed"));
    }

    private void SetupRemoteTrackingBranches(string? branchName, List<RemoteBranchInfo> branches)
    {
        _mockGitService
            .Setup(g => g.GetRemoteTrackingBranchesAsync(branchName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<IReadOnlyList<RemoteBranchInfo>>.Success(branches));
    }

    private void SetupAddWorktreeFromRemote(bool success = true, string? errorCode = null)
    {
        if (success)
        {
            _mockGitService
                .Setup(g => g.AddWorktreeFromRemoteAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CommandResult<Unit>.Success(Unit.Value));
        }
        else
        {
            _mockGitService
                .Setup(g => g.AddWorktreeFromRemoteAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CommandResult<Unit>.Failure(
                    errorCode ?? ErrorCodes.WorktreeCreationFailed,
                    "Failed to add worktree from remote"));
        }
    }

    // ============================================================
    // US1: Local Branch Checkout
    // ============================================================

    [Fact]
    public async Task CheckoutWorktreeAsync_LocalBranchExists_ReturnsSuccess()
    {
        // Arrange
        SetupGitRepo();
        SetupLocalBranchExists("feature/review-me", true);
        SetupListWorktrees();
        SetupAddWorktree();

        var options = new CheckoutWorktreeOptions { BranchName = "feature/review-me" };

        // Act
        var result = await _worktreeService.CheckoutWorktreeAsync(
            options, _mockInteractionService.Object);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data.Branch.ShouldBe("feature/review-me");
    }

    [Fact]
    public async Task CheckoutWorktreeAsync_BranchAlreadyCheckedOut_ReturnsBR002Error()
    {
        // Arrange
        var existingPath = "/existing/path/feature-review-me";
        SetupGitRepo();
        SetupLocalBranchExists("feature/review-me", true);
        SetupListWorktrees(new List<WorktreeInfo>
        {
            new(existingPath, "feature/review-me", false, "abc123", DateTime.UtcNow, true)
        });

        var options = new CheckoutWorktreeOptions { BranchName = "feature/review-me" };

        // Act
        var result = await _worktreeService.CheckoutWorktreeAsync(
            options, _mockInteractionService.Object);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.BranchAlreadyInUse);
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain(existingPath);
    }

    [Fact]
    public async Task CheckoutWorktreeAsync_TargetPathAlreadyExists_ReturnsWT001Error()
    {
        // Arrange
        SetupGitRepo();
        SetupLocalBranchExists("feature/review-me", true);
        SetupListWorktrees();

        _mockPathHelper
            .Setup(p => p.ValidatePath(It.IsAny<string>()))
            .Returns(new PathValidationResult(false, "Path already exists"));

        var options = new CheckoutWorktreeOptions { BranchName = "feature/review-me" };

        // Act
        var result = await _worktreeService.CheckoutWorktreeAsync(
            options, _mockInteractionService.Object);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.WorktreeAlreadyExists);
    }

    [Fact]
    public async Task CheckoutWorktreeAsync_NotGitRepository_ReturnsGIT002Error()
    {
        // Arrange
        SetupGitRepo(false);

        var options = new CheckoutWorktreeOptions { BranchName = "feature/review-me" };

        // Act
        var result = await _worktreeService.CheckoutWorktreeAsync(
            options, _mockInteractionService.Object);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.NotGitRepository);
    }

    // ============================================================
    // US2: Remote Branch Checkout (single remote)
    // ============================================================

    [Fact]
    public async Task CheckoutWorktreeAsync_LocalAbsentSingleRemote_AutoSelectsAndCreates()
    {
        // Arrange
        SetupGitRepo();
        SetupLocalBranchExists("feature/remote-only", false);
        SetupRemoteTrackingBranches("feature/remote-only", new List<RemoteBranchInfo>
        {
            new("origin", "feature/remote-only", "origin/feature/remote-only")
        });
        SetupListWorktrees();
        SetupAddWorktreeFromRemote();

        var options = new CheckoutWorktreeOptions { BranchName = "feature/remote-only" };

        // Act
        var result = await _worktreeService.CheckoutWorktreeAsync(
            options, _mockInteractionService.Object);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _mockGitService.Verify(
            g => g.AddWorktreeFromRemoteAsync(
                It.IsAny<string>(), "feature/remote-only", "origin", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckoutWorktreeAsync_LocalAbsentNoRemotes_ReturnsRM003()
    {
        // Arrange
        SetupGitRepo();
        SetupLocalBranchExists("feature/nonexistent", false);
        SetupRemoteTrackingBranches("feature/nonexistent", new List<RemoteBranchInfo>());

        var options = new CheckoutWorktreeOptions { BranchName = "feature/nonexistent" };

        // Act
        var result = await _worktreeService.CheckoutWorktreeAsync(
            options, _mockInteractionService.Object);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.BranchNotFoundAnywhere);
    }

    [Fact]
    public async Task CheckoutWorktreeAsync_FetchFails_ReturnsRM002()
    {
        // Arrange
        SetupGitRepo();
        SetupLocalBranchExists("feature/remote-only", false);

        _mockGitService
            .Setup(g => g.GetRemotesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<IReadOnlyList<string>>.Success(new List<string> { "origin" }));
        _mockGitService
            .Setup(g => g.FetchFromRemoteAsync("origin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<Unit>.Failure(
                ErrorCodes.RemoteFetchFailed, "Network error",
                ErrorCodes.GetSolution(ErrorCodes.RemoteFetchFailed)));

        var options = new CheckoutWorktreeOptions { BranchName = "feature/remote-only", Fetch = true };

        // Act
        var result = await _worktreeService.CheckoutWorktreeAsync(
            options, _mockInteractionService.Object);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.RemoteFetchFailed);
    }

    [Fact]
    public async Task CheckoutWorktreeAsync_BranchNotFoundOnAnyRemote_ReturnsRM003()
    {
        // Arrange
        SetupGitRepo();
        SetupLocalBranchExists("feature/missing", false);
        SetupRemoteTrackingBranches("feature/missing", new List<RemoteBranchInfo>());

        var options = new CheckoutWorktreeOptions { BranchName = "feature/missing" };

        // Act
        var result = await _worktreeService.CheckoutWorktreeAsync(
            options, _mockInteractionService.Object);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.BranchNotFoundAnywhere);
    }

    // ============================================================
    // US2: --fetch flag scenarios
    // ============================================================

    [Fact]
    public async Task CheckoutWorktreeAsync_LocalBranchWithFetchAndUpstream_FetchesThenCreates()
    {
        // Arrange
        SetupGitRepo();
        SetupLocalBranchExists("feature/local", true);
        SetupListWorktrees();
        SetupAddWorktree();

        _mockGitService
            .Setup(g => g.GetBranchUpstreamRemoteAsync("feature/local", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<string?>.Success("origin"));
        _mockGitService
            .Setup(g => g.FetchFromRemoteAsync("origin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<Unit>.Success(Unit.Value));

        var options = new CheckoutWorktreeOptions { BranchName = "feature/local", Fetch = true };

        // Act
        var result = await _worktreeService.CheckoutWorktreeAsync(
            options, _mockInteractionService.Object);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _mockGitService.Verify(
            g => g.FetchFromRemoteAsync("origin", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckoutWorktreeAsync_LocalBranchWithFetchNoUpstream_ProceedsWithWarning()
    {
        // Arrange
        SetupGitRepo();
        SetupLocalBranchExists("feature/local", true);
        SetupListWorktrees();
        SetupAddWorktree();

        _mockGitService
            .Setup(g => g.GetBranchUpstreamRemoteAsync("feature/local", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<string?>.Success(null));

        var options = new CheckoutWorktreeOptions { BranchName = "feature/local", Fetch = true };

        // Act
        var result = await _worktreeService.CheckoutWorktreeAsync(
            options, _mockInteractionService.Object);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _mockGitService.Verify(
            g => g.FetchFromRemoteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckoutWorktreeAsync_LocalAbsentWithFetch_FetchesAllRemotesThenSearches()
    {
        // Arrange
        SetupGitRepo();
        SetupLocalBranchExists("feature/remote-only", false);

        _mockGitService
            .Setup(g => g.GetRemotesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<IReadOnlyList<string>>.Success(new List<string> { "origin", "upstream" }));
        _mockGitService
            .Setup(g => g.FetchFromRemoteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<Unit>.Success(Unit.Value));
        SetupRemoteTrackingBranches("feature/remote-only", new List<RemoteBranchInfo>
        {
            new("origin", "feature/remote-only", "origin/feature/remote-only")
        });
        SetupListWorktrees();
        SetupAddWorktreeFromRemote();

        var options = new CheckoutWorktreeOptions { BranchName = "feature/remote-only", Fetch = true };

        // Act
        var result = await _worktreeService.CheckoutWorktreeAsync(
            options, _mockInteractionService.Object);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _mockGitService.Verify(
            g => g.FetchFromRemoteAsync("origin", It.IsAny<CancellationToken>()),
            Times.Once);
        _mockGitService.Verify(
            g => g.FetchFromRemoteAsync("upstream", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ============================================================
    // US3: Multi-remote selection
    // ============================================================

    [Fact]
    public async Task CheckoutWorktreeAsync_MultipleRemotes_CallsInteractionService()
    {
        // Arrange
        SetupGitRepo();
        SetupLocalBranchExists("feature/shared", false);
        SetupRemoteTrackingBranches("feature/shared", new List<RemoteBranchInfo>
        {
            new("origin", "feature/shared", "origin/feature/shared"),
            new("upstream", "feature/shared", "upstream/feature/shared")
        });

        _mockInteractionService
            .Setup(i => i.SelectAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        SetupListWorktrees();
        SetupAddWorktreeFromRemote();

        var options = new CheckoutWorktreeOptions { BranchName = "feature/shared" };

        // Act
        var result = await _worktreeService.CheckoutWorktreeAsync(
            options, _mockInteractionService.Object);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _mockInteractionService.Verify(
            i => i.SelectAsync(
                It.IsAny<string>(),
                It.Is<IReadOnlyList<string>>(l => l.Contains("origin") && l.Contains("upstream")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckoutWorktreeAsync_MultipleRemotes_UserSelectsOrigin_CreatesFromOrigin()
    {
        // Arrange
        SetupGitRepo();
        SetupLocalBranchExists("feature/shared", false);
        SetupRemoteTrackingBranches("feature/shared", new List<RemoteBranchInfo>
        {
            new("origin", "feature/shared", "origin/feature/shared"),
            new("upstream", "feature/shared", "upstream/feature/shared")
        });

        _mockInteractionService
            .Setup(i => i.SelectAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0); // Select "origin" (index 0)

        SetupListWorktrees();
        SetupAddWorktreeFromRemote();

        var options = new CheckoutWorktreeOptions { BranchName = "feature/shared" };

        // Act
        var result = await _worktreeService.CheckoutWorktreeAsync(
            options, _mockInteractionService.Object);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _mockGitService.Verify(
            g => g.AddWorktreeFromRemoteAsync(
                It.IsAny<string>(), "feature/shared", "origin", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckoutWorktreeAsync_MultipleRemotes_UserCancels_ReturnsCleanExit()
    {
        // Arrange
        SetupGitRepo();
        SetupLocalBranchExists("feature/shared", false);
        SetupRemoteTrackingBranches("feature/shared", new List<RemoteBranchInfo>
        {
            new("origin", "feature/shared", "origin/feature/shared"),
            new("upstream", "feature/shared", "upstream/feature/shared")
        });

        _mockInteractionService
            .Setup(i => i.SelectAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        var options = new CheckoutWorktreeOptions { BranchName = "feature/shared" };

        // Act
        var result = await _worktreeService.CheckoutWorktreeAsync(
            options, _mockInteractionService.Object);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        _mockGitService.Verify(
            g => g.AddWorktreeFromRemoteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckoutWorktreeAsync_MultipleRemotes_RemoteSpecified_SkipsPrompt()
    {
        // Arrange
        SetupGitRepo();
        SetupLocalBranchExists("feature/shared", false);
        SetupRemoteTrackingBranches("feature/shared", new List<RemoteBranchInfo>
        {
            new("origin", "feature/shared", "origin/feature/shared"),
            new("upstream", "feature/shared", "upstream/feature/shared")
        });
        SetupListWorktrees();
        SetupAddWorktreeFromRemote();

        var options = new CheckoutWorktreeOptions { BranchName = "feature/shared", Remote = "upstream" };

        // Act
        var result = await _worktreeService.CheckoutWorktreeAsync(
            options, _mockInteractionService.Object);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _mockInteractionService.Verify(
            i => i.SelectAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockGitService.Verify(
            g => g.AddWorktreeFromRemoteAsync(
                It.IsAny<string>(), "feature/shared", "upstream", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckoutWorktreeAsync_NonexistentRemoteSpecified_ReturnsRM001()
    {
        // Arrange
        SetupGitRepo();
        SetupLocalBranchExists("feature/shared", false);
        SetupRemoteTrackingBranches("feature/shared", new List<RemoteBranchInfo>
        {
            new("origin", "feature/shared", "origin/feature/shared"),
        });

        var options = new CheckoutWorktreeOptions { BranchName = "feature/shared", Remote = "typo" };

        // Act
        var result = await _worktreeService.CheckoutWorktreeAsync(
            options, _mockInteractionService.Object);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.RemoteNotFound);
    }

    // ============================================================
    // Edge cases (T039)
    // ============================================================

    [Fact]
    public async Task CheckoutWorktreeAsync_BranchNameWithSlash_PathResolvedCorrectly()
    {
        // Arrange
        SetupGitRepo();
        SetupLocalBranchExists("feature/sub/deep", true);
        SetupListWorktrees();
        SetupAddWorktree();

        string? capturedPath = null;
        _mockGitService
            .Setup(g => g.AddWorktreeAsync(It.IsAny<string>(), "feature/sub/deep", It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((path, _, _) => capturedPath = path)
            .ReturnsAsync(CommandResult<bool>.Success(true));

        var options = new CheckoutWorktreeOptions { BranchName = "feature/sub/deep" };

        // Act
        var result = await _worktreeService.CheckoutWorktreeAsync(
            options, _mockInteractionService.Object);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        capturedPath.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task CheckoutWorktreeAsync_NoRemotesConfigured_ReturnsRM003()
    {
        // Arrange
        SetupGitRepo();
        SetupLocalBranchExists("feature/x", false);
        SetupRemoteTrackingBranches("feature/x", new List<RemoteBranchInfo>());

        var options = new CheckoutWorktreeOptions { BranchName = "feature/x" };

        // Act
        var result = await _worktreeService.CheckoutWorktreeAsync(
            options, _mockInteractionService.Object);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.BranchNotFoundAnywhere);
    }

    [Fact]
    public async Task CheckoutWorktreeAsync_RemoteBranchNotFoundOnSpecifiedRemote_ErrorIncludesRemoteName()
    {
        // Arrange
        SetupGitRepo();
        SetupLocalBranchExists("feature/shared", false);
        SetupRemoteTrackingBranches("feature/shared", new List<RemoteBranchInfo>
        {
            new("origin", "feature/shared", "origin/feature/shared"),
        });

        var options = new CheckoutWorktreeOptions { BranchName = "feature/shared", Remote = "upstream" };

        // Act
        var result = await _worktreeService.CheckoutWorktreeAsync(
            options, _mockInteractionService.Object);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.RemoteNotFound);
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage.ShouldContain("upstream");
    }

    [Fact]
    public async Task CheckoutWorktreeAsync_MultipleRemotes_OutOfRangeIndex_ReturnsFailure()
    {
        // Arrange
        SetupGitRepo();
        SetupLocalBranchExists("feature/shared", false);
        SetupRemoteTrackingBranches("feature/shared", new List<RemoteBranchInfo>
        {
            new("origin", "feature/shared", "origin/feature/shared"),
            new("upstream", "feature/shared", "upstream/feature/shared")
        });

        // Return an out-of-range index (2 when only 0 and 1 are valid)
        _mockInteractionService
            .Setup(i => i.SelectAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var options = new CheckoutWorktreeOptions { BranchName = "feature/shared" };

        // Act
        var result = await _worktreeService.CheckoutWorktreeAsync(
            options, _mockInteractionService.Object);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.RemoteNotFound);
    }

    [Fact]
    public async Task CheckoutWorktreeAsync_RemoteBranch_SetsRemoteOnWorktreeInfo()
    {
        // Arrange
        SetupGitRepo();
        SetupLocalBranchExists("feature/shared", false);
        SetupRemoteTrackingBranches("feature/shared", new List<RemoteBranchInfo>
        {
            new("origin", "feature/shared", "origin/feature/shared"),
        });
        SetupListWorktrees();
        SetupAddWorktreeFromRemote();

        var options = new CheckoutWorktreeOptions { BranchName = "feature/shared" };

        // Act
        var result = await _worktreeService.CheckoutWorktreeAsync(
            options, _mockInteractionService.Object);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data.Remote.ShouldBe("origin");
    }
}
