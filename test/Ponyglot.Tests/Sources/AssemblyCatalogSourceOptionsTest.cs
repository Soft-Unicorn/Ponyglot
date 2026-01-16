using AwesomeAssertions;
using Ponyglot.Sources;
using Xunit;

namespace Ponyglot.Tests.Sources;

public class AssemblyCatalogSourceOptionsTest
{
    private readonly AssemblyCatalogSourceOptions _sut = new();

    [Fact]
    public void Filter_Created_ShouldBeNull()
    {
        // Arrange

        // Act
        var value = _sut.Filter;

        // Assert
        value.Should().BeNull();
    }

    [Fact]
    public void CatalogNameResolver_Created_ShouldBeNull()
    {
        // Arrange

        // Act
        var value = _sut.CatalogNameResolver;

        // Assert
        value.Should().BeNull();
    }
}