using Kuju63.WorkTree.CommandLine.Models;
using Kuju63.WorkTree.CommandLine.Services.Editor;
using Kuju63.WorkTree.CommandLine.Services.Git;
using Kuju63.WorkTree.CommandLine.Services.Worktree;
using Kuju63.WorkTree.CommandLine.Utils;
using Moq;
using Shouldly;

namespace Kuju63.WorkTree.Tests.Services.Worktree;

/// <summary>
/// Tests for WorktreeService editor integration functionality.
/// </summary>
public class WorktreeServiceEditorTests
{
    private readonly Mock<IGitService> _mockGitService;
    private readonly Mock<IPathHelper> _mockPathHelper;

    public WorktreeServiceEditorTests()
    {
        _mockGitService = new Mock<IGitService>();
        _mockPathHelper = new Mock<IPathHelper>();
    }

    #region LaunchEditorIfSpecifiedAsync Tests

    [Fact]
    public async Task CreateWorktreeAsync_WithEditorType_LaunchesEditorSuccessfully()
    {
        // Arrange
        var mockEditorService = new Mock<IEditorService>();
        var worktreeService = new WorktreeService(
            _mockGitService.Object,
            _mockPathHelper.Object,
            mockEditorService.Object);

        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-x",
            BaseBranch = "main",
            EditorType = EditorType.VSCode
        };

        SetupSuccessfulWorktreeCreation();

        mockEditorService
            .Setup(x => x.LaunchEditorAsync(
                "/Users/dev/wt-feature-x",
                EditorType.VSCode,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<string>.Success("Editor launched successfully"));

        // Act
        var result = await worktreeService.CreateWorktreeAsync(options);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Branch.ShouldBe("feature-x");
        result.Data.Path.ShouldBe("/Users/dev/wt-feature-x");
        result.Warnings.ShouldBeEmpty();

        mockEditorService.Verify(
            x => x.LaunchEditorAsync("/Users/dev/wt-feature-x", EditorType.VSCode, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateWorktreeAsync_WithEditorType_WhenLaunchFails_ReturnsSuccessWithWarning()
    {
        // Arrange
        var mockEditorService = new Mock<IEditorService>();
        var worktreeService = new WorktreeService(
            _mockGitService.Object,
            _mockPathHelper.Object,
            mockEditorService.Object);

        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-x",
            BaseBranch = "main",
            EditorType = EditorType.VSCode
        };

        SetupSuccessfulWorktreeCreation();

        mockEditorService
            .Setup(x => x.LaunchEditorAsync(
                "/Users/dev/wt-feature-x",
                EditorType.VSCode,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<string>.Failure(
                ErrorCodes.EditorNotFound,
                "VSCode not found",
                "Install VSCode or specify a different editor"));

        // Act
        var result = await worktreeService.CreateWorktreeAsync(options);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Branch.ShouldBe("feature-x");
        result.Warnings.ShouldNotBeNull();
        result.Warnings!.Count.ShouldBe(1);
        result.Warnings[0].ShouldContain("Warning:");
        result.Warnings[0].ShouldContain("VSCode not found");

        mockEditorService.Verify(
            x => x.LaunchEditorAsync("/Users/dev/wt-feature-x", EditorType.VSCode, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateWorktreeAsync_WithEditorTypeButNoEditorService_SkipsEditorLaunch()
    {
        // Arrange
        // Create WorktreeService without EditorService
        var worktreeService = new WorktreeService(
            _mockGitService.Object,
            _mockPathHelper.Object);

        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-x",
            BaseBranch = "main",
            EditorType = EditorType.VSCode
        };

        SetupSuccessfulWorktreeCreation();

        // Act
        var result = await worktreeService.CreateWorktreeAsync(options);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Branch.ShouldBe("feature-x");
        result.Data.Path.ShouldBe("/Users/dev/wt-feature-x");
        result.Warnings.ShouldBeEmpty();
    }

    #endregion

    private void SetupSuccessfulWorktreeCreation()
    {
        _mockGitService
            .Setup(x => x.IsGitRepositoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(true));

        _mockGitService
            .Setup(x => x.BranchExistsAsync("feature-x", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(false));

        _mockGitService
            .Setup(x => x.CreateBranchAsync("feature-x", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<BranchInfo>.Success(new BranchInfo("feature-x", "main", true, false)));

        _mockPathHelper
            .Setup(x => x.ResolvePath("../wt-feature-x", It.IsAny<string>()))
            .Returns("/Users/dev/wt-feature-x");

        _mockPathHelper
            .Setup(x => x.NormalizePath("/Users/dev/wt-feature-x"))
            .Returns("/Users/dev/wt-feature-x");

        _mockPathHelper
            .Setup(x => x.ValidatePath("/Users/dev/wt-feature-x"))
            .Returns(new PathValidationResult(true));

        _mockGitService
            .Setup(x => x.AddWorktreeAsync("/Users/dev/wt-feature-x", "feature-x", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(true));
    }

}
