using Kuju63.WorkTree.CommandLine.Models;
using Kuju63.WorkTree.CommandLine.Utils;

namespace Kuju63.WorkTree.CommandLine.Services.Editor;

/// <summary>
/// Provides functionality for launching and managing code editors.
/// </summary>
public class EditorService : IEditorService
{
    private readonly IProcessRunner _processRunner;

    public EditorService(IProcessRunner processRunner)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
    }

    public async Task<CommandResult<string>> LaunchEditorAsync(
        string path,
        EditorType editorType,
        CancellationToken cancellationToken)
    {
        var config = ResolveEditorCommand(editorType);

        // Check if editor is available
        var whichCommand = OperatingSystem.IsWindows() ? "where" : "which";
        var checkResult = await _processRunner.RunAsync(
            whichCommand,
            config.Command,
            null,
            cancellationToken);

        if (checkResult.ExitCode != 0)
        {
            return CommandResult<string>.Failure(
                ErrorCodes.EditorNotFound,
                $"Editor command '{config.Command}' not found in PATH",
                $"Install {editorType} or add it to your PATH environment variable");
        }

        // Launch the editor
        var launchResult = await _processRunner.RunAsync(
            config.Command,
            $"\"{path}\"",
            null,
            cancellationToken);

        if (launchResult.ExitCode != 0)
        {
            return CommandResult<string>.Failure(
                ErrorCodes.EditorNotFound,
                $"Failed to launch {editorType}",
                "Check editor installation and permissions");
        }

        return CommandResult<string>.Success($"Launched {editorType} with path: {path}");
    }

    /// <summary>
    /// Launches the specified editor with the given path. This overload does not accept a cancellation token.
    /// </summary>
    /// <param name="path">The path to open in the editor.</param>
    /// <param name="editorType">The type of editor to launch.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing the result of the launch operation.</returns>
    public Task<CommandResult<string>> LaunchEditorAsync(string path, EditorType editorType)
        => LaunchEditorAsync(path, editorType, CancellationToken.None);

    public EditorConfig ResolveEditorCommand(EditorType editorType)
    {
        if (EditorPresets.KnownEditors.TryGetValue(editorType, out var config))
        {
            return config;
        }

        throw new ArgumentException($"Unknown editor type: {editorType}", nameof(editorType));
    }
}
