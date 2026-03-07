using Kuju63.WorkTree.CommandLine.Models;
using Kuju63.WorkTree.CommandLine.Services.Git;
using Kuju63.WorkTree.CommandLine.Services.Interaction;
using Kuju63.WorkTree.CommandLine.Services.Worktree;
using Kuju63.WorkTree.CommandLine.Utils;
using Moq;
using Shouldly;

namespace Kuju63.WorkTree.Tests.Integration;

/// <summary>
/// End-to-end tests for the checkout worktree flow, using mocked git service.
/// </summary>
public class CheckoutWorktreeE2ETests
{
    private readonly Mock<IGitService> _mockGitService;
    private readonly Mock<IPathHelper> _mockPathHelper;
    private readonly Mock<IInteractionService> _mockInteractionService;
    private readonly WorktreeService _worktreeService;

    public CheckoutWorktreeE2ETests()
    {
        _mockGitService = new Mock<IGitService>();
        _mockPathHelper = new Mock<IPathHelper>();
        _mockInteractionService = new Mock<IInteractionService>();

        _worktreeService = new WorktreeService(
            _mockGitService.Object,
            _mockPathHelper.Object);

        SetupPathHelper();
    }

    private void SetupPathHelper()
    {
        _mockPathHelper
            .Setup(p => p.ResolvePath(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((path, _) => $"/worktrees{path}");
        _mockPathHelper
            .Setup(p => p.NormalizePath(It.IsAny<string>()))
            .Returns<string>(p => p);
        _mockPathHelper
            .Setup(p => p.ValidatePath(It.IsAny<string>()))
            .Returns(new PathValidationResult(true));
        _mockPathHelper
            .Setup(p => p.EnsureParentDirectoryExists(It.IsAny<string>()));
    }

    [Fact]
    public async Task E2E_LocalBranchCheckout_FullFlow()
    {
        // Arrange - simulate a repo with a local branch
        _mockGitService
            .Setup(g => g.IsGitRepositoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(true));
        _mockGitService
            .Setup(g => g.BranchExistsAsync("feature/local", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(true));
        _mockGitService
            .Setup(g => g.ListWorktreesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Success(new List<WorktreeInfo>()));
        _mockGitService
            .Setup(g => g.AddWorktreeAsync(It.IsAny<string>(), "feature/local", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(true));

        var options = new CheckoutWorktreeOptions { BranchName = "feature/local" };

        // Act
        var result = await _worktreeService.CheckoutWorktreeAsync(
            options, _mockInteractionService.Object);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data.Branch.ShouldBe("feature/local");
        result.Data.Exists.ShouldBeTrue();
    }

    [Fact]
    public async Task E2E_RemoteBranchCheckout_FullFlow()
    {
        // Arrange - simulate a repo where branch only exists on origin
        _mockGitService
            .Setup(g => g.IsGitRepositoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(true));
        _mockGitService
            .Setup(g => g.BranchExistsAsync("feature/remote-only", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(false));
        _mockGitService
            .Setup(g => g.GetRemoteTrackingBranchesAsync("feature/remote-only", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<IReadOnlyList<RemoteBranchInfo>>.Success(new List<RemoteBranchInfo>
            {
                new("origin", "feature/remote-only", "origin/feature/remote-only")
            }));
        _mockGitService
            .Setup(g => g.ListWorktreesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Success(new List<WorktreeInfo>()));
        _mockGitService
            .Setup(g => g.AddWorktreeFromRemoteAsync(
                It.IsAny<string>(), "feature/remote-only", "origin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<Unit>.Success(Unit.Value));

        var options = new CheckoutWorktreeOptions { BranchName = "feature/remote-only" };

        // Act
        var result = await _worktreeService.CheckoutWorktreeAsync(
            options, _mockInteractionService.Object);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data.Branch.ShouldBe("feature/remote-only");
    }

    [Fact]
    public async Task E2E_BranchAlreadyCheckedOut_ReturnsError()
    {
        // Arrange
        _mockGitService
            .Setup(g => g.IsGitRepositoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(true));
        _mockGitService
            .Setup(g => g.BranchExistsAsync("feature/local", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(true));
        _mockGitService
            .Setup(g => g.ListWorktreesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Success(new List<WorktreeInfo>
            {
                new("/existing/path", "feature/local", false, "abc123", DateTime.UtcNow, true)
            }));

        var options = new CheckoutWorktreeOptions { BranchName = "feature/local" };

        // Act
        var result = await _worktreeService.CheckoutWorktreeAsync(
            options, _mockInteractionService.Object);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.BranchAlreadyInUse);
    }

    [Fact]
    public async Task E2E_BranchNotFoundAnywhere_ReturnsRM003()
    {
        // Arrange
        _mockGitService
            .Setup(g => g.IsGitRepositoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(true));
        _mockGitService
            .Setup(g => g.BranchExistsAsync("feature/ghost", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(false));
        _mockGitService
            .Setup(g => g.GetRemoteTrackingBranchesAsync("feature/ghost", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<IReadOnlyList<RemoteBranchInfo>>.Success(new List<RemoteBranchInfo>()));

        var options = new CheckoutWorktreeOptions { BranchName = "feature/ghost" };

        // Act
        var result = await _worktreeService.CheckoutWorktreeAsync(
            options, _mockInteractionService.Object);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.BranchNotFoundAnywhere);
    }
}
