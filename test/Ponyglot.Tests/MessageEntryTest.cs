using System;
using AwesomeAssertions;
using Xunit;

namespace Ponyglot.Tests;

public class MessageEntryTest
{
    [Fact]
    public void NonPlural_ValidArguments_CreatesValidInstance()
    {
        // Arrange
        var translation = TranslationForm.Text("my-translation");

        // Act
        var sut = MessageEntry.NonPlural("my-context", "my-message-id", translation);

        // Assert
        sut.MessageId.Should().Be("my-message-id");
        sut.Context.Should().Be("my-context");
        sut.Translations.Should().ContainSingle().Which.Should().BeSameAs(translation);
        sut.IsPlural.Should().BeFalse();
    }

    [Theory]
    [InlineData("messageId")]
    [InlineData("context")]
    [InlineData("translation")]
    public void NonPlural_ArgumentIsNull_Throws(string parameterName)
    {
        // Arrange

        // Act
        var action = () => MessageEntry.NonPlural(
            parameterName == "context" ? null! : "my-context",
            parameterName == "messageId" ? null! : "my-message-id",
            parameterName == "translation" ? null! : TranslationForm.Text("my-translation"));

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName(parameterName);
    }

    [Fact]
    public void Plural_ValidArguments_CreatesValidInstance()
    {
        // Arrange
        var translations = new[] { TranslationForm.Text("my-translation-1"), TranslationForm.Text("my-translation-2") };

        // Act
        var sut = MessageEntry.Plural("my-context", "my-message-id", translations);

        // Assert
        sut.MessageId.Should().Be("my-message-id");
        sut.Context.Should().Be("my-context");
        sut.Translations.Should().BeSameAs(translations);
        sut.IsPlural.Should().BeTrue();
    }

    [Theory]
    [InlineData("messageId")]
    [InlineData("context")]
    [InlineData("translations")]
    public void Plural_ArgumentIsNull_Throws(string parameterName)
    {
        // Arrange

        // Act
        var action = () => MessageEntry.Plural(
            parameterName == "context" ? null! : "my-context",
            parameterName == "messageId" ? null! : "my-message-id",
            parameterName == "translations" ? null! : [TranslationForm.Text("my-translation")]);

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName(parameterName);
    }

    [Fact]
    public void Plural_TranslationsIsEmpty_Throws()
    {
        // Arrange

        // Act
        var action = () => MessageEntry.Plural("my-context", "my-message-id", []);

        // Assert
        action.Should().ThrowExactly<ArgumentException>().WithParameterName("translations").WithMessage("*translations*empty*");
    }

    [Fact]
    public void Plural_TranslationsContainsANullElement_Throws()
    {
        // Arrange

        // Act
        var action = () => MessageEntry.Plural("my-context", "my-message-id", [TranslationForm.Text("x"), null!, TranslationForm.Text("y")]);

        // Assert
        action.Should().ThrowExactly<ArgumentException>().WithParameterName("translations").WithMessage("*contain*null*");
    }
}