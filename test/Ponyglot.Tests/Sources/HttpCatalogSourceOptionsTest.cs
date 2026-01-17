using AwesomeAssertions;
using Ponyglot.Sources;
using Xunit;

namespace Ponyglot.Tests.Sources;

public class HttpCatalogSourceOptionsTest
{
    private readonly HttpCatalogSourceOptions _sut = new();

    [Fact]
    public void ManifestReaders_Created_ShouldContainTheDefaultReaders()
    {
        // Arrange

        // Act
        var value = _sut.ManifestReaders;

        // Assert
        value.Should().SatisfyRespectively(
            first => first.Should().BeOfType<HttpCatalogTextManifestReader>(),
            second => second.Should().BeOfType<HttpCatalogJsonManifestReader>());
    }

    [Fact]
    public void SameOrigin_Created_ShouldBeNull()
    {
        // Arrange

        // Act
        var value = _sut.SameOrigin;

        // Assert
        value.Should().BeTrue();
    }

    [Fact]
    public void MaxCatalogs_Created_ShouldBeNull()
    {
        // Arrange

        // Act
        var value = _sut.MaxCatalogs;

        // Assert
        value.Should().Be(1000);
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
    public void ConfigureManifestRequest_Created_ShouldBeNull()
    {
        // Arrange

        // Act
        var value = _sut.ConfigureManifestRequest;

        // Assert
        value.Should().BeNull();
    }

    [Fact]
    public void ConfigureCatalogRequest_Created_ShouldBeNull()
    {
        // Arrange

        // Act
        var value = _sut.ConfigureCatalogRequest;

        // Assert
        value.Should().BeNull();
    }
}