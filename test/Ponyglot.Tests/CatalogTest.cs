using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.Extensions;
using Ponyglot.Tests._TestUtils;
using Xunit;

namespace Ponyglot.Tests;

public class CatalogTest
{
    private readonly string _uid;
    private readonly string _catalogName;
    private readonly CultureInfo _culture;
    private readonly IPluralRule _pluralRule;

    public CatalogTest()
    {
        _uid = "my-catalog-uid";
        _catalogName = "my-catalog";
        _culture = new CultureInfo("qps-Ploc");
        _pluralRule = Substitute.For<IPluralRule>();
        _pluralRule.PluralCount.Returns(2);
        _pluralRule.GetPluralForm(default).ReturnsForAnyArgs(0);
    }

    [Fact]
    public void Constructor_ValidArguments_BuildsTheIndex()
    {
        // Arrange
        _pluralRule.PluralCount.Returns(2);
        var entries = new[]
        {
            MessageEntry.NonPlural("", "my-message-a", TranslationForm.CompositeFormat("my-translation-a")),
            MessageEntry.NonPlural("my-context-1", "my-message-a", TranslationForm.Text("my-translation-a-ctx")),
            MessageEntry.Plural("", "my-message-b", [TranslationForm.Text("my-translation-b-0"), TranslationForm.CompositeFormat("my-translation-b-1")]),
            MessageEntry.Plural("my-context-2", "my-message-c", [TranslationForm.Text("my-translation-c-0"), TranslationForm.Text("my-translation-c-1")]),
        };

        // Act
        var sut = CreateSut(entries: entries);

        // Assert
        var index = sut.GetTranslationsIndex();
        index.Should().BeEquivalentTo(new Dictionary<string, Dictionary<string, IReadOnlyList<TranslationForm>>>
        {
            ["my-message-a"] = new()
            {
                [""] = [TranslationForm.CompositeFormat("my-translation-a")],
                ["my-context-1"] = [TranslationForm.Text("my-translation-a-ctx")],
            },
            ["my-message-b"] = new()
            {
                [""] = [TranslationForm.Text("my-translation-b-0"), TranslationForm.CompositeFormat("my-translation-b-1")],
            },
            ["my-message-c"] = new()
            {
                ["my-context-2"] = [TranslationForm.Text("my-translation-c-0"), TranslationForm.Text("my-translation-c-1")],
            },
        });
    }

    [Fact]
    public void Constructor_ValidArguments_SetsTheProperties()
    {
        // Arrange

        // Act
        var sut = CreateSut();

        // Assert
        sut.Uid.Should().BeSameAs(_uid);
        sut.CatalogName.Should().Be(_catalogName);
        sut.Culture.Should().BeSameAs(_culture);
    }

    [Fact]
    public void Constructor_EmptyEntries_DoesNotThrow()
    {
        // Arrange

        // Act
        var action = () => CreateSut(entries: []);

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void Constructor_Always_ValidatesThePluralRule()
    {
        // Arrange
        var testedNValues = new List<long>();
        _pluralRule.Configure().GetPluralForm(default).ReturnsForAnyArgs(ci =>
        {
            testedNValues.Add(ci.Arg<long>());
            return (int)(ci.Arg<long>() % _pluralRule.PluralCount);
        });

        // Act
        _ = CreateSut();

        // Assert
        _pluralRule.ReceivedWithAnyArgs().GetPluralForm(default);
        testedNValues.Should().HaveCountGreaterThan(10);
        testedNValues.Should().Contain([0, 1, 2], because: "simple cases should be tested");
        testedNValues.Should().Contain([10, 11, 100, 101], because: "modulo 10/100 rules should be tested");
        testedNValues.Should().Contain([11, 14], because: "Slavic 11–14 exceptions should be tested");
        testedNValues.Should().Contain([3, 4, 5], because: "few and many should be tested");
        testedNValues.Should().Contain(long.MaxValue, because: "very large value greater than int32 max value should be tested");
    }

    [Theory]
    [InlineData("uid")]
    [InlineData("catalogName")]
    [InlineData("culture")]
    [InlineData("pluralRule")]
    [InlineData("entries")]
    public void Constructor_ArgumentIsNull_Throws(string parameterName)
    {
        // Arrange

        // Act
        var action = () => new Catalog(
            parameterName == "uid" ? null! : _uid,
            parameterName == "catalogName" ? null! : _catalogName,
            parameterName == "culture" ? null! : _culture,
            parameterName == "pluralRule" ? null! : _pluralRule,
            parameterName == "entries" ? null! : new[] { MessageEntry.NonPlural("", "my-message", TranslationForm.Text("my-translation")) });

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName(parameterName);
    }

    [Theory]
    [InlineData(2, -1)]
    [InlineData(2, 2)]
    [InlineData(5, 10)]
    public void Constructor_PluralRuleProducesInvalidValue_Throws(int pluralCount, int value)
    {
        // Arrange
        _pluralRule.PluralCount.Returns(pluralCount);
        _pluralRule.GetPluralForm(default).ReturnsForAnyArgs(value);

        // Act
        var action = () => CreateSut();

        // Assert
        action.Should().ThrowExactly<ArgumentException>()
            .WithParameterName("pluralRule")
            .WithMessage($"Plural rule {_pluralRule} returned {value} for count 0 (expected >= 0 and < {pluralCount}).*");
    }

    [Fact]
    public void Constructor_DuplicateEntryForMessageAndContext_Throws()
    {
        // Arrange
        var entries = new[]
        {
            MessageEntry.NonPlural("my-context", "my-message", TranslationForm.Text("my-translation-1")),
            MessageEntry.NonPlural("my-context", "my-message", TranslationForm.Text("my-translation-2")),
        };

        // Act
        var action = () => CreateSut(entries);

        // Assert
        action.Should().ThrowExactly<ArgumentException>()
            .WithParameterName("entries")
            .WithMessage("Duplicate catalog entry for message 'my-message' with context 'my-context' in catalog 'my-catalog-uid'.*");
    }

    [Fact]
    public void Constructor_EntryIsNull_Throws()
    {
        // Arrange
        var entries = new[]
        {
            MessageEntry.NonPlural("my-context", "my-message-1", TranslationForm.Text("my-translation-1")),
            MessageEntry.NonPlural("my-context", "my-message-2", TranslationForm.Text("my-translation-2")),
            MessageEntry.NonPlural("my-context", "my-message-3", TranslationForm.Text("my-translation-3")),
            null!,
            MessageEntry.NonPlural("my-context", "my-message-4", TranslationForm.Text("my-translation-4")),
        };

        // Act
        var action = () => CreateSut(entries);

        // Assert
        action.Should().ThrowExactly<ArgumentException>()
            .WithParameterName("entries")
            .WithMessage("The catalog entry at index 3 is null.*");
    }

    [Fact]
    public void Constructor_EntryHasNoTranslations_Throws()
    {
        // Arrange
        var entry = MessageEntry.New("my-message", "my-context", isPlural: false, translations: []);

        // Act
        var action = () => CreateSut([entry]);

        // Assert
        action.Should().ThrowExactly<ArgumentException>()
            .WithParameterName("entries")
            .WithMessage("No translations defined for message 'my-message' with context 'my-context' in catalog 'my-catalog-uid'.*");
    }

    [Fact]
    public void Constructor_PluralEntryHasInvalidPluralCount_Throws()
    {
        // Arrange
        _pluralRule.PluralCount.Returns(3);
        var entry = MessageEntry.Plural("my-context", "my-message", [TranslationForm.Text("my-translation-0"), TranslationForm.Text("my-translation-1")]);

        // Act
        var action = () => CreateSut([entry]);

        // Assert
        action.Should().ThrowExactly<ArgumentException>()
            .WithParameterName("entries")
            .WithMessage("Plurals count (2) mismatch for message 'my-message' with context 'my-context' in catalog 'my-catalog-uid' (expected 3).*");
    }

    [Fact]
    public void Constructor_CompositeFormatFormHasInvalidFormatString_Throws()
    {
        // Arrange
        var entry = MessageEntry.NonPlural("my-context", "my-message", TranslationForm.CompositeFormat("{0 BAD"));

        // Act
        var action = () => CreateSut([entry]);

        // Assert
        string formatError;
        try
        {
            _ = CompositeFormat.Parse(entry.Translations[0].Message);
            throw new Exception("Expected exception not thrown");
        }
        catch (FormatException exception)
        {
            formatError = exception.Message;
        }

        action.Should().ThrowExactly<ArgumentException>()
            .WithParameterName("entries")
            .WithMessage($"Invalid translation format string ({{0 BAD) at index 0 for message 'my-message' with context 'my-context' in catalog 'my-catalog-uid': {formatError}*");
    }

    [Fact]
    public void Constructor_TextFormHasInvalidFormatString_DoesNotThrow()
    {
        // Arrange
        var entry = MessageEntry.NonPlural("my-context", "my-message", TranslationForm.Text("{0 BAD"));

        // Act
        var action = () => CreateSut([entry]);

        // Assert
        action.Should().NotThrow();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void TryGet_TranslationFoundAndCountSpecified_EvaluatesThePluralRule(long count)
    {
        // Arrange
        _pluralRule.PluralCount.Returns(1);

        var sut = CreateSut([MessageEntry.Plural("my-context", "my-message", [TranslationForm.Text("my-translation")])]);

        _pluralRule.ClearReceivedCalls();

        // Act
        sut.TryGet("my-context", count, "my-message", out _);

        // Assert
        _pluralRule.Received(1).GetPluralForm(count);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(0, false)]
    public void TryGet_TranslationFoundAndCountSpecified_ReturnsTheTranslation(int pluralIndex, bool isPluralTranslation)
    {
        // Arrange
        _pluralRule.PluralCount.Returns(3);
        _pluralRule.GetPluralForm(default).ReturnsForAnyArgs(pluralIndex);

        var forms = new[]
        {
            TranslationForm.Text("my-translation-a"),
            TranslationForm.Text("my-translation-b"),
            TranslationForm.Text("my-translation-c"),
        };

        var sut = CreateSut([
            isPluralTranslation switch
            {
                true => MessageEntry.Plural("my-context", "my-message", forms),
                false => MessageEntry.NonPlural("my-context", "my-message", forms[0]),
            },
            MessageEntry.NonPlural("other-context", "my-message", TranslationForm.Text("x-translation")),
            MessageEntry.NonPlural("my-context", "other-message", TranslationForm.Text("y-translation")),
        ]);

        // Act
        var result = sut.TryGet("my-context", pluralIndex, "my-message", out var translation);

        // Assert
        result.Should().BeTrue();
        translation.Should().BeSameAs(forms[pluralIndex]);
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(2, true)]
    [InlineData(-1, false)]
    [InlineData(2, false)]
    public void TryGet_TranslationFoundButPluralRuleIndexOutOfRange_ReturnsNull(int pluralIndex, bool isPluralTranslation)
    {
        // Arrange
        _pluralRule.PluralCount.Returns(1);

        var sut = CreateSut([
            isPluralTranslation switch
            {
                true => MessageEntry.Plural("my-context", "my-message", [TranslationForm.Text("my-translation")]),
                false => MessageEntry.NonPlural("my-context", "my-message", TranslationForm.Text("my-translation")),
            },
        ]);

        _pluralRule.GetPluralForm(default).ReturnsForAnyArgs(pluralIndex); // Only return invalid values after constructor validation

        // Act
        var result = sut.TryGet("my-context", pluralIndex, "my-message", out var translation);

        // Assert
        result.Should().BeFalse();
        translation.Should().BeNull();
    }

    [Fact]
    public void TryGet_TranslationFoundAndCountNull_DoesNotEvaluateThePluralRule()
    {
        // Arrange
        _pluralRule.PluralCount.Returns(1);
        var sut = CreateSut([MessageEntry.Plural("my-context", "my-message", [TranslationForm.Text("my-translation")])]);

        _pluralRule.ClearReceivedCalls();

        // Act
        sut.TryGet("my-context", null, "my-message", out _);

        // Assert
        _pluralRule.DidNotReceiveWithAnyArgs().GetPluralForm(default);
    }

    [Theory]
    [CombinatorialData]
    public void TryGet_TranslationFoundAndCountNull_ReturnsTheTranslation(bool isPluralTranslation)
    {
        // Arrange
        _pluralRule.PluralCount.Returns(3);

        var forms = new[]
        {
            TranslationForm.Text("my-translation-a"),
            TranslationForm.Text("my-translation-b"),
            TranslationForm.Text("my-translation-c"),
        };

        var sut = CreateSut([
            isPluralTranslation switch
            {
                true => MessageEntry.Plural("my-context", "my-message", forms),
                false => MessageEntry.NonPlural("my-context", "my-message", forms[0]),
            },
            MessageEntry.NonPlural("other-context", "my-message", TranslationForm.Text("x-translation")),
            MessageEntry.NonPlural("my-context", "other-message", TranslationForm.Text("y-translation")),
        ]);

        // Act
        var result = sut.TryGet("my-context", null, "my-message", out var translation);

        // Assert
        result.Should().BeTrue();
        translation.Should().BeSameAs(forms[0]);
    }

    [Theory]
    [CombinatorialData]
    public void TryGet_TranslationNotFound_DoesNotEvaluateThePluralRule(bool countSpecified, bool contextDiffersOnlyByCase)
    {
        // Arrange
        _pluralRule.PluralCount.Returns(1);

        var entryContext = contextDiffersOnlyByCase ? "MY-CONTEXT" : "other-context";
        var sut = CreateSut([MessageEntry.Plural(entryContext, "other-message", [TranslationForm.Text("my-translation")])]);

        _pluralRule.ClearReceivedCalls();

        long? count = countSpecified ? 0 : null;

        // Act
        sut.TryGet("my-context", count, "my-message", out _);

        // Assert
        _pluralRule.DidNotReceiveWithAnyArgs().GetPluralForm(default);
    }

    [Theory]
    [CombinatorialData]
    public void TryGet_TranslationNotFound_ReturnsNull(bool countSpecified, bool contextDiffersOnlyByCase)
    {
        // Arrange
        _pluralRule.PluralCount.Returns(1);

        var entryContext = contextDiffersOnlyByCase ? "MY-CONTEXT" : "other-context";
        var sut = CreateSut([MessageEntry.Plural(entryContext, "other-message", [TranslationForm.Text("my-translation")])]);

        long? count = countSpecified ? 0 : null;

        // Act
        var result = sut.TryGet("my-context", count, "my-message", out var translation);

        // Assert
        result.Should().BeFalse();
        translation.Should().BeNull();
    }

    [Fact]
    public void TryGet_PluralRuleThrows_Throws()
    {
        // Arrange
        var error = new Exception("💥Kaboom💥");
        _pluralRule.PluralCount.Returns(1);

        var sut = CreateSut([MessageEntry.NonPlural("my-context", "my-message", TranslationForm.Text("my-translation"))]);

        _pluralRule.GetPluralForm(default).Throws(error); // Throw after constructor validation

        // Act
        var action = () => sut.TryGet("my-context", 0, "my-message", out _);

        // Assert
        action.Should().ThrowExactly<Exception>().Which.Should().BeSameAs(error);
    }

    [Theory]
    [InlineData("context")]
    [InlineData("messageId")]
    public void TryGet_ArgumentIsNull_Throws(string parameterName)
    {
        // Arrange
        var sut = CreateSut([MessageEntry.NonPlural("my-context", "my-message", TranslationForm.Text("my-translation"))]);

        // Act
        var action = () => sut.TryGet(
            parameterName == "context" ? null! : "my-context",
            null,
            parameterName == "messageId" ? null! : "my-message",
            out _);

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName(parameterName);
    }

    [Fact]
    public void ToString_Created_ReturnsTheUri()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.ToString();

        // Assert
        result.Should().Be(_uid);
    }

    #region Helpers

    private Catalog CreateSut(IEnumerable<MessageEntry>? entries = null)
    {
        entries ??= [MessageEntry.NonPlural("my-context", "my-message", TranslationForm.Text("my-translation"))];
        return new Catalog(_uid, _catalogName, _culture, _pluralRule, entries);
    }

    #endregion
}