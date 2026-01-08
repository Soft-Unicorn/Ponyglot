using System;
using System.Reflection;
using AwesomeAssertions;
using NSubstitute;
using Xunit;

namespace Ponyglot.Tests;

public class TranslatorConventionsTest
{
    [Fact]
    public void ResolveType_AssemblyNameAndTypeFullNameAvailable_ReturnsCorrectCatalogNameAndContext()
    {
        // Arrange
        var type = GetType();

        // Act
        var (catalogName, context) = TranslatorConventions.ResolveType(type);

        // Assert
        catalogName.Should().Be(type.Assembly.GetName().Name);
        context.Should().Be(type.FullName);
    }

    [Fact]
    public void ResolveType_AssemblyNameNotAvailable_ReturnsCorrectCatalogNameAndContext()
    {
        // Arrange
        var type = Substitute.For<Type>();
        type.Assembly.Returns(new AssemblyWithoutName());
        type.FullName.Returns(GetType().FullName);

        // Act
        var (catalogName, context) = TranslatorConventions.ResolveType(type);

        // Assert
        catalogName.Should().BeEmpty();
        context.Should().Be(type.FullName);

        type.Assembly.GetName().Name.Should().BeNull(because: "The test is void if the assembly name of the type is not null");
    }

    [Fact]
    public void ResolveType_FullNameNotAvailable_ReturnsCorrectCatalogNameAndContext()
    {
        // Arrange
        var type = Substitute.For<Type>();
        type.Assembly.Returns(typeof(object).Assembly);
        type.FullName.Returns((string?)null);

        // Act
        var (catalogName, context) = TranslatorConventions.ResolveType(type);

        // Assert
        catalogName.Should().Be(typeof(object).Assembly.GetName().Name);
        context.Should().BeEmpty();

        type.FullName.Should().BeNull(because: "The test is void if the full name of the type is not null");
    }

    [Fact]
    public void ResolveType_TypeIsNull_Throws()
    {
        // Arrange

        // Act
        Action action = () => TranslatorConventions.ResolveType(null!);

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName("type");
    }

    #region Helpers

    // ReSharper disable all

    internal class AssemblyWithoutName : Assembly
    {
        public override AssemblyName GetName() => new AssemblyName();
    }

    #endregion
}