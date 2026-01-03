using System;
using AwesomeAssertions;
using Xunit;

namespace Ponyglot.Tests;

public class TranslationFormTest
{
    [Fact]
    public void Text_ValidMessage_CreatesAValidInstance()
    {
        // Arrange

        // Act
        var sut = TranslationForm.Text("my-message");

        // Assert
        sut.Should().NotBeNull();
        sut.Message.Should().Be("my-message");
        sut.IsCompositeFormat.Should().BeFalse();
    }

    [Fact]
    public void Text_NullMessage_Throws()
    {
        // Arrange

        // Act
        var action = () => TranslationForm.Text(null!);

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName("message");
    }

    [Fact]
    public void CompositeFormat_ValidMessage_CreatesAValidInstance()
    {
        // Arrange

        // Act
        var sut = TranslationForm.CompositeFormat("my-message");

        // Assert
        sut.Should().NotBeNull();
        sut.Message.Should().Be("my-message");
        sut.IsCompositeFormat.Should().BeTrue();
    }

    [Fact]
    public void CompositeFormat_NullMessage_Throws()
    {
        // Arrange

        // Act
        var action = () => TranslationForm.CompositeFormat(null!);

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName("message");
    }

    [Fact]
    public void ToString_Text_ReturnsMessage()
    {
        // Arrange
        var sut = TranslationForm.Text("my-message");

        // Act
        var result = sut.ToString();

        // Assert
        result.Should().Be("my-message");
    }

    [Fact]
    public void ToString_CompositeFormat_ReturnsMessage()
    {
        // Arrange
        var sut = TranslationForm.CompositeFormat("my-message");

        // Act
        var result = sut.ToString();

        // Assert
        result.Should().Be("my-message");
    }
}