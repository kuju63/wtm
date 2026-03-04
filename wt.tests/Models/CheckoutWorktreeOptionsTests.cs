using Kuju63.WorkTree.CommandLine.Models;
using Shouldly;

namespace Kuju63.WorkTree.Tests.Models;

public class CheckoutWorktreeOptionsTests
{
    [Fact]
    public void Validate_WithValidBranchName_ShouldReturnValid()
    {
        // Arrange
        var options = new CheckoutWorktreeOptions { BranchName = "feature/my-branch" };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithNullBranchName_ShouldReturnInvalid()
    {
        // Arrange
        var options = new CheckoutWorktreeOptions { BranchName = null! };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Validate_WithEmptyBranchName_ShouldReturnInvalid()
    {
        // Arrange
        var options = new CheckoutWorktreeOptions { BranchName = "" };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_WithWhitespaceBranchName_ShouldReturnInvalid()
    {
        // Arrange
        var options = new CheckoutWorktreeOptions { BranchName = "   " };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_WithRemoteContainingSpaces_ShouldReturnInvalid()
    {
        // Arrange
        var options = new CheckoutWorktreeOptions
        {
            BranchName = "feature/valid",
            Remote = "invalid remote"
        };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void Validate_WithValidRemoteName_ShouldReturnValid()
    {
        // Arrange
        var options = new CheckoutWorktreeOptions
        {
            BranchName = "feature/valid",
            Remote = "origin"
        };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithNullRemote_ShouldReturnValid()
    {
        // Arrange
        var options = new CheckoutWorktreeOptions
        {
            BranchName = "feature/valid",
            Remote = null
        };

        // Act
        var result = options.Validate();

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void CheckoutWorktreeOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new CheckoutWorktreeOptions { BranchName = "main" };

        // Assert
        options.Remote.ShouldBeNull();
        options.Fetch.ShouldBeFalse();
        options.EditorType.ShouldBeNull();
        options.OutputFormat.ShouldBe(OutputFormat.Human);
        options.Verbose.ShouldBeFalse();
    }
}
