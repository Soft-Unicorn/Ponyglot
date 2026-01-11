using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Ponyglot.Loading;
using Xunit;

namespace Ponyglot.Tests.Loading;

public class CatalogResourceTest
{
    [Theory]
    [InlineData("uri")]
    public void Constructor_ArgumentIsNull_Throws(string parameterName)
    {
        // Arrange

        // Act
        var action = () => new CatalogResourceImpl(
            parameterName == "uri" ? null! : new Uri("my://resource"));

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName(parameterName);
    }

    [Fact]
    public void Uri_Created_ReturnsConstructorValue()
    {
        // Arrange
        var resourceUri = new Uri("my://resource");
        var sut = new CatalogResourceImpl(resourceUri);

        // Act
        var uri = sut.Uri;

        // Assert
        uri.Should().BeSameAs(resourceUri);
    }

    [Fact]
    public void ToString_Created_ReturnsTheUri()
    {
        // Arrange
        var sut = new CatalogResourceImpl(new Uri("my://resource"));

        // Act
        var result = sut.ToString();

        // Assert
        result.Should().Be("my://resource/");
    }

    #region Helpers

    private class CatalogResourceImpl : CatalogResource
    {
        public CatalogResourceImpl(Uri uri)
            : base(uri)
        {
        }

        public override Task<Stream> OpenAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    #endregion
}