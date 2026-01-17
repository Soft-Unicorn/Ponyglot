using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Ponyglot.Sources;
using Ponyglot.Tests._TestUtils;
using Xunit;

namespace Ponyglot.Tests.Sources;

public class HttpCatalogTextManifestReaderTest
{
    private readonly HttpCatalogTextManifestReader _sut = new();

    [Fact]
    public void MediaTypes_Created_ReturnsExpectedMediaTypes()
    {
        // Arrange

        // Act
        var mediaTypes = _sut.MediaTypes;

        // Assert
        mediaTypes.Should().BeEquivalentTo("text/uri-list", "text/plain");
    }

    [Fact]
    public async Task ReadAsync_ValidStream_ReturnsUis()
    {
        // Arrange
        var stream = string.Join(
            "\r\n",
            "# This is a comment.",
            "https://example.com/my-resource-a.txt",
            "",
            "https://example.com/my-resource-b.txt",
            "  https://example.com/my-resource-c.txt",
            "https://example.com/my-resource-d.txt ",
            "            ").AsStream();

        // Act
        var result = await _sut.ReadAsync(stream, TestContext.Current.CancellationToken).RealizeAsync();

        // Assert
        result.Should().BeEquivalentTo(
            "https://example.com/my-resource-a.txt",
            "https://example.com/my-resource-b.txt",
            "https://example.com/my-resource-c.txt",
            "https://example.com/my-resource-d.txt"
        );
    }

    [Fact]
    public async Task ReadAsync_StreamIsNull_Throws()
    {
        // Arrange

        // Act
        var action = () => _sut.ReadAsync(null!, TestContext.Current.CancellationToken).ConsumeAsync();

        // Assert
        await action.Should().ThrowExactlyAsync<ArgumentNullException>().WithParameterName("stream");
    }

    [Fact]
    public async Task ReadAsync_ReadError_Throws()
    {
        // Arrange
        var error = new IOException("💥Kaboom💥");
        var stream = Substitute.ForPartsOf<Stream>();
        stream.CanRead.Returns(true);
        stream.ReadAsync(default!, default!, default!, Arg.Any<CancellationToken>()).ThrowsAsyncForAnyArgs(error);

        // Act
        var action = () => _sut.ReadAsync(stream, TestContext.Current.CancellationToken).ConsumeAsync();

        // Assert
        (await action.Should().ThrowExactlyAsync<IOException>()).Which.Should().BeSameAs(error);
    }

    [Fact]
    public async Task ReadAsync_CancellationOccurs_Throws()
    {
        // Arrange
        var stream = new MemoryStream("""
            https://example.com/my-resource-a.txt           
            https://example.com/my-resource-b.txt
            https://example.com/my-resource-c.txt
            https://example.com/my-resource-d.txt
            """u8.ToArray());

        // Act
        var action = () => _sut.ReadAsync(stream, new CancellationToken(canceled: true)).ConsumeAsync();

        // Assert
        await action.Should().ThrowAsync<OperationCanceledException>();
    }
}