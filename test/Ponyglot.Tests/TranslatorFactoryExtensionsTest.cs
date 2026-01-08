using System;
using System.Diagnostics.CodeAnalysis;
using AwesomeAssertions;
using NSubstitute;
using Xunit;

namespace Ponyglot.Tests;

public class TranslatorFactoryExtensionsTest
{
    private readonly ITranslatorFactory _factory = Substitute.For<ITranslatorFactory>();

    [Fact]
    public void CreateOfT_Always_CallsTheFactoryWithTheCorrectCatalogNameAndContext()
    {
        // Arrange

        // Act
        _factory.Create<TranslatorFactoryExtensionsTest>();

        // Assert
        var catalogName = GetCatalogName(typeof(TranslatorFactoryExtensionsTest));
        var context = GetContext(typeof(TranslatorFactoryExtensionsTest));
        _factory.Received(1).Create(catalogName, context);
    }

    [Fact]
    public void CreateOfT_Always_ReturnsTheTranslator()
    {
        // Arrange
        var expectedTranslator = Substitute.For<ITranslator>();

        _factory.Create(default!, default!).ReturnsForAnyArgs(expectedTranslator);

        // Act
        var translator = _factory.Create<object>();

        // Assert
        translator.Should().BeSameAs(expectedTranslator);
    }

    [Fact]
    public void CreateOfT_FactoryIsNull_Throws()
    {
        // Arrange
        ITranslatorFactory nullFactory = null!;

        // Act
        var action = () => nullFactory.Create<object>();

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName("factory");
    }

    [Fact]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "Unit test")]
    public void Create_ValidArguments_CallsTheFactoryWithTheCorrectCatalogNameAndContext()
    {
        // Arrange

        // Act
        _factory.Create(typeof(TranslatorFactoryExtensionsTest));

        // Assert
        var catalogName = GetCatalogName(typeof(TranslatorFactoryExtensionsTest));
        var context = GetContext(typeof(TranslatorFactoryExtensionsTest));
        _factory.Received(1).Create(catalogName, context);
    }

    [Fact]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "Unit test")]
    public void Create_ValidArguments_ReturnsTheTranslator()
    {
        // Arrange
        var expectedTranslator = Substitute.For<ITranslator>();

        _factory.Create(default!, default!).ReturnsForAnyArgs(expectedTranslator);

        // Act
        var translator = _factory.Create(typeof(object));

        // Assert
        translator.Should().BeSameAs(expectedTranslator);
    }

    [Fact]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "Unit test")]
    public void Create_FactoryIsNull_Throws()
    {
        // Arrange
        ITranslatorFactory nullFactory = null!;

        // Act
        var action = () => nullFactory.Create(typeof(object));

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName("factory");
    }

    [Fact]
    [SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "Unit test")]
    public void Create_TypeIsNull_Throws()
    {
        // Arrange

        // Act
        var action = () => _factory.Create(null!);

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName("type");
    }

    #region Helpers

    private static string GetCatalogName(Type type) => type.Assembly.GetName().Name ?? throw new InvalidOperationException($"Assembly name is not available for type {type.FullName}.");
    private static string GetContext(Type type) => type.FullName ?? throw new InvalidOperationException($"Full name is not available for type {type.FullName}.");

    #endregion
}