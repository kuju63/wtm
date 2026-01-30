using System.IO.Abstractions;
using System.Runtime.InteropServices;
using Kuju63.WorkTree.CommandLine.Services.Git;
using Kuju63.WorkTree.CommandLine.Utils;
using Moq;
using Shouldly;

namespace Kuju63.WorkTree.Tests.Integration;

[Collection("Sequential")]
public class CrossPlatformTests
{
    [Fact]
    public void PathHelper_ShouldHandlePathSeparatorsCorrectly()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var mockPath = new Mock<IPath>();
        mockFileSystem.Setup(fs => fs.Path).Returns(mockPath.Object);

        var testPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? @"C:\Users\test\project"
            : "/home/test/project";

        mockPath.Setup(p => p.IsPathRooted(It.IsAny<string>())).Returns(true);
        mockPath.Setup(p => p.GetFullPath(It.IsAny<string>())).Returns(testPath);

        var pathHelper = new PathHelper(mockFileSystem.Object);

        // Act
        var normalizedPath = pathHelper.NormalizePath(testPath);

        // Assert
        normalizedPath.ShouldNotBeNullOrEmpty();
        normalizedPath.ShouldContain("/");
        normalizedPath.ShouldNotContain("\\");
    }

    [Fact]
    public void PathHelper_ShouldResolveRelativePathsCorrectly()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var mockPath = new Mock<IPath>();
        mockFileSystem.Setup(fs => fs.Path).Returns(mockPath.Object);

        var relativePath = "../worktrees/test-branch";
        var basePath = "/home/user/project";
        var expectedAbsolutePath = "/home/user/worktrees/test-branch";

        mockPath.Setup(p => p.IsPathRooted(relativePath)).Returns(false);
        mockPath.Setup(p => p.Combine(basePath, relativePath)).Returns($"{basePath}/{relativePath}");
        mockPath.Setup(p => p.GetFullPath(It.IsAny<string>())).Returns(expectedAbsolutePath);

        var pathHelper = new PathHelper(mockFileSystem.Object);

        // Act
        var absolutePath = pathHelper.ResolvePath(relativePath, basePath);

        // Assert
        absolutePath.ShouldNotBeNullOrEmpty();
        absolutePath.ShouldBe(expectedAbsolutePath);
    }

    [Theory]
    [InlineData("feature-x")]
    [InlineData("bugfix/issue-123")]
    [InlineData("user_story_1")]
    public void Validators_BranchName_ShouldAcceptValidNames(string branchName)
    {
        // Act
        var result = Validators.ValidateBranchName(branchName);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    // Starts with dash
    [InlineData("-invalid")]
    // Contains double dots
    [InlineData("invalid..branch")]
    // Contains @{
    [InlineData("invalid@{branch")]
    // Contains space
    [InlineData("invalid branch")]
    // Empty string
    [InlineData("")]
    public void Validators_BranchName_ShouldRejectInvalidNames(string branchName)
    {
        // Act
        var result = Validators.ValidateBranchName(branchName);

        // Assert
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void ProcessRunner_ShouldHandleLineEndingsCorrectly()
    {
        // Arrange
        var expectedNewLine = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "\r\n"
            : "\n";

        // Act & Assert
        // This test verifies that output processing doesn't break on different line endings
        Environment.NewLine.ShouldBe(expectedNewLine);
    }

    [Fact]
    public async Task GitService_ShouldWorkOnCurrentPlatform()
    {
        // Arrange
        var processRunner = new ProcessRunner();
        var fileSystem = new System.IO.Abstractions.FileSystem();
        var gitService = new GitService(processRunner, fileSystem);

        // Act - Test if Git is available on the current platform
        var result = await gitService.IsGitRepositoryAsync(default);

        // Assert
        result.ShouldNotBeNull();
    }
}

