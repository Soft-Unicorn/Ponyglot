using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Ponyglot.Sources;
using Xunit;

namespace Ponyglot.Tests.Sources;

public class StreamResourceTest
{
    [Theory]
    [InlineData("uid")]
    [InlineData("name")]
    [InlineData("catalogName")]
    public void Constructor_ArgumentIsNull_Throws(string parameterName)
    {
        // Arrange

        // Act
        var action = () => new StreamResourceImpl(
            parameterName == "uid" ? null! : "my-uid",
            parameterName == "name" ? null! : "my-name",
            parameterName == "catalogName" ? null! : "my-catalog");

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName(parameterName);
    }

    [Theory]
    [InlineData("uid")]
    [InlineData("name")]
    public void Constructor_ArgumentIsEmpty_Throws(string parameterName)
    {
        // Arrange

        // Act
        var action = () => new StreamResourceImpl(
            parameterName == "uid" ? "" : "my-uid",
            parameterName == "name" ? "" : "my-name",
            "");

        // Assert
        action.Should().ThrowExactly<ArgumentException>().WithParameterName(parameterName).WithMessage("*empty*");
    }

    [Fact]
    public void Uid_Created_ReturnsTheConstructorValue()
    {
        // Arrange
        var sut = new StreamResourceImpl(uid: "my-uid");

        // Act
        var uid = sut.Uid;

        // Assert
        uid.Should().Be("my-uid");
    }

    [Fact]
    public void Name_Created_ReturnsTheConstructorValue()
    {
        // Arrange
        var sut = new StreamResourceImpl(name: "my-name");

        // Act
        var name = sut.Name;

        // Assert
        name.Should().Be("my-name");
    }

    [Fact]
    public void CatalogName_Created_ReturnsTheConstructorValue()
    {
        // Arrange
        var sut = new StreamResourceImpl(catalogName: "my-catalog");

        // Act
        var catalogName = sut.CatalogName;

        // Assert
        catalogName.Should().Be("my-catalog");
    }

    [Fact]
    public void ToString_Created_ReturnsTheUid()
    {
        // Arrange
        var sut = new StreamResourceImpl(uid: "my-uid");

        // Act
        var result = sut.ToString();

        // Assert
        result.Should().Be("my-uid");
    }

    #region Helpers

    private class StreamResourceImpl : StreamResource
    {
        public StreamResourceImpl(string uid = "default-uid", string name = "default-name", string catalogName = "default-catalog")
            : base(uid, name, catalogName)
        {
        }

        public override ValueTask<Stream> OpenAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    #endregion
}