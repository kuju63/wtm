using Kuju63.WorkTree.CommandLine.Services.Interaction;
using Shouldly;

namespace Kuju63.WorkTree.Tests.Services.Interaction;

public class ConsoleInteractionServiceTests
{
    private readonly List<string> _choices = new() { "origin", "upstream", "fork" };

    private ConsoleInteractionService CreateService(params string[] inputs)
    {
        var inputQueue = new Queue<string>(inputs);
        return new ConsoleInteractionService(
            () => inputQueue.Count > 0 ? inputQueue.Dequeue() : null,
            _ => { }
        );
    }

    [Fact]
    public async Task SelectAsync_ValidInput_1_ReturnsIndex0()
    {
        // Arrange
        var service = CreateService("1");

        // Act
        var result = await service.SelectAsync("Choose:", _choices);

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    public async Task SelectAsync_ValidInput_2_ReturnsIndex1()
    {
        // Arrange
        var service = CreateService("2");

        // Act
        var result = await service.SelectAsync("Choose:", _choices);

        // Assert
        result.ShouldBe(1);
    }

    [Fact]
    public async Task SelectAsync_ValidInput_N_ReturnsLastIndex()
    {
        // Arrange
        var service = CreateService("3");

        // Act
        var result = await service.SelectAsync("Choose:", _choices);

        // Assert
        result.ShouldBe(2);
    }

    [Fact]
    public async Task SelectAsync_EmptyInput_ReturnsNull()
    {
        // Arrange
        var service = CreateService("");

        // Act
        var result = await service.SelectAsync("Choose:", _choices);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task SelectAsync_QInput_ReturnsNull()
    {
        // Arrange
        var service = CreateService("q");

        // Act
        var result = await service.SelectAsync("Choose:", _choices);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task SelectAsync_QUppercase_ReturnsNull()
    {
        // Arrange
        var service = CreateService("Q");

        // Act
        var result = await service.SelectAsync("Choose:", _choices);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task SelectAsync_ThreeInvalidInputs_ReturnsNull()
    {
        // Arrange
        var service = CreateService("abc", "999", "xyz");

        // Act
        var result = await service.SelectAsync("Choose:", _choices);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task SelectAsync_InvalidThenValid_ReturnsValidIndex()
    {
        // Arrange
        var service = CreateService("abc", "2");

        // Act
        var result = await service.SelectAsync("Choose:", _choices);

        // Assert
        result.ShouldBe(1);
    }

    [Fact]
    public async Task SelectAsync_CancellationToken_ReturnsNull()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var service = CreateService("1");

        // Act
        var result = await service.SelectAsync("Choose:", _choices, cts.Token);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task SelectAsync_ZeroInput_IsInvalid_ReturnsNullAfterRetries()
    {
        // Arrange
        var service = CreateService("0", "0", "0");

        // Act
        var result = await service.SelectAsync("Choose:", _choices);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task SelectAsync_OutOfRangeInput_IsInvalid_ReturnsNullAfterRetries()
    {
        // Arrange
        var service = CreateService("4", "5", "6");

        // Act
        var result = await service.SelectAsync("Choose:", _choices);

        // Assert
        result.ShouldBeNull();
    }
}
