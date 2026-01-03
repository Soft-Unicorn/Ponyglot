using System;
using System.Globalization;
using System.Linq;
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
    [ClassData(typeof(SuccessTestData.T))]
    public void T_NoError_ReturnsTheCorrectValue(bool format, bool found, string argsCase)
    {
        // Arrange
        var sut = CreateSut();

        _translationStore.TryGet(default!, default!, default!, default!, out _).ReturnsForAnyArgs(ci =>
        {
            ci[4] = found switch
            {
                true => format ? "my-translation-{0:0.00}" : "my-translation",
                false => null,
            };
            return found;
        });

        object?[]? args = argsCase switch
        {
            "null" => null,
            "empty" => [],
            "more" => format ? [123, "X"] : ["X"],
            "right count" => format ? [123] : [],
            _ => throw new NotSupportedException($"Unsupported args test case: {argsCase}"),
        };

        var messageId = format ? "my-message-{0:0.00}" : "my-message";

        // Act
        var result = sut.T(messageId, args);

        // Expected
        result.Should().Be(found switch
        {
            true => format ? "my-translation-123.00" : "my-translation",
            false => format ? "my-message-123.00" : "my-message",
        });
    }

    [Theory]
    [ClassData(typeof(ErrorTestData.T))]
    public void T_FormatError_Throws(bool found, string errorCase)
    {
        // Arrange
        var sut = CreateSut();

        string? translation;
        string messageId;
        switch (errorCase)
        {
            case "arg missing":
                translation = found ? "my-translation-{0:0.00}-{1}-{2}" : null;
                messageId = "my-message-{0:0.00}-{1}-{2}";
                break;
            case "invalid format string":
                translation = found ? "my-translation-{0" : null;
                messageId = found ? "my-message-{0}" : "my-message-{0";
                break;
            default:
                throw new NotSupportedException($"Unsupported error case: {errorCase}");
        }

        _translationStore.TryGet(default!, default!, default!, default!, out _).ReturnsForAnyArgs(ci =>
        {
            ci[4] = translation;
            return translation != null;
        });

        // Act
        var action = () => sut.T(messageId, 123, "hello");

        // Assert
        var culture = _cultureSource.Culture.Name;
        action.Should().ThrowExactly<FormatException>()
            .WithMessage(translation switch
            {
                null => $"The {culture} translation for '{messageId}' could not be formatted with the arguments [123, hello].",
                _ => $"The {culture} translation for '{messageId}' ({translation}) could not be formatted with the arguments [123, hello].",
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
    [ClassData(typeof(SuccessTestData.N))]
    public void N_NoError_ReturnsTheCorrectValue(int count, bool format, bool found, string argsCase)
    {
        // Arrange
        var sut = CreateSut();

        _translationStore.TryGetPlural(default!, default!, default!, default, default!, out _).ReturnsForAnyArgs(ci =>
        {
            ci[5] = found switch
            {
                true => format ? "my-translation-{0:0.00}" : "my-translation",
                false => null,
            };
            return found;
        });

        object?[]? args = argsCase switch
        {
            "null" => null,
            "empty" => [],
            "more" => format ? [123, "X"] : ["X"],
            "right count" => format ? [123] : [],
            _ => throw new NotSupportedException($"Unsupported args test case: {argsCase}"),
        };

        var messageId = format ? "my-message-{0:0.00}" : "my-message";
        var pluralId = format ? "my-plural-{0:0.00}" : "my-plural";

        // Act
        var result = sut.N(count, messageId, pluralId, args);

        // Expected
        result.Should().Be(found switch
        {
            true => format ? "my-translation-123.00" : "my-translation",
            false => count switch
            {
                1 => format ? "my-message-123.00" : "my-message",
                _ => format ? "my-plural-123.00" : "my-plural",
            },
        });
    }

    [Theory]
    [ClassData(typeof(ErrorTestData.N))]
    public void N_FormatError_Throws(int count, bool found, string errorCase)
    {
        // Arrange
        var sut = CreateSut();

        string? translation;
        string messageId;
        string pluralId;
        switch (errorCase)
        {
            case "arg missing":
                translation = found ? "my-translation-{0:0.00}-{1}-{2}" : null;
                messageId = "my-message-{0:0.00}-{1}-{2}";
                pluralId = "my-plural-{0:0.00}-{1}-{2}";
                break;
            case "invalid format string":
                translation = found ? "my-translation-{0" : null;
                messageId = found ? "my-message-{0}" : "my-message-{0";
                pluralId = found ? "my-plural-{0}" : "my-plural-{0";
                break;
            default:
                throw new NotSupportedException($"Unsupported error case: {errorCase}");
        }

        _translationStore.TryGetPlural(default!, default!, default!, default, default!, out _).ReturnsForAnyArgs(ci =>
        {
            ci[5] = translation;
            return translation != null;
        });

        // Act
        var action = () => sut.N(count, messageId, pluralId, 123, "hello");

        // Assert
        var culture = _cultureSource.Culture.Name;
        action.Should().ThrowExactly<FormatException>()
            .WithMessage(translation switch
            {
                null => $"The {culture} translation for '{(count == 1 ? messageId : pluralId)}' could not be formatted with the arguments [123, hello].",
                _ => $"The {culture} translation for '{(count == 1 ? messageId : pluralId)}' ({translation}) could not be formatted with the arguments [123, hello].",
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

    #region Test Data

    public static class SuccessTestData
    {
        private static readonly (bool Format, bool Found, string CaseArgs)[] BaseData =
        [
            (false, false, "null"),
            (false, false, "empty"),
            (false, false, "more"),
            (false, true, "null"),
            (false, true, "empty"),
            (false, true, "more"),
            (true, false, "right count"),
            (true, false, "more"),
            (true, true, "right count"),
            (true, true, "more"),
        ];

        private static readonly int[] Counts = [0, 1, 2];

        // P0=format, P1=found, P2=args case
        public class T : TheoryData<bool, bool, string>
        {
            public T()
                : base(BaseData)
            {
            }
        }

        // P0=count, P1=format, P2=found, P3=args case
        public class N : TheoryData<int, bool, bool, string>
        {
            public N()
                : base(Counts.SelectMany(count => BaseData.Select(r => (count, r.Format, r.Found, r.CaseArgs))))
            {
            }
        }
    }

    public static class ErrorTestData
    {
        private static readonly (bool Found, string CaseArgs)[] BaseData =
        [
            (false, "arg missing"),
            (false, "invalid format string"),
            (true, "arg missing"),
            (true, "invalid format string"),
        ];

        private static readonly int[] Counts = [0, 1, 2];

        // P0=format, P1=found, P2=args case
        public class T : TheoryData<bool, string>
        {
            public T()
                : base(BaseData)
            {
            }
        }

        // P0=count, P1=format, P2=found, P3=args case
        public class N : TheoryData<int, bool, string>
        {
            public N()
                : base(Counts.SelectMany(count => BaseData.Select(r => (count, r.Found, r.CaseArgs))))
            {
            }
        }
    }

    #endregion

    #region Helpers

    private Translator CreateSut(string domain = "<domain>", string context = "<context>") => new Translator(_translationStore, _cultureSource, domain, context);

    #endregion
}