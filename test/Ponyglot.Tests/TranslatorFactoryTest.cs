using System;
using AwesomeAssertions;
using NSubstitute;
using Ponyglot.Tests._TestUtils;
using Xunit;

namespace Ponyglot.Tests;

public class TranslatorFactoryTest
{
    private readonly TranslationStore _translationStore;
    private readonly ICultureSource _cultureSource;
    private readonly TranslatorFactory _sut;

    public TranslatorFactoryTest()
    {
        _translationStore = Substitute.For<TranslationStore>();
        _cultureSource = Substitute.For<ICultureSource>();
        _sut = Substitute.ForPartsOf<TranslatorFactory>(_translationStore, _cultureSource);
    }

    [Theory]
    [InlineData("translationStore")]
    [InlineData("cultureSource")]
    public void Constructor_ArgumentIsNull_Throws(string parameterName)
    {
        // Arrange

        // Act
        var action = () => new TranslatorFactory(
            parameterName == "translationStore" ? null! : _translationStore,
            parameterName == "cultureSource" ? null! : _cultureSource);

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName(parameterName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("my-context")]
    public void Create_ValidArguments_ReturnsAValidTranslator(string context)
    {
        // Arrange

        // Act
        var translator = _sut.Create("my-catalog", context);

        // Assert
        translator.Should().BeOfType<Translator>().Which.Should().Satisfy<Translator>(o =>
        {
            o.Context.Should().Be(context);
            o.CatalogName.Should().Be("my-catalog");
            o.GetTranslationStore().Should().BeSameAs(_translationStore);
            o.GetCultureSource().Should().BeSameAs(_cultureSource);
        });
    }

    [Fact]
    public void Create_CatalogNameIsNull_Throws()
    {
        // Arrange

        // Act
        var action = () => _sut.Create(null!, "my-context");

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName("catalogName");
    }
}