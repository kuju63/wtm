using System.IO.Abstractions;
using Kuju63.WorkTree.CommandLine.Models;
using Kuju63.WorkTree.CommandLine.Services.Git;
using Kuju63.WorkTree.CommandLine.Utils;
using Moq;
using Shouldly;

namespace Kuju63.WorkTree.Tests.Services.Git;

public class GitServiceTests
{
    private readonly Mock<IProcessRunner> _mockProcessRunner;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly GitService _gitService;

    public GitServiceTests()
    {
        _mockProcessRunner = new Mock<IProcessRunner>();
        _mockFileSystem = new Mock<IFileSystem>();

        // Setup default file system behavior
        _mockFileSystem.Setup(fs => fs.File.Exists(It.IsAny<string>())).Returns(false);
        _mockFileSystem.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(true);

        _gitService = new GitService(_mockProcessRunner.Object, _mockFileSystem.Object);
    }

    [Fact]
    public async Task IsGitRepositoryAsync_WhenInGitRepo_ReturnsTrue()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "rev-parse --git-dir", null, default))
            .ReturnsAsync(new ProcessResult(0, ".git", ""));

        // Act
        var result = await _gitService.IsGitRepositoryAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeTrue();
    }

    [Fact]
    public async Task IsGitRepositoryAsync_WhenNotInGitRepo_ReturnsFalse()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "rev-parse --git-dir", null, default))
            .ReturnsAsync(new ProcessResult(128, "", "fatal: not a git repository"));

        // Act
        var result = await _gitService.IsGitRepositoryAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeFalse();
    }

    [Fact]
    public async Task GetCurrentBranchAsync_WithValidRepo_ReturnsBranchName()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "branch --show-current", null, default))
            .ReturnsAsync(new ProcessResult(0, "main", ""));

        // Act
        var result = await _gitService.GetCurrentBranchAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBe("main");
    }

    [Fact]
    public async Task GetCurrentBranchAsync_WhenDetachedHead_ReturnsError()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "branch --show-current", null, default))
            .ReturnsAsync(new ProcessResult(0, "", ""));

        // Act
        var result = await _gitService.GetCurrentBranchAsync();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.GitCommandFailed);
    }

    [Fact]
    public async Task BranchExistsAsync_WhenBranchExists_ReturnsTrue()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "rev-parse --verify feature-x", null, default))
            .ReturnsAsync(new ProcessResult(0, "abc123def", ""));

        // Act
        var result = await _gitService.BranchExistsAsync("feature-x");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeTrue();
    }

    [Fact]
    public async Task BranchExistsAsync_WhenBranchDoesNotExist_ReturnsFalse()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "rev-parse --verify nonexistent", null, default))
            .ReturnsAsync(new ProcessResult(128, "", "fatal: Needed a single revision"));

        // Act
        var result = await _gitService.BranchExistsAsync("nonexistent");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeFalse();
    }

    [Fact]
    public async Task CreateBranchAsync_WithValidName_CreatesSuccessfully()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "branch feature-x main", null, default))
            .ReturnsAsync(new ProcessResult(0, "", ""));

        // Act
        var result = await _gitService.CreateBranchAsync("feature-x", "main");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Name.ShouldBe("feature-x");
        result.Data.BaseBranch.ShouldBe("main");
    }

    [Fact]
    public async Task CreateBranchAsync_WhenBranchAlreadyExists_ReturnsError()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "branch feature-x main", null, default))
            .ReturnsAsync(new ProcessResult(128, "", "fatal: A branch named 'feature-x' already exists"));

        // Act
        var result = await _gitService.CreateBranchAsync("feature-x", "main");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.BranchAlreadyExists);
    }

    [Fact]
    public async Task AddWorktreeAsync_WithValidPath_AddsSuccessfully()
    {
        // Arrange
        var worktreePath = "/Users/dev/worktrees/feature-x";
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", $"worktree add \"{worktreePath}\" feature-x", null, default))
            .ReturnsAsync(new ProcessResult(0, $"Preparing worktree\nBranch 'feature-x' set up", ""));

        // Act
        var result = await _gitService.AddWorktreeAsync(worktreePath, "feature-x");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeTrue();
    }

    [Fact]
    public async Task AddWorktreeAsync_WhenWorktreeAlreadyExists_ReturnsError()
    {
        // Arrange
        var worktreePath = "/Users/dev/worktrees/feature-x";
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", $"worktree add \"{worktreePath}\" feature-x", null, default))
            .ReturnsAsync(new ProcessResult(128, "", "fatal: '/Users/dev/worktrees/feature-x' already exists"));

        // Act
        var result = await _gitService.AddWorktreeAsync(worktreePath, "feature-x");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.WorktreeAlreadyExists);
    }

    [Fact]
    public async Task ListWorktreesAsync_WithValidWorktrees_ReturnsWorktreeList()
    {
        // Arrange
        var porcelainOutput = @"worktree /path/to/main
HEAD abc123def456
branch refs/heads/main

worktree /path/to/feature-a
HEAD def456789012
branch refs/heads/feature-a

";
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "worktree list --porcelain", null, default))
            .ReturnsAsync(new ProcessResult(0, porcelainOutput, ""));

        // Act
        var result = await _gitService.ListWorktreesAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Count.ShouldBe(2);
        result.Data[0].Path.ShouldBe("/path/to/main");
        result.Data[0].Branch.ShouldBe("main");
        result.Data[1].Path.ShouldBe("/path/to/feature-a");
        result.Data[1].Branch.ShouldBe("feature-a");
    }

    [Fact]
    public async Task ListWorktreesAsync_WithInvalidWorktreeLine_IgnoresMalformedEntry()
    {
        // Arrange - Line with "worktree " prefix but too short
        var porcelainOutput = @"worktree
HEAD abc123def456
branch refs/heads/main

worktree /path/to/valid
HEAD def456789012
branch refs/heads/feature

";
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "worktree list --porcelain", null, default))
            .ReturnsAsync(new ProcessResult(0, porcelainOutput, ""));

        // Act
        var result = await _gitService.ListWorktreesAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Count.ShouldBe(1);
        result.Data[0].Path.ShouldBe("/path/to/valid");
    }

    [Fact]
    public async Task ListWorktreesAsync_WithInvalidHeadLine_IgnoresInvalidHead()
    {
        // Arrange - Line with "HEAD " prefix but too short
        var porcelainOutput = @"worktree /path/to/test
HEAD
branch refs/heads/main

worktree /path/to/valid
HEAD def456789012
branch refs/heads/feature

";
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "worktree list --porcelain", null, default))
            .ReturnsAsync(new ProcessResult(0, porcelainOutput, ""));

        // Act
        var result = await _gitService.ListWorktreesAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Count.ShouldBe(1);
        result.Data[0].Path.ShouldBe("/path/to/valid");
    }

    [Fact]
    public async Task ListWorktreesAsync_WithInvalidBranchLine_IgnoresInvalidBranch()
    {
        // Arrange - Line with "branch " prefix but too short
        var porcelainOutput = @"worktree /path/to/test
HEAD abc123def456
branch

worktree /path/to/valid
HEAD def456789012
branch refs/heads/feature

";
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "worktree list --porcelain", null, default))
            .ReturnsAsync(new ProcessResult(0, porcelainOutput, ""));

        // Act
        var result = await _gitService.ListWorktreesAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Count.ShouldBe(2);
        // First worktree should use HEAD as branch since branch line was invalid
        result.Data[0].Branch.ShouldBe("abc123def456");
        result.Data[1].Branch.ShouldBe("feature");
    }

    [Fact]
    public async Task ListWorktreesAsync_WithDetachedHead_ReturnsDetachedWorktree()
    {
        // Arrange
        var porcelainOutput = @"worktree /path/to/detached
HEAD abc123def456
detached

";
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "worktree list --porcelain", null, default))
            .ReturnsAsync(new ProcessResult(0, porcelainOutput, ""));

        // Act
        var result = await _gitService.ListWorktreesAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Count.ShouldBe(1);
        result.Data[0].IsDetached.ShouldBeTrue();
    }

    #region HasUncommittedChangesAsync Tests

    [Fact]
    public async Task HasUncommittedChangesAsync_WithNoChanges_ReturnsFalse()
    {
        // Arrange
        var worktreePath = "/path/to/worktree";
        _mockProcessRunner
            .Setup(x => x.RunAsync(
                "git",
                $"-C \"{worktreePath}\" status --porcelain",
                null,
                default))
            .ReturnsAsync(new ProcessResult(0, "", ""));

        // Act
        var result = await _gitService.HasUncommittedChangesAsync(worktreePath);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeFalse();
    }

    [Fact]
    public async Task HasUncommittedChangesAsync_WithOnlyUntrackedFiles_ReturnsFalse()
    {
        // Arrange
        var worktreePath = "/path/to/worktree";
        var statusOutput = "?? untracked1.txt\n?? untracked2.txt\n";
        _mockProcessRunner
            .Setup(x => x.RunAsync(
                "git",
                $"-C \"{worktreePath}\" status --porcelain",
                null,
                default))
            .ReturnsAsync(new ProcessResult(0, statusOutput, ""));

        // Act
        var result = await _gitService.HasUncommittedChangesAsync(worktreePath);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeFalse();
    }

    [Fact]
    public async Task HasUncommittedChangesAsync_WithStagedChanges_ReturnsTrue()
    {
        // Arrange
        var worktreePath = "/path/to/worktree";
        var statusOutput = "M  staged.txt\n";
        _mockProcessRunner
            .Setup(x => x.RunAsync(
                "git",
                $"-C \"{worktreePath}\" status --porcelain",
                null,
                default))
            .ReturnsAsync(new ProcessResult(0, statusOutput, ""));

        // Act
        var result = await _gitService.HasUncommittedChangesAsync(worktreePath);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeTrue();
    }

    [Fact]
    public async Task HasUncommittedChangesAsync_WithUnstagedChanges_ReturnsTrue()
    {
        // Arrange
        var worktreePath = "/path/to/worktree";
        var statusOutput = " M unstaged.txt\n";
        _mockProcessRunner
            .Setup(x => x.RunAsync(
                "git",
                $"-C \"{worktreePath}\" status --porcelain",
                null,
                default))
            .ReturnsAsync(new ProcessResult(0, statusOutput, ""));

        // Act
        var result = await _gitService.HasUncommittedChangesAsync(worktreePath);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeTrue();
    }

    [Fact]
    public async Task HasUncommittedChangesAsync_WithMixedChanges_ReturnsTrue()
    {
        // Arrange
        var worktreePath = "/path/to/worktree";
        var statusOutput = "M  staged.txt\n M unstaged.txt\n?? untracked.txt\n";
        _mockProcessRunner
            .Setup(x => x.RunAsync(
                "git",
                $"-C \"{worktreePath}\" status --porcelain",
                null,
                default))
            .ReturnsAsync(new ProcessResult(0, statusOutput, ""));

        // Act
        var result = await _gitService.HasUncommittedChangesAsync(worktreePath);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeTrue();
    }

    [Fact]
    public async Task HasUncommittedChangesAsync_WithDeletedFiles_ReturnsTrue()
    {
        // Arrange
        var worktreePath = "/path/to/worktree";
        var statusOutput = "D  deleted.txt\n";
        _mockProcessRunner
            .Setup(x => x.RunAsync(
                "git",
                $"-C \"{worktreePath}\" status --porcelain",
                null,
                default))
            .ReturnsAsync(new ProcessResult(0, statusOutput, ""));

        // Act
        var result = await _gitService.HasUncommittedChangesAsync(worktreePath);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeTrue();
    }

    [Fact]
    public async Task HasUncommittedChangesAsync_WhenGitCommandFails_ReturnsError()
    {
        // Arrange
        var worktreePath = "/path/to/worktree";
        _mockProcessRunner
            .Setup(x => x.RunAsync(
                "git",
                $"-C \"{worktreePath}\" status --porcelain",
                null,
                default))
            .ReturnsAsync(new ProcessResult(128, "", "fatal: not a git repository"));

        // Act
        var result = await _gitService.HasUncommittedChangesAsync(worktreePath);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.GitCommandFailed);
    }

    [Fact]
    public async Task HasUncommittedChangesAsync_WithEmptyLines_IgnoresEmptyLines()
    {
        // Arrange
        var worktreePath = "/path/to/worktree";
        var statusOutput = "\n\n?? untracked.txt\n\n";
        _mockProcessRunner
            .Setup(x => x.RunAsync(
                "git",
                $"-C \"{worktreePath}\" status --porcelain",
                null,
                default))
            .ReturnsAsync(new ProcessResult(0, statusOutput, ""));

        // Act
        var result = await _gitService.HasUncommittedChangesAsync(worktreePath);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeFalse();
    }

    [Fact]
    public async Task HasUncommittedChangesAsync_WithWhitespacePrefix_RecognizesChanges()
    {
        // Arrange
        var worktreePath = "/path/to/worktree";
        var statusOutput = "  M  file.txt\n";
        _mockProcessRunner
            .Setup(x => x.RunAsync(
                "git",
                $"-C \"{worktreePath}\" status --porcelain",
                null,
                default))
            .ReturnsAsync(new ProcessResult(0, statusOutput, ""));

        // Act
        var result = await _gitService.HasUncommittedChangesAsync(worktreePath);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeTrue();
    }

    #endregion

    #region IsWorktreeLockedAsync Tests

    [Fact]
    public async Task IsWorktreeLockedAsync_WhenLockFileExists_ReturnsTrue()
    {
        // Arrange
        var worktreePath = "/path/to/worktree";
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "rev-parse --git-dir", null, default))
            .ReturnsAsync(new ProcessResult(0, "/repo/.git", ""));
        _mockFileSystem
            .Setup(fs => fs.File.Exists(It.Is<string>(p => p.EndsWith("locked"))))
            .Returns(true);

        // Act
        var result = await _gitService.IsWorktreeLockedAsync(worktreePath);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeTrue();
    }

    [Fact]
    public async Task IsWorktreeLockedAsync_WhenLockFileDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var worktreePath = "/path/to/worktree";
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "rev-parse --git-dir", null, default))
            .ReturnsAsync(new ProcessResult(0, "/repo/.git", ""));
        _mockFileSystem
            .Setup(fs => fs.File.Exists(It.Is<string>(p => p.EndsWith("locked"))))
            .Returns(false);

        // Act
        var result = await _gitService.IsWorktreeLockedAsync(worktreePath);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeFalse();
    }

    [Fact]
    public async Task IsWorktreeLockedAsync_WithDifferentWorktreeName_ChecksCorrectPath()
    {
        // Arrange
        var worktreePath = "/path/to/my-feature";
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "rev-parse --git-dir", null, default))
            .ReturnsAsync(new ProcessResult(0, "/repo/.git", ""));

        var expectedLockPath = "/repo/.git/worktrees/my-feature/locked";
        _mockFileSystem
            .Setup(fs => fs.File.Exists(expectedLockPath))
            .Returns(true);

        // Act
        var result = await _gitService.IsWorktreeLockedAsync(worktreePath);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeTrue();
        _mockFileSystem.Verify(
            fs => fs.File.Exists(It.Is<string>(p => p.Contains("my-feature") && p.EndsWith("locked"))),
            Times.Once);
    }

    [Fact]
    public async Task IsWorktreeLockedAsync_WhenIOExceptionThrown_ReturnsError()
    {
        // Arrange
        var worktreePath = "/path/to/worktree";
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "rev-parse --git-dir", null, default))
            .ReturnsAsync(new ProcessResult(0, "/repo/.git", ""));
        _mockFileSystem
            .Setup(fs => fs.File.Exists(It.IsAny<string>()))
            .Throws(new IOException("Disk read error"));

        // Act
        var result = await _gitService.IsWorktreeLockedAsync(worktreePath);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.GitCommandFailed);
        result.ErrorMessage!.ShouldContain("Failed to check lock status");
    }

    [Fact]
    public async Task IsWorktreeLockedAsync_WhenUnauthorizedAccessExceptionThrown_ReturnsError()
    {
        // Arrange
        var worktreePath = "/path/to/worktree";
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "rev-parse --git-dir", null, default))
            .ReturnsAsync(new ProcessResult(0, "/repo/.git", ""));
        _mockFileSystem
            .Setup(fs => fs.File.Exists(It.IsAny<string>()))
            .Throws(new UnauthorizedAccessException("Access denied"));

        // Act
        var result = await _gitService.IsWorktreeLockedAsync(worktreePath);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.GitCommandFailed);
        result.ErrorMessage!.ShouldContain("Access denied");
    }

    [Fact]
    public async Task IsWorktreeLockedAsync_WhenGitDirNotFound_ReturnsError()
    {
        // Arrange
        var worktreePath = "/path/to/worktree";
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "rev-parse --git-dir", null, default))
            .ReturnsAsync(new ProcessResult(128, "", "fatal: not a git repository"));

        // Act
        var result = await _gitService.IsWorktreeLockedAsync(worktreePath);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.NotGitRepository);
        result.ErrorMessage!.ShouldContain("Could not determine git directory");
    }

    #endregion

    #region RemoveWorktreeAsync Tests

    [Fact]
    public async Task RemoveWorktreeAsync_WithoutForce_ExecutesStandardRemoval()
    {
        // Arrange
        var worktreePath = "/path/to/worktree";
        _mockProcessRunner
            .Setup(x => x.RunAsync(
                "git",
                $"worktree remove \"{worktreePath}\"",
                null,
                default))
            .ReturnsAsync(new ProcessResult(0, "", ""));

        // Act
        var result = await _gitService.RemoveWorktreeAsync(worktreePath, false);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveWorktreeAsync_WithForce_ExecutesForceRemoval()
    {
        // Arrange
        var worktreePath = "/path/to/worktree";
        _mockProcessRunner
            .Setup(x => x.RunAsync(
                "git",
                $"worktree remove --force \"{worktreePath}\"",
                null,
                default))
            .ReturnsAsync(new ProcessResult(0, "", ""));

        // Act
        var result = await _gitService.RemoveWorktreeAsync(worktreePath, true);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveWorktreeAsync_WhenWorktreeNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var worktreePath = "/path/to/nonexistent";
        _mockProcessRunner
            .Setup(x => x.RunAsync(
                "git",
                It.IsAny<string>(),
                null,
                default))
            .ReturnsAsync(new ProcessResult(1, "", "fatal: '/path/to/nonexistent' is not a working tree"));

        // Act
        var result = await _gitService.RemoveWorktreeAsync(worktreePath, false);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("WT-RM-001");
        result.ErrorMessage!.ShouldContain("not found");
    }

    [Fact]
    public async Task RemoveWorktreeAsync_WhenUncommittedChanges_ReturnsUncommittedChangesError()
    {
        // Arrange
        var worktreePath = "/path/to/worktree";
        _mockProcessRunner
            .Setup(x => x.RunAsync(
                "git",
                It.IsAny<string>(),
                null,
                default))
            .ReturnsAsync(new ProcessResult(1, "", "fatal: '/path/to/worktree' contains modified or untracked files, use --force to delete it"));

        // Act
        var result = await _gitService.RemoveWorktreeAsync(worktreePath, false);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("WT-RM-003");
        result.ErrorMessage!.ShouldContain("uncommitted changes");
    }

    [Fact]
    public async Task RemoveWorktreeAsync_WhenLocked_ReturnsLockedError()
    {
        // Arrange
        var worktreePath = "/path/to/worktree";
        _mockProcessRunner
            .Setup(x => x.RunAsync(
                "git",
                It.IsAny<string>(),
                null,
                default))
            .ReturnsAsync(new ProcessResult(1, "", "fatal: '/path/to/worktree' is locked"));

        // Act
        var result = await _gitService.RemoveWorktreeAsync(worktreePath, false);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("WT-RM-005");
        result.ErrorMessage!.ShouldContain("locked");
    }

    [Fact]
    public async Task RemoveWorktreeAsync_WhenUnknownError_ReturnsGenericError()
    {
        // Arrange
        var worktreePath = "/path/to/worktree";
        _mockProcessRunner
            .Setup(x => x.RunAsync(
                "git",
                It.IsAny<string>(),
                null,
                default))
            .ReturnsAsync(new ProcessResult(1, "", "fatal: unknown error occurred"));

        // Act
        var result = await _gitService.RemoveWorktreeAsync(worktreePath, false);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.GitCommandFailed);
        result.ErrorMessage!.ShouldContain("Failed to remove worktree");
    }

    [Fact]
    public async Task RemoveWorktreeAsync_WithSpecialCharactersInPath_QuotesPathCorrectly()
    {
        // Arrange
        var worktreePath = "/path/with spaces/worktree";
        _mockProcessRunner
            .Setup(x => x.RunAsync(
                "git",
                $"worktree remove \"{worktreePath}\"",
                null,
                default))
            .ReturnsAsync(new ProcessResult(0, "", ""));

        // Act
        var result = await _gitService.RemoveWorktreeAsync(worktreePath, false);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _mockProcessRunner.Verify(
            x => x.RunAsync(
                "git",
                It.Is<string>(s => s.Contains($"\"{worktreePath}\"")),
                null,
                default),
            Times.Once);
    }

    #endregion

    #region GetRemotesAsync

    [Fact]
    public async Task GetRemotesAsync_WithRemotes_ReturnsList()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "remote", null, default))
            .ReturnsAsync(new ProcessResult(0, "origin\nupstream\n", ""));

        // Act
        var result = await _gitService.GetRemotesAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data.Count.ShouldBe(2);
        result.Data.ShouldContain("origin");
        result.Data.ShouldContain("upstream");
    }

    [Fact]
    public async Task GetRemotesAsync_WithEmptyOutput_ReturnsEmptyList()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "remote", null, default))
            .ReturnsAsync(new ProcessResult(0, "", ""));

        // Act
        var result = await _gitService.GetRemotesAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetRemotesAsync_GitError_ReturnsFailure()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "remote", null, default))
            .ReturnsAsync(new ProcessResult(128, "", "fatal: not a git repository"));

        // Act
        var result = await _gitService.GetRemotesAsync();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldNotBeNullOrEmpty();
    }

    #endregion

    #region GetRemoteTrackingBranchesAsync

    [Fact]
    public async Task GetRemoteTrackingBranchesAsync_ParsesOutput_ReturnsAllBranches()
    {
        // Arrange
        var output = "  origin/main\n  origin/feature/review-me\n  upstream/main\n";
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "branch -r", null, default))
            .ReturnsAsync(new ProcessResult(0, output, ""));

        // Act
        var result = await _gitService.GetRemoteTrackingBranchesAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetRemoteTrackingBranchesAsync_SkipsHeadLines()
    {
        // Arrange
        var output = "  origin/HEAD -> origin/main\n  origin/main\n  origin/feature\n";
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "branch -r", null, default))
            .ReturnsAsync(new ProcessResult(0, output, ""));

        // Act
        var result = await _gitService.GetRemoteTrackingBranchesAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data.ShouldNotContain(x => x.BranchName.Contains("HEAD"));
        result.Data.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetRemoteTrackingBranchesAsync_WithBranchFilter_ReturnsMatchingBranches()
    {
        // Arrange
        var output = "  origin/main\n  origin/feature/review-me\n  upstream/feature/review-me\n";
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "branch -r", null, default))
            .ReturnsAsync(new ProcessResult(0, output, ""));

        // Act
        var result = await _gitService.GetRemoteTrackingBranchesAsync("feature/review-me");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data!.Count.ShouldBe(2);
        result.Data.ShouldAllBe(x => x.BranchName == "feature/review-me");
    }

    [Fact]
    public async Task GetRemoteTrackingBranchesAsync_ParsesBranchWithMultipleSlashes()
    {
        // Arrange
        var output = "  origin/feature/sub/deep\n";
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "branch -r", null, default))
            .ReturnsAsync(new ProcessResult(0, output, ""));

        // Act
        var result = await _gitService.GetRemoteTrackingBranchesAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data!.Count.ShouldBe(1);
        result.Data[0].RemoteName.ShouldBe("origin");
        result.Data[0].BranchName.ShouldBe("feature/sub/deep");
        result.Data[0].FullRef.ShouldBe("origin/feature/sub/deep");
    }

    #endregion

    #region GetBranchUpstreamRemoteAsync

    [Fact]
    public async Task GetBranchUpstreamRemoteAsync_WithUpstream_ReturnsRemoteName()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "config branch.main.remote", null, default))
            .ReturnsAsync(new ProcessResult(0, "origin\n", ""));

        // Act
        var result = await _gitService.GetBranchUpstreamRemoteAsync("main");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBe("origin");
    }

    [Fact]
    public async Task GetBranchUpstreamRemoteAsync_WithoutUpstream_ReturnsNull()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "config branch.feature.remote", null, default))
            .ReturnsAsync(new ProcessResult(1, "", ""));

        // Act
        var result = await _gitService.GetBranchUpstreamRemoteAsync("feature");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBeNull();
    }

    [Fact]
    public async Task GetBranchUpstreamRemoteAsync_GitError_ReturnsFailure()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "config branch.main.remote", null, default))
            .ReturnsAsync(new ProcessResult(128, "", "fatal: not a git repository"));

        // Act
        var result = await _gitService.GetBranchUpstreamRemoteAsync("main");

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    #endregion

    #region FetchFromRemoteAsync

    [Fact]
    public async Task FetchFromRemoteAsync_Success_ReturnsSuccess()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "fetch \"origin\"", null, default))
            .ReturnsAsync(new ProcessResult(0, "", ""));

        // Act
        var result = await _gitService.FetchFromRemoteAsync("origin");

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task FetchFromRemoteAsync_Failure_ReturnsMappedError()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "fetch \"origin\"", null, default))
            .ReturnsAsync(new ProcessResult(1, "", "error: could not fetch origin"));

        // Act
        var result = await _gitService.FetchFromRemoteAsync("origin");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(ErrorCodes.RemoteFetchFailed);
        result.Solution.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task FetchFromRemoteAsync_Failure_CapturesStderr()
    {
        // Arrange
        var stderrMessage = "error: could not resolve hostname 'example.com'";
        _mockProcessRunner
            .Setup(x => x.RunAsync("git", "fetch \"origin\"", null, default))
            .ReturnsAsync(new ProcessResult(1, "", stderrMessage));

        // Act
        var result = await _gitService.FetchFromRemoteAsync("origin");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Solution.ShouldNotBeNullOrEmpty();
    }

    #endregion

    #region AddWorktreeFromRemoteAsync

    [Fact]
    public async Task AddWorktreeFromRemoteAsync_Success_CallsCorrectArgs()
    {
        // Arrange
        var worktreePath = "/tmp/feature-review";
        var branchName = "feature/review-me";
        var remoteName = "origin";
        _mockProcessRunner
            .Setup(x => x.RunAsync(
                "git",
                It.Is<string>(s => s.Contains("worktree add --track") && s.Contains(branchName) && s.Contains(remoteName)),
                null,
                default))
            .ReturnsAsync(new ProcessResult(0, "", ""));

        // Act
        var result = await _gitService.AddWorktreeFromRemoteAsync(worktreePath, branchName, remoteName);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task AddWorktreeFromRemoteAsync_Failure_ReturnsFailure()
    {
        // Arrange
        _mockProcessRunner
            .Setup(x => x.RunAsync(
                "git",
                It.Is<string>(s => s.Contains("worktree add --track")),
                null,
                default))
            .ReturnsAsync(new ProcessResult(128, "", "error: pathspec 'origin/feature/x' did not match any file(s)"));

        // Act
        var result = await _gitService.AddWorktreeFromRemoteAsync("/tmp/x", "feature/x", "origin");

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    #endregion
}
