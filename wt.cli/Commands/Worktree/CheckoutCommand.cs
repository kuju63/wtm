using System.CommandLine;
using Kuju63.WorkTree.CommandLine.Models;
using Kuju63.WorkTree.CommandLine.Services.Interaction;
using Kuju63.WorkTree.CommandLine.Services.Worktree;

namespace Kuju63.WorkTree.CommandLine.Commands.Worktree;

/// <summary>
/// Represents a command to check out an existing branch as a worktree.
/// </summary>
public class CheckoutCommand : Command
{
    public CheckoutCommand(IWorktreeService worktreeService, IInteractionService interactionService)
        : base("checkout", "Check out an existing branch as a worktree")
    {
        var (branchArgument, remoteOption, fetchOption, editorOption, outputOption, verboseOption) =
            AddArgumentsAndOptions();

        this.SetAction(async (parseResult, cancellationToken) =>
        {
            var branch = parseResult.GetValue(branchArgument);
            if (string.IsNullOrEmpty(branch))
            {
                parseResult.InvocationConfiguration.Error.WriteLine("Error: branch-name argument is required");
                return 1;
            }

            var options = new CheckoutWorktreeOptions
            {
                BranchName = branch,
                Remote = parseResult.GetValue(remoteOption),
                Fetch = parseResult.GetValue(fetchOption),
                EditorType = parseResult.GetValue(editorOption),
                OutputFormat = parseResult.GetValue(outputOption),
                Verbose = parseResult.GetValue(verboseOption)
            };

            var result = await worktreeService.CheckoutWorktreeAsync(options, interactionService, cancellationToken);

            if (result.IsSuccess)
            {
                DisplaySuccess(result.Data!, options.OutputFormat, parseResult.InvocationConfiguration.Output);
                foreach (var warning in result.Warnings)
                {
                    parseResult.InvocationConfiguration.Error.WriteLine($"Warning: {warning}");
                }

                return 0;
            }

            if (result.ErrorCode == ErrorCodes.UserCancelled)
            {
                return 0;
            }

            DisplayError(result, options.Verbose, parseResult.InvocationConfiguration.Error);
            return 1;
        });
    }

    private (Argument<string>, Option<string?>, Option<bool>, Option<EditorType?>, Option<OutputFormat>, Option<bool>)
        AddArgumentsAndOptions()
    {
        var branchArgument = new Argument<string>("branch-name")
        {
            Description = "Name of the branch to check out"
        };

        var remoteOption = new Option<string?>("--remote")
        {
            Description = "Remote name to use (skips interactive prompt when multiple remotes found)"
        };
        var fetchOption = new Option<bool>("--fetch")
        {
            Description = "Fetch from remote before creating worktree",
            DefaultValueFactory = _ => false
        };
        var editorOption = new Option<EditorType?>("--editor", "-e")
        {
            Description = "Editor to launch after creating worktree"
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

        this.Arguments.Add(branchArgument);
        this.Options.Add(remoteOption);
        this.Options.Add(fetchOption);
        this.Options.Add(editorOption);
        this.Options.Add(outputOption);
        this.Options.Add(verboseOption);

        return (branchArgument, remoteOption, fetchOption, editorOption, outputOption, verboseOption);
    }

    private static void DisplaySuccess(WorktreeInfo worktreeInfo, OutputFormat format, TextWriter output)
    {
        if (format == OutputFormat.Json)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(new
            {
                success = true,
                worktree = new
                {
                    path = worktreeInfo.Path,
                    branch = worktreeInfo.Branch,
                    remote = worktreeInfo.Remote,
                    isDetached = worktreeInfo.IsDetached,
                    createdAt = worktreeInfo.CreatedAt
                }
            }, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            output.WriteLine(json);
        }
        else
        {
            output.WriteLine("✓ Worktree checked out successfully");
            output.WriteLine($"  Branch:  {worktreeInfo.Branch}");
            output.WriteLine($"  Path:    {worktreeInfo.Path}");
            if (!string.IsNullOrEmpty(worktreeInfo.Remote))
            {
                output.WriteLine($"  Remote:  {worktreeInfo.Remote}");
            }
        }
    }

    private static void DisplayError(CommandResult<WorktreeInfo> result, bool verbose, TextWriter error)
    {
        error.WriteLine($"✗ {result.ErrorMessage}");

        if (!string.IsNullOrEmpty(result.Solution))
        {
            error.WriteLine($"  Solution:   {result.Solution}");
        }

        if (verbose && !string.IsNullOrEmpty(result.ErrorCode))
        {
            error.WriteLine($"  Error Code: {result.ErrorCode}");
        }
    }
}
