using System.CommandLine;
using Kuju63.WorkTree.CommandLine.Commands.Worktree;
using Kuju63.WorkTree.CommandLine.Models;
using Kuju63.WorkTree.CommandLine.Services.Interaction;
using Kuju63.WorkTree.CommandLine.Services.Worktree;
using Moq;
using Shouldly;

namespace Kuju63.WorkTree.Tests.Commands.Worktree;

public class CheckoutCommandTests
{
    private readonly Mock<IWorktreeService> _mockWorktreeService;
    private readonly Mock<IInteractionService> _mockInteractionService;
    private readonly CheckoutCommand _checkoutCommand;

    public CheckoutCommandTests()
    {
        _mockWorktreeService = new Mock<IWorktreeService>();
        _mockInteractionService = new Mock<IInteractionService>();
        _checkoutCommand = new CheckoutCommand(_mockWorktreeService.Object, _mockInteractionService.Object);
    }

    private async Task<(int exitCode, string output, string error)> InvokeAsync(string[] args)
    {
        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(_checkoutCommand);

        var outputWriter = new StringWriter();
        var errorWriter = new StringWriter();

        var parseResult = rootCommand.Parse(args);
        var config = parseResult.InvocationConfiguration;
        config.Output = outputWriter;
        config.Error = errorWriter;

        var exitCode = await parseResult.InvokeAsync();
        return (exitCode, outputWriter.ToString(), errorWriter.ToString());
    }

    [Fact]
    public async Task CheckoutCommand_Success_PrintsPath()
    {
        // Arrange
        var worktreeInfo = new WorktreeInfo(
            "/path/to/feature-review",
            "feature/review-me",
            false,
            string.Empty,
            DateTime.UtcNow,
            true);

        _mockWorktreeService
            .Setup(s => s.CheckoutWorktreeAsync(
                It.IsAny<CheckoutWorktreeOptions>(),
                It.IsAny<IInteractionService>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<WorktreeInfo>.Success(worktreeInfo));

        // Act
        var (exitCode, output, _) = await InvokeAsync(["checkout", "feature/review-me"]);

        // Assert
        exitCode.ShouldBe(0);
        output.ShouldContain("/path/to/feature-review");
    }

    [Fact]
    public async Task CheckoutCommand_SuccessWithJsonOutput_ProducesJson()
    {
        // Arrange
        var worktreeInfo = new WorktreeInfo(
            "/path/to/feature-review",
            "feature/review-me",
            false,
            string.Empty,
            DateTime.UtcNow,
            true);

        _mockWorktreeService
            .Setup(s => s.CheckoutWorktreeAsync(
                It.IsAny<CheckoutWorktreeOptions>(),
                It.IsAny<IInteractionService>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<WorktreeInfo>.Success(worktreeInfo));

        // Act
        var (exitCode, output, _) = await InvokeAsync(["checkout", "feature/review-me", "--output", "json"]);

        // Assert
        exitCode.ShouldBe(0);
        output.ShouldContain("\"success\"");
        output.ShouldContain("true");
        output.ShouldContain("/path/to/feature-review");
    }

    [Fact]
    public async Task CheckoutCommand_Failure_ReturnsNonZeroExit()
    {
        // Arrange
        _mockWorktreeService
            .Setup(s => s.CheckoutWorktreeAsync(
                It.IsAny<CheckoutWorktreeOptions>(),
                It.IsAny<IInteractionService>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<WorktreeInfo>.Failure(
                ErrorCodes.BranchNotFoundAnywhere,
                "Branch not found",
                "Check your branch name"));

        // Act
        var (exitCode, _, error) = await InvokeAsync(["checkout", "feature/nonexistent"]);

        // Assert
        exitCode.ShouldNotBe(0);
        error.ShouldContain("Branch not found");
    }

    [Fact]
    public async Task CheckoutCommand_EditorFlag_PassedToOptions()
    {
        // Arrange
        CheckoutWorktreeOptions? capturedOptions = null;
        _mockWorktreeService
            .Setup(s => s.CheckoutWorktreeAsync(
                It.IsAny<CheckoutWorktreeOptions>(),
                It.IsAny<IInteractionService>(),
                It.IsAny<CancellationToken>()))
            .Callback<CheckoutWorktreeOptions, IInteractionService, CancellationToken>(
                (opts, _, _) => capturedOptions = opts)
            .ReturnsAsync(CommandResult<WorktreeInfo>.Success(
                new WorktreeInfo("/path", "feature/x", false, "", DateTime.UtcNow, true)));

        // Act
        await InvokeAsync(["checkout", "feature/x", "--editor", "vscode"]);

        // Assert
        capturedOptions.ShouldNotBeNull();
        capturedOptions.EditorType.ShouldBe(EditorType.VSCode);
    }

    [Fact]
    public async Task CheckoutCommand_FetchFlag_PassedToOptions()
    {
        // Arrange
        CheckoutWorktreeOptions? capturedOptions = null;
        _mockWorktreeService
            .Setup(s => s.CheckoutWorktreeAsync(
                It.IsAny<CheckoutWorktreeOptions>(),
                It.IsAny<IInteractionService>(),
                It.IsAny<CancellationToken>()))
            .Callback<CheckoutWorktreeOptions, IInteractionService, CancellationToken>(
                (opts, _, _) => capturedOptions = opts)
            .ReturnsAsync(CommandResult<WorktreeInfo>.Success(
                new WorktreeInfo("/path", "feature/x", false, "", DateTime.UtcNow, true)));

        // Act
        await InvokeAsync(["checkout", "feature/x", "--fetch"]);

        // Assert
        capturedOptions.ShouldNotBeNull();
        capturedOptions.Fetch.ShouldBeTrue();
    }

    [Fact]
    public async Task CheckoutCommand_RemoteFlag_ParsedAndPassedToOptions()
    {
        // Arrange
        CheckoutWorktreeOptions? capturedOptions = null;
        _mockWorktreeService
            .Setup(s => s.CheckoutWorktreeAsync(
                It.IsAny<CheckoutWorktreeOptions>(),
                It.IsAny<IInteractionService>(),
                It.IsAny<CancellationToken>()))
            .Callback<CheckoutWorktreeOptions, IInteractionService, CancellationToken>(
                (opts, _, _) => capturedOptions = opts)
            .ReturnsAsync(CommandResult<WorktreeInfo>.Success(
                new WorktreeInfo("/path", "feature/x", false, "", DateTime.UtcNow, true)));

        // Act
        await InvokeAsync(["checkout", "feature/x", "--remote", "origin"]);

        // Assert
        capturedOptions.ShouldNotBeNull();
        capturedOptions.Remote.ShouldBe("origin");
    }

    [Fact]
    public async Task CheckoutCommand_RM001Error_DisplayedCorrectly()
    {
        // Arrange
        _mockWorktreeService
            .Setup(s => s.CheckoutWorktreeAsync(
                It.IsAny<CheckoutWorktreeOptions>(),
                It.IsAny<IInteractionService>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<WorktreeInfo>.Failure(
                ErrorCodes.RemoteNotFound,
                "Remote 'typo' not found",
                ErrorCodes.GetSolution(ErrorCodes.RemoteNotFound)));

        // Act
        var (exitCode, _, error) = await InvokeAsync(["checkout", "feature/x", "--remote", "typo"]);

        // Assert
        exitCode.ShouldNotBe(0);
        error.ShouldContain("typo");
    }

    [Fact]
    public async Task CheckoutCommand_UserCancelled_ReturnsExitCode0()
    {
        // Arrange
        _mockWorktreeService
            .Setup(s => s.CheckoutWorktreeAsync(
                It.IsAny<CheckoutWorktreeOptions>(),
                It.IsAny<IInteractionService>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<WorktreeInfo>.Failure(
                ErrorCodes.UserCancelled,
                "User cancelled remote selection",
                ErrorCodes.GetSolution(ErrorCodes.UserCancelled)));

        // Act
        var (exitCode, _, _) = await InvokeAsync(["checkout", "feature/x"]);

        // Assert
        exitCode.ShouldBe(0);
    }

    [Fact]
    public async Task CheckoutCommand_Success_OutputsEnglishLabels()
    {
        // Arrange
        var worktreeInfo = new WorktreeInfo(
            "/path/to/wt-feature",
            "feature/x",
            false,
            string.Empty,
            DateTime.UtcNow,
            true);

        _mockWorktreeService
            .Setup(s => s.CheckoutWorktreeAsync(
                It.IsAny<CheckoutWorktreeOptions>(),
                It.IsAny<IInteractionService>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<WorktreeInfo>.Success(worktreeInfo));

        // Act
        var (exitCode, output, _) = await InvokeAsync(["checkout", "feature/x"]);

        // Assert
        exitCode.ShouldBe(0);
        output.ShouldContain("Branch:");
        output.ShouldContain("Path:");
        output.ShouldNotContain("ブランチ");
        output.ShouldNotContain("パス");
    }

    [Fact]
    public async Task CheckoutCommand_SuccessWithRemote_DisplaysRemoteInfo()
    {
        // Arrange
        var worktreeInfo = new WorktreeInfo(
            "/path/to/wt-feature",
            "feature/x",
            false,
            string.Empty,
            DateTime.UtcNow,
            true)
        { Remote = "origin" };

        _mockWorktreeService
            .Setup(s => s.CheckoutWorktreeAsync(
                It.IsAny<CheckoutWorktreeOptions>(),
                It.IsAny<IInteractionService>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<WorktreeInfo>.Success(worktreeInfo));

        // Act
        var (exitCode, output, _) = await InvokeAsync(["checkout", "feature/x"]);

        // Assert
        exitCode.ShouldBe(0);
        output.ShouldContain("Remote:");
        output.ShouldContain("origin");
    }

    [Fact]
    public async Task CheckoutCommand_SuccessWithJsonOutput_IncludesRemoteField()
    {
        // Arrange
        var worktreeInfo = new WorktreeInfo(
            "/path/to/wt-feature",
            "feature/x",
            false,
            string.Empty,
            DateTime.UtcNow,
            true)
        { Remote = "upstream" };

        _mockWorktreeService
            .Setup(s => s.CheckoutWorktreeAsync(
                It.IsAny<CheckoutWorktreeOptions>(),
                It.IsAny<IInteractionService>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<WorktreeInfo>.Success(worktreeInfo));

        // Act
        var (exitCode, output, _) = await InvokeAsync(["checkout", "feature/x", "--output", "json"]);

        // Assert
        exitCode.ShouldBe(0);
        output.ShouldContain("\"remote\"");
        output.ShouldContain("upstream");
    }

    [Fact]
    public async Task CheckoutCommand_Failure_DisplaysEnglishSolutionLabel()
    {
        // Arrange
        _mockWorktreeService
            .Setup(s => s.CheckoutWorktreeAsync(
                It.IsAny<CheckoutWorktreeOptions>(),
                It.IsAny<IInteractionService>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<WorktreeInfo>.Failure(
                ErrorCodes.BranchNotFoundAnywhere,
                "Branch not found",
                "Check your branch name"));

        // Act
        var (exitCode, _, error) = await InvokeAsync(["checkout", "feature/nonexistent"]);

        // Assert
        exitCode.ShouldNotBe(0);
        error.ShouldContain("Solution:");
        error.ShouldNotContain("解決方法");
    }
}
