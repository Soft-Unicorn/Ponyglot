using System;
using System.Globalization;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.Extensions;
using Ponyglot.Tests._TestUtils;
using Xunit;

namespace Ponyglot.Tests;

public class TranslatorTest
{
    private readonly TranslationStore _translationStore;
    private readonly ICultureSource _cultureSource;
    private readonly TranslatorDouble _sut;

    public TranslatorTest()
    {
        _translationStore = Substitute.For<TranslationStore>();

        _cultureSource = Substitute.For<ICultureSource>();
        _cultureSource.Culture.Returns(new CultureInfo("qps-Ploc"));

        _sut = Substitute.ForPartsOf<TranslatorDouble>(_translationStore, _cultureSource, "my-catalog", "my-context");
    }

    [Theory]
    [InlineData("translationStore")]
    [InlineData("cultureSource")]
    [InlineData("catalogName")]
    [InlineData("context")]
    public void Constructor_ArgumentIsNull_Throws(string parameterName)
    {
        // Arrange

        // Act
        var action = () => new Translator(
            parameterName == "translationStore" ? null! : _translationStore,
            parameterName == "cultureSource" ? null! : _cultureSource,
            parameterName == "catalogName" ? null! : "my-catalog",
            parameterName == "context" ? null! : "my-context");

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName(parameterName);
    }

    [Fact]
    public void Store_Created_ReturnsTheConstructorValue()
    {
        // Arrange

        // Act
        var store = _sut.Store;

        // Assert
        store.Should().BeSameAs(_translationStore);
    }

    [Fact]
    public void CultureSource_Created_ReturnsTheConstructorValue()
    {
        // Arrange

        // Act
        var cultureSource = _sut.CultureSource;

        // Assert
        cultureSource.Should().BeSameAs(_cultureSource);
    }

    [Fact]
    public void CatalogName_Created_ReturnsTheConstructorValue()
    {
        // Arrange

        // Act
        var catalogName = _sut.CatalogName;

        // Assert
        catalogName.Should().Be("my-catalog");
    }

    [Fact]
    public void Context_Created_ReturnsTheConstructorValue()
    {
        // Arrange

        // Act
        var context = _sut.Context;

        // Assert
        context.Should().Be("my-context");
    }

    [Theory]
    [InlineData("ja-JP")]
    [InlineData("en")]
    public void ForCulture_NoState_ReturnsAnInstanceLockedOnTheSpecifiedCulture(string cultureName)
    {
        // Arrange

        var culture = new CultureInfo(cultureName);

        // Act
        var result = _sut.ForCulture(culture);

        // Assert
        result.Should().BeOfType<Translator>().Which.Should().Satisfy<Translator>(o =>
        {
            o.Context.Should().Be(_sut.Context);
            o.CatalogName.Should().Be(_sut.CatalogName);
            o.GetTranslationStore().Should().BeSameAs(_translationStore);
            o.GetCultureSource().Culture.Should().BeSameAs(culture).And.NotBeSameAs(_cultureSource.Culture);
        });
    }

    [Theory]
    [CombinatorialData]
    public void T_Always_UsesGetTranslation([CombinatorialMemberData(nameof(ArgsOrNoArgsTestData))] object?[]? args)
    {
        // Arrange
        _sut.Configure().GetTranslation_(default, default!, default!, default).ReturnsForAnyArgs("my-translation");

        // Act
        var translation = _sut.T("my-message-id", args);

        // Assert
        _sut.Received(1).GetTranslation_(count: null, "my-message-id", "my-message-id", args);
        translation.Should().Be("my-translation");
    }

    [Theory]
    [CombinatorialData]
    public void N_Always_UsesGetTranslation(
        [CombinatorialValues(0, 1, 2)] int count,
        [CombinatorialMemberData(nameof(ArgsOrNoArgsTestData))] object?[]? args)
    {
        // Arrange
        _sut.Configure().GetTranslation_(default, default!, default!, default).ReturnsForAnyArgs("my-translation");

        // Act
        var translation = _sut.N(count, "my-message-id", "my-plural-id", args);

        // Assert
        _sut.Received(1).GetTranslation_(count: count, "my-message-id", count == 1 ? "my-message-id" : "my-plural-id", args);
        translation.Should().Be("my-translation");
    }

    [Fact]
    public void GetTranslation_Always_SearchesForTheTranslation()
    {
        // Arrange

        // Act
        _sut.GetTranslation_(123, "my-message-id", "my-default-message", default);

        // Assert
        _translationStore.Received(1).TryGet(_sut.CatalogName, _cultureSource.Culture, _sut.Context, 123, "my-message-id", out _);
    }

    [Theory]
    [CombinatorialData]
    public void GetTranslation_StoreFails_Throws(bool defaultMessageIsSameThanMessageId)
    {
        // Arrange
        var error = new Exception("💥Kaboom💥");
        _translationStore.TryGet(default!, default!, default!, default, default!, out _).ThrowsForAnyArgs(error);

        var messageId = "my-message-id";
        var defaultMessage = defaultMessageIsSameThanMessageId ? messageId : "my-default-message";

        // Act
        var action = () => _sut.GetTranslation_(default, messageId, defaultMessage, default);

        // Assert
        var expectedMessage = defaultMessageIsSameThanMessageId switch
        {
            true => $"The {_cultureSource.Culture.Name} translation for '{messageId}' could not be loaded from the store.",
            false => $"The {_cultureSource.Culture.Name} translation for '{messageId}' ({defaultMessage}) could not be loaded from the store.",
        };

        action.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage(expectedMessage)
            .WithInnerException<Exception>().Which.Should().BeSameAs(error);
    }

    [Fact]
    public void GetTranslation_FormatTranslationFound_ReturnsFormattedMessage()
    {
        // Arrange
        SetFoundTranslation(TranslationForm.CompositeFormat("my-translation-{0}-{1}"));

        // Act
        var result = _sut.GetTranslation_(default, default!, default!, [123, "X"]);

        // Assert
        result.Should().Be("my-translation-123-X");
    }

    [Theory]
    [InlineData("my-translation-{99}")]
    [InlineData("my-translation-{bad-format")]
    public void GetTranslation_FormatTranslationFoundAndFormattingFails_Throws(string translation)
    {
        // Arrange
        SetFoundTranslation(TranslationForm.CompositeFormat(translation));

        // Act
        var action = () => _sut.GetTranslation_(default, "my-message-id", default!, [123, "hello", true, false]);

        // Assert
        action.Should().ThrowExactly<FormatException>()
            .WithMessage($"The {_cultureSource.Culture.Name} translation for 'my-message-id' ({translation}) could not be formatted with the arguments [123, \"hello\", true, false].");
    }

    [Theory]
    [CombinatorialData]
    public void GetTranslation_TextTranslationFound_ReturnsText([CombinatorialMemberData(nameof(ArgsOrNoArgsTestData))] object?[]? args)
    {
        // Arrange
        SetFoundTranslation(TranslationForm.Text("my-translation"));

        // Act
        var result = _sut.GetTranslation_(default, default!, default!, args);

        // Assert
        result.Should().Be("my-translation");
    }

    [Theory]
    [CombinatorialData]
    public void GetTranslation_TextTranslationFoundAndTextIsAnInvalidFormatString_ReturnsText([CombinatorialMemberData(nameof(ArgsOrNoArgsTestData))] object?[]? args)
    {
        // Arrange
        SetFoundTranslation(TranslationForm.Text("my-translation{bad-format"));

        // Act
        var result = _sut.GetTranslation_(default, default!, default!, args);

        // Assert
        result.Should().Be("my-translation{bad-format");
    }

    [Theory]
    [InlineData("my-message", null, "my-message")]
    [InlineData("my-message", new object?[] { }, "my-message")]
    [InlineData("my-message", new object?[] { 123 }, "my-message")]
    [InlineData("my-message-{0}", null, "my-message-{0}")]
    [InlineData("my-message-{0}", new object?[] { }, "my-message-{0}")]
    [InlineData("my-message-{0}", new object?[] { 123 }, "my-message-123")]
    [InlineData("my-message-{bad_format", null, "my-message-{bad_format")]
    [InlineData("my-message-{bad_format", new object?[] { }, "my-message-{bad_format")]
    public void GetTranslation_NotFound_ReturnsTranslation(string defaultMessage, object?[]? args, string expected)
    {
        // Arrange

        // Act
        var result = _sut.GetTranslation_(default, default!, defaultMessage, args);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [CombinatorialData]
    public void GetTranslation_NotFoundWithArgsAndMessageCannotBeFormatted_Throws(
        [CombinatorialValues("my-translation-{99}", "my-translation-{bad-format")] string defaultMessage,
        bool defaultMessageIsSameThanMessageId)
    {
        // Arrange
        SetFoundTranslation(TranslationForm.CompositeFormat(defaultMessage));

        var messageId = defaultMessageIsSameThanMessageId ? defaultMessage : "my-message-id";

        // Act
        var action = () => _sut.GetTranslation_(default, messageId, defaultMessage, [123, "hello", true, false]);

        // Assert
        var expectedMessage = defaultMessageIsSameThanMessageId switch
        {
            true => $"The {_cultureSource.Culture.Name} translation for '{messageId}' could not be formatted with the arguments [123, \"hello\", true, false].",
            false => $"The {_cultureSource.Culture.Name} translation for '{messageId}' ({defaultMessage}) could not be formatted with the arguments [123, \"hello\", true, false].",
        };

        action.Should().ThrowExactly<FormatException>().WithMessage(expectedMessage);
    }

    [Theory]
    [InlineData(true, "en", ",")]
    [InlineData(true, "fr", "\u202f")]
    [InlineData(true, "de-CH", "’")]
    [InlineData(false, "en", ",")]
    [InlineData(false, "fr", "\u202f")]
    [InlineData(false, "de-CH", "’")]
    public void GetTranslation_FormatTranslation_FormatsUsingCurrentCulture(bool found, string cultureName, string thousandSeparator)
    {
        // Arrange
        _cultureSource.Culture.Returns(new CultureInfo(cultureName));

        if (found)
            SetFoundTranslation(TranslationForm.CompositeFormat("{0:N0}"));

        // Act
        var result = _sut.GetTranslation_(default, default!, "{0:N0}", [1234]);

        // Assert
        result.Should().Be($"1{thousandSeparator}234");
    }

    #region Test Data

    public static readonly TheoryData<object?[]?> ArgsOrNoArgsTestData =
    [
        (object?[]?)null,
        (object?[]?)[],
        (object?[]?)["X"],
    ];

    #endregion

    #region Helpers

    // ReSharper disable all

    internal class TranslatorDouble : Translator
    {
        public TranslatorDouble(TranslationStore translationStore, ICultureSource cultureSource, string catalogName, string context)
            : base(translationStore, cultureSource, catalogName, context)
        {
        }

        public new TranslationStore Store => base.Store;
        public new ICultureSource CultureSource => base.CultureSource;

        public virtual string GetTranslation_(long? count, string messageId, string defaultMessage, object?[]? args) => base.GetTranslation(count, messageId, defaultMessage, args);
        protected sealed override string GetTranslation(long? count, string messageId, string defaultMessage, object?[]? args) => GetTranslation_(count, messageId, defaultMessage, args);
    }

    private void SetFoundTranslation(TranslationForm translation)
    {
        _translationStore.TryGet(default!, default!, default!, default, default!, out _).ReturnsForAnyArgs(ci =>
        {
            ci[5] = translation;
            return true;
        });
    }

    #endregion
}