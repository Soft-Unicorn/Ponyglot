using System.IO;
using AwesomeAssertions;
using Ponyglot.Sources;
using Xunit;

namespace Ponyglot.Tests.Sources;

public class FileSystemCatalogSourceOptionsTest
{
    private readonly FileSystemCatalogSourceOptions _sut = new();

    [Fact]
    public void FileSearchOptions_Created_HasCorrectDefaults()
    {
        // Arrange

        // Act
        var value = _sut.FileSearchOptions;

        // Assert
        value.Should().BeEquivalentTo(new EnumerationOptions { RecurseSubdirectories = true });
    }

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