using Kuju63.WorkTree.CommandLine.Models;
using Shouldly;

namespace Kuju63.WorkTree.Tests.Models;

public class UnitTests
{
    [Fact]
    public void Value_ReturnsSameInstanceEachTime()
    {
        // Act
        var first = Unit.Value;
        var second = Unit.Value;

        // Assert
        first.ShouldBeSameAs(second);
    }
}
