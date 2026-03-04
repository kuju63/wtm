namespace Kuju63.WorkTree.CommandLine.Models;

/// <summary>
/// Represents a unit type (void) for generic command results.
/// </summary>
public sealed class Unit
{
    /// <summary>
    /// Gets the singleton instance of Unit.
    /// </summary>
    public static Unit Value => new();

    private Unit()
    {
    }
}
