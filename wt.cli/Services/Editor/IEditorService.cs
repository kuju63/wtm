using Kuju63.WorkTree.CommandLine.Models;

namespace Kuju63.WorkTree.CommandLine.Services.Editor;

/// <summary>
/// Defines methods for launching and managing code editors.
/// </summary>
public interface IEditorService
{
    /// <summary>
    /// Launches the specified editor with the given path asynchronously.
    /// This overload does not accept a cancellation token.
    /// </summary>
    /// <param name="path">The path to open in the editor.</param>
    /// <param name="editorType">The type of editor to launch.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing the result of the launch operation.</returns>
    Task<CommandResult<string>> LaunchEditorAsync(string path, EditorType editorType);

    /// <summary>
    /// Launches the specified editor with the given path asynchronously.
    /// </summary>
    /// <param name="path">The path to open in the editor.</param>
    /// <param name="editorType">The type of editor to launch.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="CommandResult{T}"/> containing the result of the launch operation.</returns>
    Task<CommandResult<string>> LaunchEditorAsync(string path, EditorType editorType, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves the editor command configuration for the specified editor type.
    /// </summary>
    /// <param name="editorType">The type of editor to resolve.</param>
    /// <returns>The <see cref="EditorConfig"/> for the specified editor type.</returns>
    EditorConfig ResolveEditorCommand(EditorType editorType);
}
