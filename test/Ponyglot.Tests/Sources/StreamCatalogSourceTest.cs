using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.Extensions;
using Ponyglot.Sources;
using Ponyglot.Tests._TestUtils;
using Xunit;

namespace Ponyglot.Tests.Sources;

[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]
public class StreamCatalogSourceTest
{
    private readonly ICatalogReader _catalogReader;
    private readonly StreamCatalogSourceImpl _sut;

    public StreamCatalogSourceTest()
    {
        _catalogReader = Substitute.For<ICatalogReader>();
        _sut = Substitute.ForPartsOf<StreamCatalogSourceImpl>(_catalogReader);
    }

    [Theory]
    [InlineData("catalogReader")]
    public void Constructor_ArgumentIsNull_Throws(string parameterName)
    {
        // Arrange

        // Act
        var action = () => new StreamCatalogSourceImpl(
            parameterName == "catalogReader" ? null! : _catalogReader);

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName(parameterName);
    }

    [Fact]
    public async Task LoadCatalogsAsync_Always_EnumeratesTheResources()
    {
        // Arrange

        // Act
        await _sut.LoadCatalogsAsync(TestContext.Current.CancellationToken).ConsumeAsync();

        // Assert
        _sut.Received(1).EnumerateResourcesAsync_(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task LoadCatalogsAsync_ResourcesFound_CallsTheReaderForEachResource()
    {
        // Arrange
        var resources = new[]
        {
            CreateResource("my-resource-a"),
            CreateResource("my-resource-b"),
            CreateResource("my-resource-c"),
            CreateResource("my-resource-d"),
        };

        _sut.Configure().EnumerateResourcesAsync_(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(resources.ToAsyncEnumerable());

        // Act
        await _sut.LoadCatalogsAsync(TestContext.Current.CancellationToken).ConsumeAsync();

        // Assert
        Received.InOrder(() =>
        {
            _catalogReader.TryReadCatalogAsync(resources[0], TestContext.Current.CancellationToken);
            _catalogReader.TryReadCatalogAsync(resources[1], TestContext.Current.CancellationToken);
            _catalogReader.TryReadCatalogAsync(resources[2], TestContext.Current.CancellationToken);
            _catalogReader.TryReadCatalogAsync(resources[3], TestContext.Current.CancellationToken);
        });
    }

    [Fact]
    public async Task LoadCatalogsAsync_ResourcesFound_ReturnsLoadedResources()
    {
        // Arrange
        var resources = new[]
        {
            CreateResource("my-resource-a"),
            CreateResource("my-resource-b"),
            CreateResource("my-resource-c"),
            CreateResource("my-resource-d"),
        };

        _sut.Configure().EnumerateResourcesAsync_(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(resources.ToAsyncEnumerable());

        var catalogA = CreateCatalog("my-datalog-a");
        var catalogC = CreateCatalog("my-datalog-c");
        _catalogReader.TryReadCatalogAsync(resources[0], Arg.Any<CancellationToken>()).Returns(catalogA);
        _catalogReader.TryReadCatalogAsync(resources[2], Arg.Any<CancellationToken>()).Returns(catalogC);

        // Act
        var result = await _sut.LoadCatalogsAsync(TestContext.Current.CancellationToken).RealizeAsync();

        // Assert
        result.Should().BeEquivalentTo([catalogA, catalogC], options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task LoadCatalogsAsync_ResourceEnumerationFails_Throws()
    {
        // Arrange
        var error = new Exception("💥Kaboom💥");
        _sut.Configure().EnumerateResourcesAsync_(Arg.Any<CancellationToken>()).ThrowsForAnyArgs(error);

        // Act
        var action = () => _sut.LoadCatalogsAsync(TestContext.Current.CancellationToken).ConsumeAsync();

        // Assert
        (await action.Should().ThrowAsync<Exception>()).Which.Should().BeSameAs(error);
    }

    [Fact]
    public async Task LoadCatalogsAsync_ReaderFails_Throws()
    {
        // Arrange
        var resources = new[] { CreateResource("my-resource") };
        _sut.Configure().EnumerateResourcesAsync_(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(resources.ToAsyncEnumerable());

        var error = new Exception("💥Kaboom💥");
        _catalogReader.TryReadCatalogAsync(default!, Arg.Any<CancellationToken>()).ThrowsForAnyArgs(error);

        // Act
        var action = () => _sut.LoadCatalogsAsync(TestContext.Current.CancellationToken).ConsumeAsync();

        // Assert
        (await action.Should().ThrowAsync<Exception>()).Which.Should().BeSameAs(error);
    }

    [Fact]
    public async Task LoadCatalogsAsync_CancellationOccurs_Throws()
    {
        // Arrange
        var resources = new[] { CreateResource("my-resource") };
        _sut.Configure().EnumerateResourcesAsync_(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(resources.ToAsyncEnumerable());

        _catalogReader.TryReadCatalogAsync(default!, Arg.Any<CancellationToken>()).ReturnsForAnyArgs(ci =>
        {
            ci.Arg<CancellationToken>().ThrowIfCancellationRequested();
            return null;
        });

        // Act
        var action = () => _sut.LoadCatalogsAsync(new CancellationToken(canceled: true)).ConsumeAsync();

        // Assert
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    #region Helpers

    // ReSharper disable all

    private static StreamResource CreateResource(string uid = "default-resource")
    {
        return Substitute.ForPartsOf<StreamResource>(uid, uid, "");
    }

    private static Catalog CreateCatalog(string? uid = null)
    {
        var pluralRule = Substitute.For<IPluralRule>();
        pluralRule.PluralCount.Returns(1);
        pluralRule.GetPluralForm(default).ReturnsForAnyArgs(0);

        var catalog = new Catalog(uid ?? "my-datalog-uid", "my-catalog", new CultureInfo("qps-Ploc"), pluralRule, []);
        return catalog;
    }

    internal class StreamCatalogSourceImpl : StreamCatalogSource
    {
        public StreamCatalogSourceImpl(ICatalogReader catalogReader)
            : base(catalogReader)
        {
        }

        public virtual IAsyncEnumerable<StreamResource> EnumerateResourcesAsync_(CancellationToken cancellationToken = default) => AsyncEnumerable.Empty<StreamResource>();
        protected sealed override IAsyncEnumerable<StreamResource> EnumerateResourcesAsync(CancellationToken cancellationToken = default) => EnumerateResourcesAsync_(cancellationToken);
    }

    #endregion
}