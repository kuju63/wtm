namespace Kuju63.WorkTree.CommandLine.Services.Interaction;

/// <summary>
/// Defines methods for interactive user selection, abstracted for testability.
/// </summary>
public interface IInteractionService
{
    /// <summary>
    /// Presents a list of choices to the user and returns the selected index.
    /// Returns null if the user cancels the selection.
    /// </summary>
    /// <param name="prompt">The prompt message to display.</param>
    /// <param name="choices">The list of choices to present.</param>
    /// <returns>The 0-based index of the selected choice, or null if cancelled.</returns>
    Task<int?> SelectAsync(string prompt, IReadOnlyList<string> choices)
        => SelectAsync(prompt, choices, CancellationToken.None);

    /// <summary>
    /// Presents a list of choices to the user and returns the selected index.
    /// Returns null if the user cancels the selection.
    /// </summary>
    /// <param name="prompt">The prompt message to display.</param>
    /// <param name="choices">The list of choices to present.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The 0-based index of the selected choice, or null if cancelled.</returns>
    Task<int?> SelectAsync(string prompt, IReadOnlyList<string> choices, CancellationToken cancellationToken);
}
