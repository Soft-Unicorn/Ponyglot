using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Ponyglot.Tests._TestUtils;
using Xunit;

namespace Ponyglot.Tests;

public class TranslatorTest
{
    private readonly ITranslationStore _translationStore;
    private readonly ICultureSource _cultureSource;

    public TranslatorTest()
    {
        _translationStore = Substitute.For<ITranslationStore>();

        _cultureSource = Substitute.For<ICultureSource>();
        _cultureSource.Culture.Returns(new CultureInfo("qps-Ploc"));
        _cultureSource.Culture.NumberFormat.NumberGroupSeparator = "_"; // No standard culture uses this !
    }

    [Theory]
    [InlineData("translationStore")]
    [InlineData("cultureSource")]
    [InlineData("domain")]
    public void Constructor_ArgumentIsNull_Throws(string parameterName)
    {
        // Arrange

        // Act
        var action = () => new Translator(
            parameterName == "translationStore" ? null! : _translationStore,
            parameterName == "cultureSource" ? null! : _cultureSource,
            parameterName == "domain" ? null! : "my-domain",
            "");

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName(parameterName);
    }

    [Fact]
    public void Domain_Created_ReturnsTheConstructorValue()
    {
        // Arrange
        var sut = CreateSut(domain: "my-domain");

        // Act
        var domain = sut.Domain;

        // Assert
        domain.Should().Be("my-domain");
    }

    [Theory]
    [InlineData("")]
    [InlineData("my-context")]
    public void Context_Created_ReturnsTheConstructorValue(string value)
    {
        // Arrange
        var sut = CreateSut(context: value);

        // Act
        var domain = sut.Context;

        // Assert
        domain.Should().Be(value);
    }

    [Theory]
    [CombinatorialData]
    public void ForCulture_NoState_ReturnsAnInstanceLockedOnTheSpecifiedCulture(
        [CombinatorialValues("", "my-domain")] string context,
        [CombinatorialValues("ja-JP", "en")] string cultureName)
    {
        // Arrange
        var sut = CreateSut(domain: "my-domain", context: context);

        var culture = new CultureInfo(cultureName);

        // Act
        var result = sut.ForCulture(culture);

        // Assert
        result.Should().BeOfType<Translator>().Which.Should().Satisfy<Translator>(o =>
        {
            o.Context.Should().Be(context);
            o.Domain.Should().Be("my-domain");
            o.GetTranslationStore().Should().BeSameAs(_translationStore);
            o.GetCultureSource().Culture.Should().BeSameAs(culture).And.NotBeSameAs(_cultureSource.Culture);
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("my-context")]
    public void T_Always_QueryTheStoreForTheTranslation(string context)
    {
        // Arrange
        var sut = CreateSut(domain: "my-domain", context: context);

        // Act
        sut.T("my-message");

        // Assert
        _translationStore.ReceivedWithAnyArgs(1).TryGet(default!, default!, default!, default!, out _);
        _translationStore.Received(1).TryGet("my-domain", _cultureSource.Culture, context, "my-message", out _);
    }

    [Theory]
    [InlineData("Parameter not set")]
    [InlineData("One arg")]
    [InlineData("Empty arg array")]
    [InlineData("Null arg array")]
    [SuppressMessage("ReSharper", "RedundantExplicitParamsArrayCreation", Justification = "For testing completeness")]
    public void T_MessageFoundAndIsNotAFormatString_ReturnsMessage(string argsCase)
    {
        // Arrange
        var sut = CreateSut();

        _translationStore.TryGet(default!, default!, default!, default!, out _).ReturnsForAnyArgs(ci =>
        {
            ci[4] = "my-translated-message";
            return true;
        });

        // Act
        var result = argsCase switch
        {
            "Parameter not set" => sut.T("my-message"),
            "One arg" => sut.T("my-message", 12345),
            "Empty arg array" => sut.T("my-message", []),
            "Null arg array" => sut.T("my-message", null!),
            _ => throw new NotSupportedException($"Unsupported test case: {argsCase}"),
        };

        // Assert
        result.Should().Be("my-translated-message");
    }

    [Theory]
    [CombinatorialData]
    public void T_MessageFoundAndIsAFormatString_ReturnsFormattedMessage(bool originalMessageWasAFormatString)
    {
        // Arrange
        var sut = CreateSut();

        _translationStore.TryGet(default!, default!, default!, default!, out _).ReturnsForAnyArgs(ci =>
        {
            ci[4] = "my-translated-message-{0:N0}";
            return true;
        });

        // Act
        var result = sut.T(originalMessageWasAFormatString ? "my-message-{0}" : "my-message", 12345);

        // Assert
        var expected = string.Format(_cultureSource.Culture, "my-translated-message-{0:N0}", 12345);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Parameter not set")]
    [InlineData("One arg")]
    [InlineData("Empty arg array")]
    [InlineData("Null arg array")]
    [SuppressMessage("ReSharper", "RedundantExplicitParamsArrayCreation", Justification = "For testing completeness")]
    public void T_MessageNotFoundAndIsNotAFormatString_ReturnsMessage(string argsCase)
    {
        // Arrange
        var sut = CreateSut();

        _translationStore.TryGet(default!, default!, default!, default!, out _).ReturnsForAnyArgs(ci =>
        {
            ci[4] = null;
            return false;
        });

        // Act
        var result = argsCase switch
        {
            "Parameter not set" => sut.T("my-message"),
            "One arg" => sut.T("my-message", 12345),
            "Empty arg array" => sut.T("my-message", []),
            "Null arg array" => sut.T("my-message", null!),
            _ => throw new NotSupportedException($"Unsupported test case: {argsCase}"),
        };

        // Assert
        result.Should().Be("my-message");
    }

    [Fact]
    public void T_MessageNotFoundAndIsAFormatString_ReturnsFormattedMessage()
    {
        // Arrange
        var sut = CreateSut();

        _translationStore.TryGet(default!, default!, default!, default!, out _).ReturnsForAnyArgs(ci =>
        {
            ci[4] = null;
            return false;
        });

        // Act
        var result = sut.T("my-message-{0:N0}", 12345);

        // Assert
        var expected = string.Format(_cultureSource.Culture, "my-message-{0:N0}", 12345);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("my-message", null)]
    [InlineData("my-message", "my-translated-message")]
    [InlineData("my-message-{0}-{1}", null)]
    [InlineData("my-message-{0}-{1}", "my-translated-message-{0}-{1}")]
    public void T_MessageHasAndAdditionalArguments_ReturnsFormattedMessage(string messageId, string? translation)
    {
        // Arrange
        var sut = CreateSut();
        _translationStore.TryGet(default!, default!, default!, default!, out _).ReturnsForAnyArgs(ci =>
        {
            ci[4] = translation;
            return translation is not null;
        });

        // Act
        var result = sut.T(messageId, 12345, "hello", "world");

        // Assert
        var expected = string.Format(_cultureSource.Culture, translation ?? messageId, 12345, "hello");
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("my-message-{0}-{1}-{2}", null)]
    [InlineData("my-message-{0}-{1}-{2}", "my-translated-message-{0}-{1}-{2}")]
    public void T_MessageHasAMissingArgument_Throws(string messageId, string? translation)
    {
        // Arrange
        var sut = CreateSut();
        _translationStore.TryGet(default!, default!, default!, default!, out _).ReturnsForAnyArgs(ci =>
        {
            ci[4] = translation;
            return translation is not null;
        });

        // Act
        var action = () => sut.T(messageId, 12345, "hello");

        // Assert
        var culture = _cultureSource.Culture.Name;
        action.Should().ThrowExactly<FormatException>()
            .WithMessage(translation switch
            {
                null => $"The {culture} translation for '{messageId}' could not be formatted with the arguments [12345, hello].",
                _ => $"The {culture} translation for '{messageId}' ({translation}) could not be formatted with the arguments [12345, hello].",
            })
            .WithInnerException<FormatException>();
    }

    [Theory]
    [InlineData("my-message-{0", null, false)]
    [InlineData("my-message-{0", "my-translated-message-{0", false)]
    [InlineData("my-message-{0", null, true)]
    [InlineData("my-message-{0", "my-translated-message-{0", true)]
    public void T_MessageHasIsAnInvalidFormatString_Throws(string messageId, string? translation, bool hasArguments)
    {
        // Arrange
        var sut = CreateSut();
        _translationStore.TryGet(default!, default!, default!, default!, out _).ReturnsForAnyArgs(ci =>
        {
            ci[4] = translation;
            return translation is not null;
        });

        // Act
        var action = () => hasArguments ? sut.T(messageId, 12345) : sut.T(messageId);

        // Assert
        var culture = _cultureSource.Culture.Name;
        var argumentList = hasArguments ? "12345" : "";
        action.Should().ThrowExactly<FormatException>()
            .WithMessage(translation switch
            {
                null => $"The {culture} translation for '{messageId}' could not be formatted with the arguments [{argumentList}].",
                _ => $"The {culture} translation for '{messageId}' ({translation}) could not be formatted with the arguments [{argumentList}].",
            })
            .WithInnerException<FormatException>();
    }

    [Fact]
    public void T_MessageStoreThrows_Throws()
    {
        // Arrange
        var sut = CreateSut();

        var storeError = new Exception("💥Kaboom💥");
        _translationStore.TryGet(default!, default!, default!, default!, out _).ThrowsForAnyArgs(storeError);

        // Act
        var action = () => sut.T("my-message");

        // Assert
        var culture = _cultureSource.Culture.Name;
        action.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage($"The {culture} translation for 'my-message' could not be loaded from the store.")
            .WithInnerException<Exception>().Which.Should().BeSameAs(storeError);
    }

    [Theory]
    [InlineData("")]
    [InlineData("my-context")]
    public void N_Always_QueryTheStoreForTheTranslation(string context)
    {
        // Arrange
        var sut = CreateSut(domain: "my-domain", context: context);

        // Act
        sut.N(123, "my-message", "my-plural");

        // Assert
        _translationStore.ReceivedWithAnyArgs(1).TryGetPlural(default!, default!, default!, default, default!, out _);
        _translationStore.Received(1).TryGetPlural("my-domain", _cultureSource.Culture, context, 123, "my-message", out _);
    }

    [Theory]
    [InlineData("Parameter not set")]
    [InlineData("One arg")]
    [InlineData("Empty arg array")]
    [InlineData("Null arg array")]
    [SuppressMessage("ReSharper", "RedundantExplicitParamsArrayCreation", Justification = "For testing completeness")]
    public void N_MessageFoundAndIsNotAFormatString_ReturnsMessage(string argsCase)
    {
        // Arrange
        var sut = CreateSut();

        _translationStore.TryGetPlural(default!, default!, default!, default, default!, out _).ReturnsForAnyArgs(ci =>
        {
            ci[5] = "my-translated-message";
            return true;
        });

        // Act
        var result = argsCase switch
        {
            "Parameter not set" => sut.N(1, "my-message", "my-plural"),
            "One arg" => sut.N(1, "my-message", "my-plural", 12345),
            "Empty arg array" => sut.N(1, "my-message", "my-plural", []),
            "Null arg array" => sut.N(1, "my-message", "my-plural", null!),
            _ => throw new NotSupportedException($"Unsupported test case: {argsCase}"),
        };

        // Assert
        result.Should().Be("my-translated-message");
    }

    [Theory]
    [CombinatorialData]
    public void N_MessageFoundAndIsAFormatString_ReturnsFormattedMessage(bool originalMessageWasAFormatString)
    {
        // Arrange
        var sut = CreateSut();

        _translationStore.TryGetPlural(default!, default!, default!, default, default!, out _).ReturnsForAnyArgs(ci =>
        {
            ci[5] = "my-translated-message-{0:N0}";
            return true;
        });

        // Act
        var result = sut.N(1, originalMessageWasAFormatString ? "my-message-{0}" : "my-message", "my-plural", 12345);

        // Assert
        var expected = string.Format(_cultureSource.Culture, "my-translated-message-{0:N0}", 12345);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Parameter not set")]
    [InlineData("One arg")]
    [InlineData("Empty arg array")]
    [InlineData("Null arg array")]
    [SuppressMessage("ReSharper", "RedundantExplicitParamsArrayCreation", Justification = "For testing completeness")]
    public void N_MessageNotFoundAndIsNotAFormatString_ReturnsMessage(string argsCase)
    {
        // Arrange
        var sut = CreateSut();

        _translationStore.TryGetPlural(default!, default!, default!, default, default!, out _).ReturnsForAnyArgs(ci =>
        {
            ci[5] = null;
            return false;
        });

        // Act
        var result = argsCase switch
        {
            "Parameter not set" => sut.N(2, "my-message", "my-plural"),
            "One arg" => sut.N(2, "my-message", "my-plural", 12345),
            "Empty arg array" => sut.N(2, "my-message", "my-plural", []),
            "Null arg array" => sut.N(2, "my-message", "my-plural", null!),
            _ => throw new NotSupportedException($"Unsupported test case: {argsCase}"),
        };

        // Assert
        result.Should().Be("my-message");
    }

    [Fact]
    public void N_MessageNotFoundAndIsAFormatString_ReturnsFormattedMessage()
    {
        // Arrange
        var sut = CreateSut();

        _translationStore.TryGetPlural(default!, default!, default!, default, default!, out _).ReturnsForAnyArgs(ci =>
        {
            ci[5] = null;
            return false;
        });

        // Act
        var result = sut.N(2, "my-message-{0:N0}", "my-plural-{0:N0}", 12345);

        // Assert
        var expected = string.Format(_cultureSource.Culture, "my-message-{0:N0}", 12345);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("my-message", null)]
    [InlineData("my-message", "my-translated-message")]
    [InlineData("my-message-{0}-{1}", null)]
    [InlineData("my-message-{0}-{1}", "my-translated-message-{0}-{1}")]
    public void N_MessageHasAndAdditionalArguments_ReturnsFormattedMessage(string messageId, string? translation)
    {
        // Arrange
        var sut = CreateSut();
        _translationStore.TryGetPlural(default!, default!, default!, default, default!, out _).ReturnsForAnyArgs(ci =>
        {
            ci[5] = translation;
            return translation is not null;
        });

        // Act
        var result = sut.N(2, messageId, "my-plural", 12345, "hello", "world");

        // Assert
        var expected = string.Format(_cultureSource.Culture, translation ?? messageId, 12345, "hello");
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("my-message-{0}-{1}-{2}", null)]
    [InlineData("my-message-{0}-{1}-{2}", "my-translated-message-{0}-{1}-{2}")]
    public void N_MessageHasAMissingArgument_Throws(string messageId, string? translation)
    {
        // Arrange
        var sut = CreateSut();
        _translationStore.TryGetPlural(default!, default!, default!, default, default!, out _).ReturnsForAnyArgs(ci =>
        {
            ci[5] = translation;
            return translation is not null;
        });

        // Act
        var action = () => sut.N(2, messageId, "my-plural", 12345, "hello");

        // Assert
        var culture = _cultureSource.Culture.Name;
        action.Should().ThrowExactly<FormatException>()
            .WithMessage(translation switch
            {
                null => $"The {culture} translation for '{messageId}' could not be formatted with the arguments [12345, hello].",
                _ => $"The {culture} translation for '{messageId}' ({translation}) could not be formatted with the arguments [12345, hello].",
            })
            .WithInnerException<FormatException>();
    }

    [Theory]
    [InlineData("my-message-{0", null, false)]
    [InlineData("my-message-{0", "my-translated-message-{0", false)]
    [InlineData("my-message-{0", null, true)]
    [InlineData("my-message-{0", "my-translated-message-{0", true)]
    public void N_MessageHasIsAnInvalidFormatString_Throws(string messageId, string? translation, bool hasArguments)
    {
        // Arrange
        var sut = CreateSut();
        _translationStore.TryGetPlural(default!, default!, default!, default, default!, out _).ReturnsForAnyArgs(ci =>
        {
            ci[5] = translation;
            return translation is not null;
        });

        // Act
        var action = () => hasArguments ? sut.N(2, messageId, "my-plural", 12345) : sut.N(2, messageId, "my-plural");

        // Assert
        var culture = _cultureSource.Culture.Name;
        var argumentList = hasArguments ? "12345" : "";
        action.Should().ThrowExactly<FormatException>()
            .WithMessage(translation switch
            {
                null => $"The {culture} translation for '{messageId}' could not be formatted with the arguments [{argumentList}].",
                _ => $"The {culture} translation for '{messageId}' ({translation}) could not be formatted with the arguments [{argumentList}].",
            })
            .WithInnerException<FormatException>();
    }

    [Fact]
    public void N_MessageStoreThrows_Throws()
    {
        // Arrange
        var sut = CreateSut();

        var storeError = new Exception("💥Kaboom💥");
        _translationStore.TryGetPlural(default!, default!, default!, default, default!, out _).ThrowsForAnyArgs(storeError);

        // Act
        var action = () => sut.N(1, "my-message", "my-plural");

        // Assert
        var culture = _cultureSource.Culture.Name;
        action.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage($"The {culture} translation for 'my-message' could not be loaded from the store.")
            .WithInnerException<Exception>().Which.Should().BeSameAs(storeError);
    }

    #region Helpers

    private Translator CreateSut(string domain = "<domain>", string context = "<context>") => new Translator(_translationStore, _cultureSource, domain, context);

    #endregion
}