using System.CommandLine;
using Kuju63.WorkTree.CommandLine.Commands.Worktree;
using Kuju63.WorkTree.CommandLine.Models;
using Kuju63.WorkTree.CommandLine.Services.Worktree;
using Moq;
using Shouldly;

namespace Kuju63.WorkTree.Tests.Commands.Worktree;

/// <summary>
/// Tests for RemoveCommand.
/// </summary>
public class RemoveCommandTests
{
    private readonly Mock<IWorktreeService> _mockWorktreeService;
    private readonly RemoveCommand _command;
    private readonly StringWriter _outputWriter;
    private readonly StringWriter _errorWriter;
    private readonly InvocationConfiguration _invocationConfig;

    public RemoveCommandTests()
    {
        _mockWorktreeService = new Mock<IWorktreeService>();
        _command = new RemoveCommand(_mockWorktreeService.Object);
        _outputWriter = new StringWriter();
        _errorWriter = new StringWriter();
        _invocationConfig = new InvocationConfiguration
        {
            Output = _outputWriter,
            Error = _errorWriter
        };
    }

    [Fact]
    public async Task RemoveCommand_ParsesWorktreeArgument()
    {
        // Arrange
        var result = new RemoveWorktreeResult
        {
            Success = true,
            WorktreeId = "feature-x",
            RemovedPath = "/repo-feature-x",
            WorktreeMetadataRemoved = true,
            FilesDeleted = 100
        };

        _mockWorktreeService
            .Setup(x => x.RemoveWorktreeAsync(It.IsAny<RemoveWorktreeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<RemoveWorktreeResult>.Success(result));

        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(_command);

        // Act
        var parseResult = rootCommand.Parse(new[] { "remove", "feature-x" });
        var exitCode = await parseResult.InvokeAsync(_invocationConfig);

        // Assert
        exitCode.ShouldBe(0);
        _mockWorktreeService.Verify(
            x => x.RemoveWorktreeAsync(
                It.Is<RemoveWorktreeOptions>(o => o.WorktreeIdentifier == "feature-x"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveCommand_HumanOutput_DisplaysSuccessMessage()
    {
        // Arrange
        var result = new RemoveWorktreeResult
        {
            Success = true,
            WorktreeId = "feature-x",
            RemovedPath = "/repo-feature-x",
            WorktreeMetadataRemoved = true,
            FilesDeleted = 100
        };

        _mockWorktreeService
            .Setup(x => x.RemoveWorktreeAsync(It.IsAny<RemoveWorktreeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<RemoveWorktreeResult>.Success(result));

        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(_command);

        // Act
        var parseResult = rootCommand.Parse(new[] { "remove", "feature-x" });
        var exitCode = await parseResult.InvokeAsync(_invocationConfig);

        // Assert
        exitCode.ShouldBe(0);
        var output = _outputWriter.ToString();
        output.ShouldContain("feature-x");
        output.ShouldContain("removed successfully");
    }

    [Fact]
    public async Task RemoveCommand_JsonOutput_ReturnsStructuredResult()
    {
        // Arrange
        var result = new RemoveWorktreeResult
        {
            Success = true,
            WorktreeId = "feature-x",
            RemovedPath = "/repo-feature-x",
            WorktreeMetadataRemoved = true,
            FilesDeleted = 100,
            Duration = TimeSpan.FromMilliseconds(250)
        };

        _mockWorktreeService
            .Setup(x => x.RemoveWorktreeAsync(It.IsAny<RemoveWorktreeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<RemoveWorktreeResult>.Success(result));

        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(_command);

        // Act
        var parseResult = rootCommand.Parse(new[] { "remove", "feature-x", "--output", "json" });
        var exitCode = await parseResult.InvokeAsync(_invocationConfig);

        // Assert
        exitCode.ShouldBe(0);
        var output = _outputWriter.ToString();
        output.ShouldContain("\"success\": true");
        output.ShouldContain("\"worktree\": \"feature-x\"");
        output.ShouldContain("\"worktreeMetadataRemoved\": true");
    }

    [Fact]
    public async Task RemoveCommand_WhenNotFound_ReturnsErrorWithListSuggestion()
    {
        // Arrange
        _mockWorktreeService
            .Setup(x => x.RemoveWorktreeAsync(It.IsAny<RemoveWorktreeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<RemoveWorktreeResult>.Failure(
                "WT-RM-001",
                "Worktree 'non-existent' not found",
                "Use 'wt list' to see available worktrees"));

        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(_command);

        // Act
        var parseResult = rootCommand.Parse(new[] { "remove", "non-existent" });
        var exitCode = await parseResult.InvokeAsync(_invocationConfig);

        // Assert
        exitCode.ShouldBe(1);
        var error = _errorWriter.ToString();
        error.ShouldContain("not found");
        error.ShouldContain("wt list");
    }

    [Fact]
    public async Task RemoveCommand_ParsesForceFlag()
    {
        // Arrange
        var result = new RemoveWorktreeResult
        {
            Success = true,
            WorktreeId = "feature-x",
            RemovedPath = "/repo-feature-x",
            WorktreeMetadataRemoved = true,
            FilesDeleted = 100
        };

        _mockWorktreeService
            .Setup(x => x.RemoveWorktreeAsync(It.IsAny<RemoveWorktreeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult<RemoveWorktreeResult>.Success(result));

        var rootCommand = new RootCommand();
        rootCommand.Subcommands.Add(_command);

        // Act
        var parseResult = rootCommand.Parse(new[] { "remove", "feature-x", "--force" });
        var exitCode = await parseResult.InvokeAsync(_invocationConfig);

        // Assert
        exitCode.ShouldBe(0);
        _mockWorktreeService.Verify(
            x => x.RemoveWorktreeAsync(
                It.Is<RemoveWorktreeOptions>(o => o.Force == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
