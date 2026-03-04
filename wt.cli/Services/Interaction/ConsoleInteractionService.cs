namespace Kuju63.WorkTree.CommandLine.Services.Interaction;

/// <summary>
/// Provides interactive user selection via standard console input/output.
/// </summary>
public class ConsoleInteractionService : IInteractionService
{
    private const int MaxRetries = 3;

    private readonly Func<string?> _readLine;
    private readonly Action<string> _writeLine;

    /// <summary>
    /// Initializes a new instance of <see cref="ConsoleInteractionService"/> using Console I/O.
    /// </summary>
    public ConsoleInteractionService()
        : this(() => Console.ReadLine(), Console.WriteLine)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ConsoleInteractionService"/> with custom I/O functions.
    /// Used for testing.
    /// </summary>
    /// <param name="readLine">Function to read a line of input.</param>
    /// <param name="writeLine">Action to write a line of output.</param>
    public ConsoleInteractionService(Func<string?> readLine, Action<string> writeLine)
    {
        _readLine = readLine;
        _writeLine = writeLine;
    }

    /// <inheritdoc/>
    public Task<int?> SelectAsync(
        string prompt,
        IReadOnlyList<string> choices,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult<int?>(null);
        }

        _writeLine(prompt);
        for (var i = 0; i < choices.Count; i++)
        {
            _writeLine($"  {i + 1}) {choices[i]}");
        }

        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromResult<int?>(null);
            }

            _writeLine($"Enter number (1-{choices.Count}), or empty/q to cancel:");
            var input = _readLine()?.Trim() ?? "";

            if (string.IsNullOrEmpty(input) || input.Equals("q", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<int?>(null);
            }

            if (int.TryParse(input, out var number) && number >= 1 && number <= choices.Count)
            {
                return Task.FromResult<int?>(number - 1);
            }

            _writeLine($"Invalid input. Please enter a number between 1 and {choices.Count}.");
        }

        _writeLine("Too many invalid inputs. Cancelling.");
        return Task.FromResult<int?>(null);
    }
}
