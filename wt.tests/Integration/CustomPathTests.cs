using System.IO.Abstractions;
using Kuju63.WorkTree.CommandLine.Models;
using Kuju63.WorkTree.CommandLine.Services.Git;
using Kuju63.WorkTree.CommandLine.Services.Worktree;
using Kuju63.WorkTree.CommandLine.Utils;
using Shouldly;

namespace Kuju63.WorkTree.Tests.Integration;

[Collection("Sequential Integration Tests")]
public class CustomPathTests : IDisposable
{
    private readonly string _testRepoPath;
    private readonly string _customWorktreePath;
    private readonly string _relativeWorktreePath;
    private readonly string _originalDirectory;

    public CustomPathTests()
    {
        // Ensure we start from a valid directory
        try
        {
            var currentDir = Environment.CurrentDirectory;
            if (!Directory.Exists(currentDir))
            {
                Environment.CurrentDirectory = Path.GetTempPath();
            }
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Ignored IO error setting current directory in test ctor: {ex.Message}");
            Environment.CurrentDirectory = Path.GetTempPath();
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.Error.WriteLine($"Ignored permission error setting current directory in test ctor: {ex.Message}");
            Environment.CurrentDirectory = Path.GetTempPath();
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"Ignored argument error setting current directory in test ctor: {ex.Message}");
            Environment.CurrentDirectory = Path.GetTempPath();
        }
        catch (Exception ex)
        {
            // Fallback for unexpected exceptions: log and use temp directory.
            Console.Error.WriteLine($"Ignored unexpected error setting current directory in test ctor: {ex.Message}");
            Environment.CurrentDirectory = Path.GetTempPath();
        }

        _originalDirectory = Environment.CurrentDirectory;
        _testRepoPath = Path.Combine(Path.GetTempPath(), $"test-repo-{Guid.NewGuid()}");
        _customWorktreePath = Path.Combine(Path.GetTempPath(), $"custom-worktree-{Guid.NewGuid()}");
        _relativeWorktreePath = Path.Combine(_testRepoPath, "..", $"relative-worktree-{Guid.NewGuid()}");

        // Create test repository
        Directory.CreateDirectory(_testRepoPath);
        var processRunner = new ProcessRunner();

        // Initialize git repo
        processRunner.RunAsync("git", "init", _testRepoPath).Wait();
        processRunner.RunAsync("git", "config user.email \"test@example.com\"", _testRepoPath).Wait();
        processRunner.RunAsync("git", "config user.name \"Test User\"", _testRepoPath).Wait();
        processRunner.RunAsync("git", "checkout -b main", _testRepoPath).Wait(); // Ensure we're on main branch

        // Create initial commit
        var readmePath = Path.Combine(_testRepoPath, "README.md");
        File.WriteAllText(readmePath, "# Test Repository");
        processRunner.RunAsync("git", "add README.md", _testRepoPath).Wait();
        processRunner.RunAsync("git", "commit -m \"Initial commit\"", _testRepoPath).Wait();
    }

    public void Dispose()
    {
        RestoreOriginalDirectory();
        DeleteDirectorySafely(_testRepoPath);
        DeleteDirectorySafely(_customWorktreePath);
        DeleteDirectorySafely(_relativeWorktreePath);
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
        }
        catch
        {
            // If original directory no longer exists, change to temp
            Environment.CurrentDirectory = Path.GetTempPath();
        }
    }

    private void DeleteDirectorySafely(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Ignored IO error deleting directory '{path}': {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.Error.WriteLine($"Ignored permission error deleting directory '{path}': {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Ignored error deleting directory '{path}': {ex.Message}");
        }
    }

    [Fact]
    public async Task CreateWorktree_WithAbsolutePath_ShouldCreateWorktreeAtSpecifiedPath()
    {
        // Arrange
        var worktreeService = CreateWorktreeService();
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-absolute",
            WorktreePath = _customWorktreePath
        };

        // Act and Assert
        await TestWorktreeCreationAsync(_testRepoPath, async () =>
        {
            var result = await worktreeService.CreateWorktreeAsync(options);
            result.IsSuccess.ShouldBeTrue();
            result.Data.ShouldNotBeNull();
            result.Data!.Path.ShouldBe(_customWorktreePath);
            Directory.Exists(_customWorktreePath).ShouldBeTrue();
        });
    }

    [Fact]
    public async Task CreateWorktree_WithRelativePath_ShouldCreateWorktreeAtSpecifiedPath()
    {
        // Arrange
        var processRunner = new ProcessRunner();
        var fileSystem = new FileSystem();
        var pathHelper = new PathHelper(fileSystem);
        var gitService = new GitService(processRunner, fileSystem);
        var worktreeService = new WorktreeService(gitService, pathHelper);

        var relativePath = $"../relative-worktree-{Guid.NewGuid()}";
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-relative",
            WorktreePath = relativePath
        };

        // Change to test repo directory
        var originalDir = Environment.CurrentDirectory;
        Environment.CurrentDirectory = _testRepoPath;

        try
        {
            // Act
            var result = await worktreeService.CreateWorktreeAsync(options);


            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Data.ShouldNotBeNull();

            // Verify worktree directory exists
            Directory.Exists(result.Data!.Path).ShouldBeTrue();

            // Verify the directory name matches the expected pattern
            var directoryName = Path.GetFileName(result.Data.Path);
            directoryName.StartsWith("relative-worktree-").ShouldBeTrue();

            // Verify git worktree was created (should have .git file)
            var gitFile = Path.Combine(result.Data.Path, ".git");
            File.Exists(gitFile).ShouldBeTrue("worktree should have .git file");

            // Clean up
            if (Directory.Exists(result.Data.Path))
            {
                Directory.Delete(result.Data.Path, true);
            }
        }
        finally
        {
            try
            {
                if (Directory.Exists(originalDir))
                {
                    Environment.CurrentDirectory = originalDir;
                }
                else
                {
                    Environment.CurrentDirectory = Path.GetTempPath();
                }
            }
            catch
            {
                Console.Error.WriteLine($"Ignored error restoring current directory");
                Environment.CurrentDirectory = Path.GetTempPath();
            }
        }
    }

    [Fact]
    public async Task CreateWorktree_WithInvalidPath_ShouldReturnError()
    {
        // Arrange
        var processRunner = new ProcessRunner();
        var fileSystem = new FileSystem();
        var pathHelper = new PathHelper(fileSystem);
        var gitService = new GitService(processRunner, fileSystem);
        var worktreeService = new WorktreeService(gitService, pathHelper);

        var invalidPath = "/nonexistent/parent/directory/worktree";
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-invalid",
            WorktreePath = invalidPath
        };

        // Change to test repo directory
        var originalDir = Environment.CurrentDirectory;
        Environment.CurrentDirectory = _testRepoPath;

        try
        {
            // Act
            var result = await worktreeService.CreateWorktreeAsync(options);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ErrorCode.ShouldBe(ErrorCodes.InvalidPath);
        }
        finally
        {
            try
            {
                if (Directory.Exists(originalDir))
                {
                    Environment.CurrentDirectory = originalDir;
                }
                else
                {
                    Environment.CurrentDirectory = Path.GetTempPath();
                }
            }
            catch
            {
                Console.Error.WriteLine("Ignored error restoring current directory");
                Environment.CurrentDirectory = Path.GetTempPath();
            }
        }
    }

    [Fact]
    public async Task CreateWorktree_WithExistingDirectory_ShouldReturnError()
    {
        // Arrange
        var processRunner = new ProcessRunner();
        var fileSystem = new FileSystem();
        var pathHelper = new PathHelper(fileSystem);
        var gitService = new GitService(processRunner, fileSystem);
        var worktreeService = new WorktreeService(gitService, pathHelper);

        // Create existing directory
        Directory.CreateDirectory(_customWorktreePath);

        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-existing",
            WorktreePath = _customWorktreePath
        };

        // Change to test repo directory
        var originalDir = Environment.CurrentDirectory;
        Environment.CurrentDirectory = _testRepoPath;

        try
        {
            // Act
            var result = await worktreeService.CreateWorktreeAsync(options);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.ErrorCode.ShouldBe(ErrorCodes.InvalidPath);
        }
        finally
        {
            try
            {
                if (Directory.Exists(originalDir))
                {
                    Environment.CurrentDirectory = originalDir;
                }
                else
                {
                    Environment.CurrentDirectory = Path.GetTempPath();
                }
            }
            catch
            {
                Environment.CurrentDirectory = Path.GetTempPath();
            }
        }
    }

    [Fact]
    public async Task CreateWorktree_WithPathContainingSpaces_ShouldCreateSuccessfully()
    {
        // Arrange
        var worktreeService = CreateWorktreeService();
        var pathWithSpaces = Path.Combine(Path.GetTempPath(), $"path with spaces-{Guid.NewGuid()}");
        var options = new CreateWorktreeOptions
        {
            BranchName = "feature-spaces",
            WorktreePath = pathWithSpaces
        };

        // Act and Assert
        await TestWorktreeCreationAsync(_testRepoPath, async () =>
        {
            var result = await worktreeService.CreateWorktreeAsync(options);
            result.IsSuccess.ShouldBeTrue($"Expected success but got error: {result.ErrorMessage}");
            result.Data.ShouldNotBeNull();
            result.Data!.Path.ShouldBe(pathWithSpaces);
            Directory.Exists(pathWithSpaces).ShouldBeTrue();

            // Clean up
            if (Directory.Exists(pathWithSpaces))
            {
                Directory.Delete(pathWithSpaces, true);
            }
        });
    }

    private WorktreeService CreateWorktreeService()
    {
        var processRunner = new ProcessRunner();
        var fileSystem = new FileSystem();
        var pathHelper = new PathHelper(fileSystem);
        var gitService = new GitService(processRunner, fileSystem);
        return new WorktreeService(gitService, pathHelper);
    }

    private async Task TestWorktreeCreationAsync(string testRepoPath, Func<Task> testAction)
    {
        var originalDir = Environment.CurrentDirectory;
        Environment.CurrentDirectory = testRepoPath;

        try
        {
            await testAction();
        }
        finally
        {
            SafeRestoreDirectory(originalDir);
        }
    }

    private void SafeRestoreDirectory(string originalDir)
    {
        try
        {
            if (Directory.Exists(originalDir))
            {
                Environment.CurrentDirectory = originalDir;
            }
            else
            {
                Environment.CurrentDirectory = Path.GetTempPath();
            }
        }
        catch
        {
            Environment.CurrentDirectory = Path.GetTempPath();
        }
    }
}
