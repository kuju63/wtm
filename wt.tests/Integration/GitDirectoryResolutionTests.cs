using Kuju63.WorkTree.CommandLine.Services.Git;
using Kuju63.WorkTree.CommandLine.Utils;
using Shouldly;

namespace Kuju63.WorkTree.Tests.Integration;

/// <summary>
/// Integration tests for .git directory resolution in worktree scenarios.
/// These tests verify that the GitService correctly handles the case where
/// .git is a file (pointing to a git directory) rather than a directory itself.
/// </summary>
[Collection("Sequential Integration Tests")]
public class GitDirectoryResolutionTests : IDisposable
{
    private readonly string _testRepoPath;
    private readonly string _worktreePath;
    private readonly string _originalDirectory;

    /// <summary>
    /// Resolves a path to its real absolute path, resolving symlinks if possible.
    /// Uses managed code approach via FileInfo to avoid P/Invoke.
    /// </summary>
    private static string GetRealPath(string path)
    {
        try
        {
            // Normalize the path first
            var normalizedPath = Path.GetFullPath(path);

            // On macOS, /var is a symlink to /private/var
            // Normalize this common case to ensure path consistency
            if (OperatingSystem.IsMacOS() && normalizedPath.StartsWith("/var/"))
            {
                normalizedPath = "/private" + normalizedPath;
            }

            // Use FileInfo/DirectoryInfo which can resolve some symlinks on both Windows and Unix
            if (File.Exists(normalizedPath))
            {
                return new FileInfo(normalizedPath).FullName;
            }
            else if (Directory.Exists(normalizedPath))
            {
                return new DirectoryInfo(normalizedPath).FullName;
            }

            // If path doesn't exist yet, return the normalized path
            return normalizedPath;
        }
        catch
        {
            // Fallback to simple normalization if anything fails
            var fallbackPath = Path.GetFullPath(path);

            // Apply macOS /var normalization on fallback too
            if (OperatingSystem.IsMacOS() && fallbackPath.StartsWith("/var/"))
            {
                return "/private" + fallbackPath;
            }

            return fallbackPath;
        }
    }

    public GitDirectoryResolutionTests()
    {
        SafeSetCurrentDirectory();
        _originalDirectory = Environment.CurrentDirectory;

        // Use Path.GetTempPath() for cross-platform compatibility
        // GetRealPath resolves symlinks (e.g., /var -> /private/var on macOS)
        var testRepoPath = Path.Combine(Path.GetTempPath(), $"wt-git-dir-test-main-{Guid.NewGuid()}");
        Directory.CreateDirectory(testRepoPath);
        _testRepoPath = GetRealPath(testRepoPath);

        // Store worktree path template - normalize it now to ensure consistency
        var worktreeTempPath = Path.Combine(Path.GetTempPath(), $"wt-git-dir-test-worktree-{Guid.NewGuid()}");
        _worktreePath = GetRealPath(worktreeTempPath);

        InitializeGitRepository();
    }

    private void SafeSetCurrentDirectory()
    {
        try
        {
            var currentDir = Environment.CurrentDirectory;
            if (!Directory.Exists(currentDir))
            {
                Environment.CurrentDirectory = Path.GetTempPath();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error setting current directory in test ctor: {ex.Message}");
            Environment.CurrentDirectory = Path.GetTempPath();
        }
    }

    private void InitializeGitRepository()
    {
        Environment.CurrentDirectory = _testRepoPath;

        RunGitCommand("init");
        RunGitCommand("config user.email \"test@example.com\"");
        RunGitCommand("config user.name \"Test User\"");
        RunGitCommand("checkout -b main");

        var readmePath = Path.Combine(_testRepoPath, "README.md");
        File.WriteAllText(readmePath, "# Test Repository\n");
        RunGitCommand("add README.md");
        RunGitCommand("commit -m \"Initial commit\"");
    }

    private void RunGitCommand(string arguments)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = Environment.CurrentDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        process?.WaitForExit();

        if (process?.ExitCode != 0)
        {
            var error = process?.StandardError.ReadToEnd();
            throw new InvalidOperationException($"Git command failed: {arguments}\nError: {error}");
        }
    }

    public void Dispose()
    {
        RestoreOriginalDirectory();
        CleanupWorktrees();
        DeleteTestRepository();
        GC.SuppressFinalize(this);
    }

    private void RestoreOriginalDirectory()
    {
        try
        {
            if (Directory.Exists(_originalDirectory))
            {
                Environment.CurrentDirectory = _originalDirectory;
            }
            else
            {
                Environment.CurrentDirectory = Path.GetTempPath();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error restoring directory: {ex.Message}");
            try
            {
                Environment.CurrentDirectory = Path.GetTempPath();
            }
            catch
            {
                // Ignore if we can't set temp path either
            }
        }
    }

    private void CleanupWorktrees()
    {
        try
        {
            if (Directory.Exists(_testRepoPath))
            {
                Environment.CurrentDirectory = _testRepoPath;
                var result = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "worktree list --porcelain",
                    WorkingDirectory = _testRepoPath,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                result?.WaitForExit();
                var output = result?.StandardOutput.ReadToEnd() ?? "";

                var worktreePaths = output.Split('\n')
                                        .Where(l => l.StartsWith("worktree "))
                                        .Select(l => l.Substring(9).Trim())
                                        .Where(l => l != _testRepoPath)
                                        .Where(Directory.Exists)
                                        .ToArray();

                foreach (var path in worktreePaths.Where(p => !string.IsNullOrEmpty(p)))
                {
                    try
                    {
                        RunGitCommand($"worktree remove \"{path}\" --force");
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private void DeleteTestRepository()
    {
        try
        {
            if (Directory.Exists(_testRepoPath))
            {
                Directory.Delete(_testRepoPath, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }

        try
        {
            if (Directory.Exists(_worktreePath))
            {
                Directory.Delete(_worktreePath, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public async Task ListWorktreesAsync_WhenRunFromWorktree_ResolvesGitDirectoryCorrectly()
    {
        // Arrange - Create a worktree
        Environment.CurrentDirectory = _testRepoPath;
        RunGitCommand($"worktree add \"{_worktreePath}\" -b feature-test");

        // Switch to the worktree directory
        Environment.CurrentDirectory = _worktreePath;

        // Verify .git is a file, not a directory
        var gitPath = Path.Combine(_worktreePath, ".git");
        File.Exists(gitPath).ShouldBeTrue();
        Directory.Exists(gitPath).ShouldBeFalse();

        // Verify .git file contains gitdir reference
        var gitFileContent = File.ReadAllText(gitPath);
        gitFileContent.ShouldStartWith("gitdir:");

        // Act - List worktrees from within the worktree
        var processRunner = new ProcessRunner();
        var fileSystem = new System.IO.Abstractions.FileSystem();
        var gitService = new GitService(processRunner, fileSystem);
        var result = await gitService.ListWorktreesAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Count.ShouldBeGreaterThanOrEqualTo(2); // Main repo + our worktree

        // Verify both worktrees are listed
        // Use GetRealPath for comparison to handle symlinks across platforms
        var normalizedTestRepoPath = GetRealPath(_testRepoPath);
        var normalizedWorktreePath = GetRealPath(_worktreePath);
        var mainWorktree = result.Data.FirstOrDefault(w => GetRealPath(w.Path) == normalizedTestRepoPath);
        var featureWorktree = result.Data.FirstOrDefault(w => GetRealPath(w.Path) == normalizedWorktreePath);

        mainWorktree.ShouldNotBeNull();
        featureWorktree.ShouldNotBeNull();
        featureWorktree!.Branch.ShouldBe("feature-test");
    }

    [Fact]
    public async Task ListWorktreesAsync_WhenRunFromMainRepo_ListsWorktreesWithTimestamps()
    {
        // Arrange - Create a worktree
        Environment.CurrentDirectory = _testRepoPath;
        RunGitCommand($"worktree add \"{_worktreePath}\" -b feature-timestamp");

        // Wait a moment to ensure timestamps are different
        await Task.Delay(100);

        // Act - List worktrees from main repository
        var processRunner = new ProcessRunner();
        var fileSystem = new System.IO.Abstractions.FileSystem();
        var gitService = new GitService(processRunner, fileSystem);
        var result = await gitService.ListWorktreesAsync();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data!.Count.ShouldBeGreaterThanOrEqualTo(2);

        // Verify worktree has a creation timestamp
        // Use GetRealPath for comparison to handle symlinks across platforms
        var normalizedWorktreePath = GetRealPath(_worktreePath);
        var featureWorktree = result.Data.FirstOrDefault(w => GetRealPath(w.Path) == normalizedWorktreePath);
        featureWorktree.ShouldNotBeNull();

        // The timestamp should be recent (within last minute)
        var timeDiff = DateTime.Now - featureWorktree!.CreatedAt;
        timeDiff.TotalMinutes.ShouldBeLessThan(1);
    }

    [Fact]
    public void GitFile_InWorktree_ContainsValidGitdirReference()
    {
        // Arrange - Create a worktree
        Environment.CurrentDirectory = _testRepoPath;
        RunGitCommand($"worktree add \"{_worktreePath}\" -b feature-gitdir");

        // Act - Read .git file from worktree
        var gitFilePath = Path.Combine(_worktreePath, ".git");
        File.Exists(gitFilePath).ShouldBeTrue();

        var content = File.ReadAllText(gitFilePath);
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Assert
        lines.Length.ShouldBeGreaterThan(0);
        lines[0].ShouldStartWith("gitdir:");

        var gitDirPath = lines[0].Substring(7).Trim();
        gitDirPath.ShouldNotBeEmpty();

        // The path should point to .git/worktrees/<name> directory
        gitDirPath.ShouldContain("worktrees");

        // Verify the referenced directory exists
        if (Path.IsPathRooted(gitDirPath))
        {
            Directory.Exists(gitDirPath).ShouldBeTrue();
        }
    }

    [Fact]
    public async Task ListWorktreesAsync_WithMultipleWorktrees_ReturnsAllWorktrees()
    {
        // Arrange - Create multiple worktrees
        Environment.CurrentDirectory = _testRepoPath;

        // Create path templates - normalize them to ensure consistency with git output
        var worktree2Path = GetRealPath(Path.Combine(Path.GetTempPath(), $"wt-git-dir-test-worktree2-{Guid.NewGuid()}"));
        var worktree3Path = GetRealPath(Path.Combine(Path.GetTempPath(), $"wt-git-dir-test-worktree3-{Guid.NewGuid()}"));

        try
        {
            RunGitCommand($"worktree add \"{_worktreePath}\" -b feature-1");
            RunGitCommand($"worktree add \"{worktree2Path}\" -b feature-2");
            RunGitCommand($"worktree add \"{worktree3Path}\" -b feature-3");

            // Switch to one of the worktrees
            Environment.CurrentDirectory = _worktreePath;

            // Act - List worktrees from within a worktree
            var processRunner = new ProcessRunner();
            var fileSystem = new System.IO.Abstractions.FileSystem();
            var gitService = new GitService(processRunner, fileSystem);
            var result = await gitService.ListWorktreesAsync();

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Data.ShouldNotBeNull();
            result.Data!.Count.ShouldBe(4); // Main + 3 worktrees

            // Use GetRealPath for all path comparisons to handle symlinks across platforms
            var normalizedWorktreePaths = result.Data.Select(w => GetRealPath(w.Path)).ToList();
            var normalizedTestRepoPath = GetRealPath(_testRepoPath);
            var normalizedWorktreePath = GetRealPath(_worktreePath);
            var normalizedWorktree2Path = GetRealPath(worktree2Path);
            var normalizedWorktree3Path = GetRealPath(worktree3Path);

            normalizedWorktreePaths.ShouldContain(normalizedTestRepoPath);
            normalizedWorktreePaths.ShouldContain(normalizedWorktreePath);
            normalizedWorktreePaths.ShouldContain(normalizedWorktree2Path);
            normalizedWorktreePaths.ShouldContain(normalizedWorktree3Path);
        }
        finally
        {
            // Cleanup additional worktrees
            try
            {
                Environment.CurrentDirectory = _testRepoPath;
                if (Directory.Exists(worktree2Path))
                {
                    RunGitCommand($"worktree remove \"{worktree2Path}\" --force");
                    Directory.Delete(worktree2Path, recursive: true);
                }
                if (Directory.Exists(worktree3Path))
                {
                    RunGitCommand($"worktree remove \"{worktree3Path}\" --force");
                    Directory.Delete(worktree3Path, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
