using System.CommandLine;
using System.IO.Abstractions;
using Kuju63.WorkTree.CommandLine.Commands;
using Kuju63.WorkTree.CommandLine.Commands.Worktree;
using Kuju63.WorkTree.CommandLine.Formatters;
using Kuju63.WorkTree.CommandLine.Services.Editor;
using Kuju63.WorkTree.CommandLine.Services.Git;
using Kuju63.WorkTree.CommandLine.Services.Interaction;
using Kuju63.WorkTree.CommandLine.Services.Worktree;
using Kuju63.WorkTree.CommandLine.Utils;

// Setup Dependency Injection
var fileSystem = new FileSystem();
var processRunner = new ProcessRunner();
var pathHelper = new PathHelper(fileSystem);
var gitService = new GitService(processRunner, fileSystem);
var editorService = new EditorService(processRunner);
var worktreeService = new WorktreeService(gitService, pathHelper, editorService);
var tableFormatter = new TableFormatter();
var interactionService = new ConsoleInteractionService();

// Setup root command
var rootCommand = new RootCommand("Git worktree management CLI tool");

// Add commands
var createCommand = new CreateCommand(worktreeService);
var listCommand = new ListCommand(worktreeService, tableFormatter);
var removeCommand = new RemoveCommand(worktreeService);
var checkoutCommand = new CheckoutCommand(worktreeService, interactionService);
rootCommand.Subcommands.Add(createCommand);
rootCommand.Subcommands.Add(listCommand);
rootCommand.Subcommands.Add(removeCommand);
rootCommand.Subcommands.Add(checkoutCommand);

// Parse and execute
ParseResult parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();
