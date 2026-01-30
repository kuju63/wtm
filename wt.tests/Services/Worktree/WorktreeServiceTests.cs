using System.IO.Abstractions;
using Kuju63.WorkTree.CommandLine.Models;
using Kuju63.WorkTree.CommandLine.Services.Git;
using Kuju63.WorkTree.CommandLine.Services.Worktree;
using Kuju63.WorkTree.CommandLine.Utils;
using Moq;
using Shouldly;

namespace Kuju63.WorkTree.Tests.Services.Worktree;

public class WorktreeServiceTests
{
    private readonly Mock<IGitService> _mockGitService;
    private readonly Mock<IPathHelper> _mockPathHelper;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly WorktreeService _worktreeService;

    public WorktreeServiceTests()
    {
        _mockGitService = new Mock<IGitService>();
        _mockFileSystem = new Mock<IFileSystem>();
        _mockPathHelper = new Mock<IPathHelper>();
        _worktreeService = new WorktreeService(_mockGitService.Object, _mockPathHelper.Object);
    }

    [Fact]
    public async Task CreateWorktreeAsync_WithValidOptions_CreatesWorktreeSuccessfully()
    {
        // Arrange
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-x",
            BaseBranch = "main"
        };

        _mockGitService
            .Setup(x => x.IsGitRepositoryAsync(default))
            .ReturnsAsync(CommandResult<bool>.Success(true));

        _mockGitService
            .Setup(x => x.BranchExistsAsync("feature-x", default))
            .ReturnsAsync(CommandResult<bool>.Success(false));

        _mockGitService
            .Setup(x => x.CreateBranchAsync("feature-x", "main", default))
            .ReturnsAsync(CommandResult<BranchInfo>.Success(new BranchInfo("feature-x", "main", true, false)));

        _mockPathHelper
            .Setup(x => x.ResolvePath("../wt-feature-x", It.IsAny<string>()))
            .Returns("/Users/dev/project/../wt-feature-x");

        _mockPathHelper
            .Setup(x => x.NormalizePath("/Users/dev/project/../wt-feature-x"))
            .Returns("/Users/dev/wt-feature-x");

        _mockPathHelper
            .Setup(x => x.ValidatePath("/Users/dev/wt-feature-x"))
            .Returns(new PathValidationResult(true));

        _mockGitService
            .Setup(x => x.AddWorktreeAsync("/Users/dev/wt-feature-x", "feature-x", default))
            .ReturnsAsync(CommandResult<bool>.Success(true));

        // Act
        var result = await _worktreeService.CreateWorktreeAsync(options);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Branch.ShouldBe("feature-x");
        result.Data.Path.ShouldBe("/Users/dev/wt-feature-x");
    }

    [Fact]
    public async Task CreateWorktreeAsync_WhenNotInGitRepo_ReturnsError()
    {
        // Arrange
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-x",
            BaseBranch = "main"
        };

        _mockGitService
            .Setup(x => x.IsGitRepositoryAsync(default))
            .ReturnsAsync(CommandResult<bool>.Success(false));

        // Act
        var result = await _worktreeService.CreateWorktreeAsync(options);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.NotGitRepository);
    }

    [Fact]
    public async Task CreateWorktreeAsync_WhenBranchAlreadyExists_ReturnsError()
    {
        // Arrange
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-x",
            BaseBranch = "main"
        };

        _mockGitService
            .Setup(x => x.IsGitRepositoryAsync(default))
            .ReturnsAsync(CommandResult<bool>.Success(true));

        _mockGitService
            .Setup(x => x.BranchExistsAsync("feature-x", default))
            .ReturnsAsync(CommandResult<bool>.Success(true));

        // Act
        var result = await _worktreeService.CreateWorktreeAsync(options);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.BranchAlreadyExists);
    }

    [Fact]
    public async Task CreateWorktreeAsync_WhenInvalidPath_ReturnsError()
    {
        // Arrange
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-x",
            BaseBranch = "main"
        };

        _mockGitService
            .Setup(x => x.IsGitRepositoryAsync(default))
            .ReturnsAsync(CommandResult<bool>.Success(true));

        _mockGitService
            .Setup(x => x.BranchExistsAsync("feature-x", default))
            .ReturnsAsync(CommandResult<bool>.Success(false));

        _mockGitService
            .Setup(x => x.CreateBranchAsync("feature-x", "main", default))
            .ReturnsAsync(CommandResult<BranchInfo>.Success(new BranchInfo("feature-x", "main", true, false)));

        _mockPathHelper
            .Setup(x => x.ResolvePath("../wt-feature-x", It.IsAny<string>()))
            .Returns("/Users/dev/wt-feature-x");

        _mockPathHelper
            .Setup(x => x.NormalizePath("/Users/dev/wt-feature-x"))
            .Returns("/Users/dev/wt-feature-x");

        _mockPathHelper
            .Setup(x => x.ValidatePath("/Users/dev/wt-feature-x"))
            .Returns(new PathValidationResult(false, "Invalid path"));

        // Act
        var result = await _worktreeService.CreateWorktreeAsync(options);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.InvalidPath);
    }

    [Fact]
    public async Task CreateWorktreeAsync_WithCustomPath_UsesProvidedPath()
    {
        // Arrange
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-x",
            BaseBranch = "main",
            WorktreePath = "/custom/path/feature-x"
        };

        _mockGitService
            .Setup(x => x.IsGitRepositoryAsync(default))
            .ReturnsAsync(CommandResult<bool>.Success(true));

        _mockGitService
            .Setup(x => x.BranchExistsAsync("feature-x", default))
            .ReturnsAsync(CommandResult<bool>.Success(false));

        _mockGitService
            .Setup(x => x.CreateBranchAsync("feature-x", "main", default))
            .ReturnsAsync(CommandResult<BranchInfo>.Success(new BranchInfo("feature-x", "main", true, false)));

        _mockPathHelper
            .Setup(x => x.ResolvePath("/custom/path/feature-x", It.IsAny<string>()))
            .Returns("/custom/path/feature-x");

        _mockPathHelper
            .Setup(x => x.NormalizePath("/custom/path/feature-x"))
            .Returns("/custom/path/feature-x");

        _mockPathHelper
            .Setup(x => x.ValidatePath("/custom/path/feature-x"))
            .Returns(new PathValidationResult(true));

        _mockGitService
            .Setup(x => x.AddWorktreeAsync("/custom/path/feature-x", "feature-x", default))
            .ReturnsAsync(CommandResult<bool>.Success(true));

        // Act
        var result = await _worktreeService.CreateWorktreeAsync(options);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Path.ShouldBe("/custom/path/feature-x");
    }

    [Fact]
    public async Task ListWorktreesAsync_WithValidWorktrees_ReturnsSortedList()
    {
        // Arrange
        var worktrees = new List<WorktreeInfo>
        {
            new WorktreeInfo("/path/worktree1", "feature-1", false, string.Empty, new DateTime(2026, 1, 1), true),
            new WorktreeInfo("/path/worktree2", "feature-2", false, string.Empty, new DateTime(2026, 1, 3), true),
            new WorktreeInfo("/path/worktree3", "feature-3", false, string.Empty, new DateTime(2026, 1, 2), true)
        };

        _mockGitService
            .Setup(x => x.IsGitRepositoryAsync(default))
            .ReturnsAsync(CommandResult<bool>.Success(true));

        _mockGitService
            .Setup(x => x.ListWorktreesAsync(default))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Success(worktrees));

        // Act
        var result = await _worktreeService.ListWorktreesAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Count.ShouldBe(3);
        // Should be sorted by creation date (newest first)
        result.Data[0].Branch.ShouldBe("feature-2"); // 2026-01-03
        result.Data[1].Branch.ShouldBe("feature-3"); // 2026-01-02
        result.Data[2].Branch.ShouldBe("feature-1"); // 2026-01-01
    }

    [Fact]
    public async Task ListWorktreesAsync_WhenNotInGitRepo_ReturnsError()
    {
        // Arrange
        _mockGitService
            .Setup(x => x.IsGitRepositoryAsync(default))
            .ReturnsAsync(CommandResult<bool>.Success(false));

        // Act
        var result = await _worktreeService.ListWorktreesAsync();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.NotGitRepository);
    }

    [Fact]
    public async Task ListWorktreesAsync_WhenGitServiceFails_ReturnsError()
    {
        // Arrange
        _mockGitService
            .Setup(x => x.IsGitRepositoryAsync(default))
            .ReturnsAsync(CommandResult<bool>.Success(true));

        _mockGitService
            .Setup(x => x.ListWorktreesAsync(default))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Failure(
                ErrorCodes.GitCommandFailed,
                "Git command failed",
                "Check your Git installation"));

        // Act
        var result = await _worktreeService.ListWorktreesAsync();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.GitCommandFailed);
    }

    [Fact]
    public async Task ListWorktreesAsync_WithNoWorktrees_ReturnsEmptyList()
    {
        // Arrange
        _mockGitService
            .Setup(x => x.IsGitRepositoryAsync(default))
            .ReturnsAsync(CommandResult<bool>.Success(true));

        _mockGitService
            .Setup(x => x.ListWorktreesAsync(default))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Success(new List<WorktreeInfo>()));

        // Act
        var result = await _worktreeService.ListWorktreesAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Count.ShouldBe(0);
    }

    [Fact]
    public async Task ListWorktreesAsync_WithCancellationToken_PassesTokenToGitService()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var worktrees = new List<WorktreeInfo>
        {
            new WorktreeInfo("/path/worktree", "main", false, string.Empty, DateTime.UtcNow, true)
        };

        _mockGitService
            .Setup(x => x.IsGitRepositoryAsync(cancellationToken))
            .ReturnsAsync(CommandResult<bool>.Success(true));

        _mockGitService
            .Setup(x => x.ListWorktreesAsync(cancellationToken))
            .ReturnsAsync(CommandResult<List<WorktreeInfo>>.Success(worktrees));

        // Act
        var result = await _worktreeService.ListWorktreesAsync(cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _mockGitService.Verify(x => x.IsGitRepositoryAsync(cancellationToken), Times.Once);
        _mockGitService.Verify(x => x.ListWorktreesAsync(cancellationToken), Times.Once);
    }

    #region Phase 2: Error Path Tests

    [Fact]
    public async Task CreateWorktreeAsync_WhenBranchExistsCheckFails_ReturnsError()
    {
        // Arrange
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-x",
            BaseBranch = "main"
        };

        _mockGitService
            .Setup(x => x.IsGitRepositoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(true));

        _mockGitService
            .Setup(x => x.BranchExistsAsync("feature-x", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Failure(
                ErrorCodes.GitCommandFailed,
                "Failed to check branch existence",
                "Check Git installation"));

        // Act
        var result = await _worktreeService.CreateWorktreeAsync(options);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.GitCommandFailed);
        result.ErrorMessage.ShouldBe("Failed to check branch existence");
        result.Solution.ShouldBe("Check Git installation");
    }

    [Fact]
    public async Task CreateWorktreeAsync_WhenCreateBranchFails_ReturnsError()
    {
        // Arrange
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-x",
            BaseBranch = "main"
        };

        _mockGitService
            .Setup(x => x.IsGitRepositoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(true));

        _mockGitService
            .Setup(x => x.BranchExistsAsync("feature-x", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(false));

        _mockGitService
            .Setup(x => x.CreateBranchAsync("feature-x", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<BranchInfo>.Failure(
                ErrorCodes.GitCommandFailed,
                "Failed to create branch",
                "Check if base branch exists"));

        // Act
        var result = await _worktreeService.CreateWorktreeAsync(options);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.GitCommandFailed);
        result.ErrorMessage.ShouldBe("Failed to create branch");
        result.Solution.ShouldBe("Check if base branch exists");
    }

    [Fact]
    public async Task CreateWorktreeAsync_WhenGetCurrentBranchFails_ReturnsError()
    {
        // Arrange
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-x"
            // BaseBranch is not specified, so it should call GetCurrentBranchAsync
        };

        _mockGitService
            .Setup(x => x.IsGitRepositoryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<bool>.Success(true));

        _mockGitService
            .Setup(x => x.GetCurrentBranchAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<string>.Failure(
                ErrorCodes.GitCommandFailed,
                "Failed to get current branch",
                "Ensure you are on a branch"));

        // Act
        var result = await _worktreeService.CreateWorktreeAsync(options);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.GitCommandFailed);
        result.ErrorMessage.ShouldBe("Failed to get current branch");
        result.Solution.ShouldBe("Ensure you are on a branch");
    }

    #endregion

    #region Phase 3: Additional Error Path Tests

    [Fact]
    public async Task CreateWorktreeAsync_WhenAddWorktreeFails_ReturnsError()
    {
        // Arrange
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-x",
            BaseBranch = "main"
        };

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
            .ReturnsAsync(CommandResult<bool>.Failure(
                ErrorCodes.WorktreeCreationFailed,
                "Failed to add worktree",
                "Check if path is available"));

        // Act
        var result = await _worktreeService.CreateWorktreeAsync(options);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.WorktreeCreationFailed);
        result.ErrorMessage.ShouldBe("Failed to add worktree");
        result.Solution.ShouldBe("Check if path is available");
    }

    [Fact]
    public async Task CreateWorktreeAsync_WhenPathPreparationThrows_ReturnsError()
    {
        // Arrange
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-x",
            BaseBranch = "main"
        };

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

        _mockPathHelper
            .Setup(x => x.EnsureParentDirectoryExists("/Users/dev/wt-feature-x"))
            .Throws(new UnauthorizedAccessException("Permission denied"));

        // Act
        var result = await _worktreeService.CreateWorktreeAsync(options);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.InvalidPath);
        result.ErrorMessage.ShouldBe("Invalid worktree path");
        result.Solution.ShouldBe("Permission denied");
    }

    #endregion
}
