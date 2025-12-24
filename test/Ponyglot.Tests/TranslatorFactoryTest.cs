using System;
using System.Reflection;
using System.Reflection.Emit;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.Extensions;
using Ponyglot.Tests._TestUtils;
using Xunit;

namespace Ponyglot.Tests;

public class TranslatorFactoryTest
{
    private readonly ITranslationStore _translationStore;
    private readonly ICultureSource _cultureSource;
    private readonly TranslatorFactory _sut;

    public TranslatorFactoryTest()
    {
        _translationStore = Substitute.For<ITranslationStore>();
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

    [Fact]
    public void CreateOfT_Always_ResolvesTheDomainAndContext()
    {
        // Arrange

        // Act
        _sut.Create<MyTranslatedType>();

        // Assert
        _sut.Received(1).ResolveType(typeof(MyTranslatedType));
    }

    [Fact]
    public void CreateOfT_Always_UsesTheNonGenericCreateMethodWithTheResolvedDomainAndContext()
    {
        // Arrange
        _sut.Configure().ResolveType(default!).ReturnsForAnyArgs(("my-domain", "my-context"));

        // Act
        _sut.Create<MyTranslatedType>();

        // Assert
        _sut.Received(1).Create("my-domain", "my-context");
    }

    [Fact]
    public void CreateOfT_Always_ReturnsTheCreatedTranslator()
    {
        // Arrange
        var expectedTranslator = Substitute.For<ITranslator>();
        _sut.Configure().Create(default!, default).ReturnsForAnyArgs(expectedTranslator);

        // Act
        var translator = _sut.Create<MyTranslatedType>();

        // Assert
        translator.Should().BeSameAs(expectedTranslator);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("my-context")]
    public void Create_ValidArguments_ReturnsAValidTranslator(string? context)
    {
        // Arrange

        // Act
        var translator = _sut.Create("my-domain", context);

        // Assert
        translator.Should().BeOfType<Translator>().Which.Should().Satisfy<Translator>(o =>
        {
            o.Context.Should().Be(context);
            o.Domain.Should().Be("my-domain");
            o.GetTranslationStore().Should().BeSameAs(_translationStore);
            o.GetCultureSource().Should().BeSameAs(_cultureSource);
        });
    }

    [Fact]
    public void Create_DomainIsNull_Throws()
    {
        // Arrange

        // Act
        var action = () => _sut.Create(null!, "my-context");

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName("domain");
    }

    [Fact]
    public void ResolveType_AssemblyNameAvailable_ReturnsCorrectDomainAndContext()
    {
        // Arrange
        var type = typeof(MyTranslatedType);

        // Act
        var (domain, context) = _sut.ResolveType(type);

        // Assert
        domain.Should().Be(type.Assembly.GetName().Name);
        context.Should().Be(type.FullName);
    }

    [Fact]
    public void ResolveType_AssemblyNameNotAvailable_ReturnsCorrectDomainAndContext()
    {
        // Arrange
        var assembly = Substitute.For<Assembly>();
        assembly.GetName().Returns(new AssemblyName());

        var type = Substitute.For<Type>();
        type.Assembly.Returns(assembly);

        // Act
        var (domain, context) = _sut.ResolveType(type);

        // Assert
        domain.Should().BeEmpty();
        context.Should().Be(type.FullName);

        type.Assembly.GetName().Name.Should().BeNull(because: "The test is void if the assembly name of the type is not null");
    }

    #region Helpers

    // ReSharper disable all

    private class MyTranslatedType;

    private static Type CreateDynamicTypeWithNullAssemblyName()
    {
        var assemblyName = new AssemblyName(); // Name == null
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

        var moduleBuilder = assemblyBuilder.DefineDynamicModule("TestModule");

        var typeBuilder = moduleBuilder.DefineType("MyNamespace.MyTypeWithoutAssemblyName", TypeAttributes.Public);

        var type = typeBuilder.CreateType();

        return type;
    }

    #endregion
}