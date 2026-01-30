using System.CommandLine;
using System.CommandLine.Parsing;
using Kuju63.WorkTree.CommandLine.Models;
using Kuju63.WorkTree.CommandLine.Services.Worktree;

namespace Kuju63.WorkTree.CommandLine.Commands.Worktree;

/// <summary>
/// Command to remove a git worktree and delete its working directory.
/// </summary>
public class RemoveCommand : Command
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveCommand"/> class.
    /// </summary>
    /// <param name="worktreeService">The worktree service for removal operations.</param>
    public RemoveCommand(IWorktreeService worktreeService)
        : base("remove", "Remove a git worktree and delete its working directory")
    {
        var (worktreeArgument, forceOption, outputOption, verboseOption) = AddArgumentsAndOptions();

        this.SetAction(async (parseResult, cancellationToken) =>
        {
            var worktreeId = parseResult.GetValue(worktreeArgument);
            if (string.IsNullOrEmpty(worktreeId))
            {
                parseResult.InvocationConfiguration.Error.WriteLine("Error: worktree argument is required");
                return 1;
            }

            var options = new RemoveWorktreeOptions
            {
                WorktreeIdentifier = worktreeId,
                Force = parseResult.GetValue(forceOption),
                OutputFormat = parseResult.GetValue(outputOption),
                Verbose = parseResult.GetValue(verboseOption)
            };

            var result = await worktreeService.RemoveWorktreeAsync(options, cancellationToken);

            if (result.IsSuccess)
            {
                DisplaySuccess(result.Data!, options.OutputFormat, parseResult.InvocationConfiguration.Output);
                return 0;
            }
            else
            {
                DisplayError(result, options.Verbose, parseResult.InvocationConfiguration.Error);
                return 1;
            }
        });
    }

    /// <summary>
    /// Adds command arguments and options for the remove command.
    /// </summary>
    /// <returns>A tuple containing the worktree argument and options for force, output format, and verbose mode.</returns>
    private (Argument<string>, Option<bool>, Option<OutputFormat>, Option<bool>) AddArgumentsAndOptions()
    {
        var worktreeArgument = new Argument<string>("worktree")
        {
            Description = "Worktree identifier (branch name or path)"
        };

        var forceOption = new Option<bool>("--force", "-f")
        {
            Description = "Force removal even with uncommitted changes or locks",
            DefaultValueFactory = _ => false
        };

        var outputOption = new Option<OutputFormat>("--output", "-o")
        {
            Description = "Output format (human or json)",
            DefaultValueFactory = _ => OutputFormat.Human
        };

        var verboseOption = new Option<bool>("--verbose", "-v")
        {
            Description = "Show detailed diagnostic information",
            DefaultValueFactory = _ => false
        };

        this.Arguments.Add(worktreeArgument);
        this.Options.Add(forceOption);
        this.Options.Add(outputOption);
        this.Options.Add(verboseOption);

        return (worktreeArgument, forceOption, outputOption, verboseOption);
    }

    /// <summary>
    /// Displays the success result of a worktree removal operation.
    /// </summary>
    /// <param name="result">The removal result containing details about the removed worktree.</param>
    /// <param name="format">The output format (Human or Json).</param>
    /// <param name="output">The output writer to write the result to.</param>
    private static void DisplaySuccess(RemoveWorktreeResult result, OutputFormat format, TextWriter output)
    {
        if (format == OutputFormat.Json)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(new
            {
                success = result.Success,
                worktree = result.WorktreeId,
                path = result.RemovedPath,
                worktreeMetadataRemoved = result.WorktreeMetadataRemoved,
                filesDeleted = result.FilesDeleted,
                undeleteableItems = result.DeletionFailures.Select(f => new
                {
                    path = f.FilePath,
                    reason = f.Reason
                }),
                duration = result.Duration.TotalSeconds.ToString("F3") + "s"
            }, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            output.WriteLine(json);
        }
        else
        {
            if (result.DeletionFailures.Count > 0)
            {
                output.WriteLine($"⚠ Worktree '{result.WorktreeId}' removed, but {result.DeletionFailures.Count} files could not be deleted");
                output.WriteLine($"  Path: {result.RemovedPath}");
                output.WriteLine($"  Worktree metadata: Removed");
                output.WriteLine($"  Files deleted: {result.FilesDeleted}");
                output.WriteLine();
                output.WriteLine("Remaining files (manual cleanup required):");
                foreach (var failure in result.DeletionFailures)
                {
                    output.WriteLine($"  • {failure.FilePath} ({failure.Reason})");
                }
            }
            else
            {
                output.WriteLine($"✓ Worktree '{result.WorktreeId}' removed successfully");
                output.WriteLine($"  Path: {result.RemovedPath}");
                if (result.FilesDeleted > 0)
                {
                    output.WriteLine($"  Deleted: {result.FilesDeleted:N0} files");
                }
            }
        }
    }

    /// <summary>
    /// Displays the error result of a worktree removal operation.
    /// </summary>
    /// <param name="result">The command result containing error details.</param>
    /// <param name="verbose">Whether to display detailed diagnostic information.</param>
    /// <param name="error">The error writer to write the error message to.</param>
    private static void DisplayError(CommandResult<RemoveWorktreeResult> result, bool verbose, TextWriter error)
    {
        error.WriteLine($"✗ {result.ErrorMessage}");

        if (!string.IsNullOrEmpty(result.Solution))
        {
            error.WriteLine($"  {result.Solution}");
        }

        if (verbose && !string.IsNullOrEmpty(result.ErrorCode))
        {
            error.WriteLine($"  Error Code: {result.ErrorCode}");
        }
    }
}
