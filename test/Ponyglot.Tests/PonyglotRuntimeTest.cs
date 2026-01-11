using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Ponyglot.Sources;
using Ponyglot.Tests._TestUtils;
using Xunit;

namespace Ponyglot.Tests;

public class PonyglotRuntimeTest
{
    private readonly TranslationStore _translationStore;
    private readonly ICultureSource _cultureSource;
    private readonly ITranslatorFactory _translatorFactory;
    private readonly Func<TranslationStore, ICultureSource, ITranslatorFactory> _translationFactoryProvider;
    private readonly ICatalogSource _source1;
    private readonly ICatalogSource _source2;
    private readonly PonyglotRuntime _sut;

    public PonyglotRuntimeTest()
    {
        _translationStore = Substitute.For<TranslationStore>();
        _cultureSource = Substitute.For<ICultureSource>();
        _translatorFactory = Substitute.For<ITranslatorFactory>();
        _translationFactoryProvider = Substitute.For<Func<TranslationStore, ICultureSource, ITranslatorFactory>>();
        _translationFactoryProvider.Invoke(_translationStore, _cultureSource).Returns(_translatorFactory);

        _source1 = Substitute.For<ICatalogSource>();
        _source2 = Substitute.For<ICatalogSource>();

        _sut = new PonyglotRuntime(_translationStore, _cultureSource, _translationFactoryProvider, [_source1, _source2]);
    }

    [Fact]
    public void Constructor_Always_CallsTheTranslationFactoryProvider()
    {
        // Arrange

        // Act

        // Assert
        _translationFactoryProvider.Received(1).Invoke(_translationStore, _cultureSource);
    }

    [Theory]
    [InlineData("translationStore")]
    [InlineData("cultureSource")]
    [InlineData("translationFactoryProvider")]
    [InlineData("sources")]
    public void Constructor_ArgumentIsNull_Throws(string parameterName)
    {
        // Arrange

        // Act
        var action = () => new PonyglotRuntime(
            parameterName == "translationStore" ? null! : _translationStore,
            parameterName == "cultureSource" ? null! : _cultureSource,
            parameterName == "translationFactoryProvider" ? null! : _translationFactoryProvider,
            parameterName == "sources" ? null! : [_source1]);

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName(parameterName);
    }

    [Fact]
    public void Constructor_SourcesIsEmpty_Throws()
    {
        // Arrange

        // Act
        var action = () => new PonyglotRuntime(_translationStore, _cultureSource, _translationFactoryProvider, []);

        // Assert
        action.Should().ThrowExactly<ArgumentException>().WithMessage("The collection of catalog sources is empty.*");
    }

    [Fact]
    public void Constructor_SourcesContainsANullItem_Throws()
    {
        // Arrange
        var sources = new[]
        {
            Substitute.For<ICatalogSource>(),
            Substitute.For<ICatalogSource>(),
            Substitute.For<ICatalogSource>(),
            null!,
            Substitute.For<ICatalogSource>(),
        };

        // Act
        var action = () => new PonyglotRuntime(_translationStore, _cultureSource, _translationFactoryProvider, sources);

        // Assert
        action.Should().ThrowExactly<ArgumentException>().WithMessage("The catalog source at index 3 is null.*");
    }

    [Fact]
    public void IsInitialized_Created_ReturnsFalse()
    {
        // Arrange

        // Act
        var isInitialized = _sut.IsInitialized;

        // Assert
        isInitialized.Should().BeFalse();
    }

    [Fact]
    public async Task IsInitialized_InitializationInProgress_ReturnsFalse()
    {
        // Arrange
        var firstCallWait = new ManualResetEventSlim(false);
        var firstCallStarted = new ManualResetEventSlim(false);

        _source1.LoadCatalogsAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(
            _ =>
            {
                firstCallStarted.Set();
                firstCallWait.Wait(TestContext.Current.CancellationToken);
                return AsyncEnumerable.Empty<Catalog>();
            },
            _ => AsyncEnumerable.Empty<Catalog>());

        var pendingInitTask = Task.Run(() => _sut.InitializeAsync(TestContext.Current.CancellationToken), TestContext.Current.CancellationToken);
        firstCallStarted.Wait(TestContext.Current.CancellationToken);

        // Act
        var isInitialized = _sut.IsInitialized;

        // Assert
        try
        {
            isInitialized.Should().BeFalse();
        }
        finally
        {
            firstCallWait.Set();
            await pendingInitTask;
        }
    }

    [Fact]
    public async Task IsInitialized_InitializationFailed_ReturnsFalse()
    {
        // Arrange
        _source1.LoadCatalogsAsync(Arg.Any<CancellationToken>()).ThrowsForAnyArgs<Exception>();
        var action = () => _sut.InitializeAsync(CancellationToken.None);
        await action.Should().ThrowAsync<Exception>();

        // Act
        var isInitialized = _sut.IsInitialized;

        // Assert
        isInitialized.Should().BeFalse();
    }

    [Fact]
    public async Task IsInitialized_InitializationSucceeded_ReturnsTrue()
    {
        // Arrange
        await _sut.InitializeAsync(CancellationToken.None);

        // Act
        var isInitialized = _sut.IsInitialized;

        // Assert
        isInitialized.Should().BeTrue();
    }

    [Fact]
    public void Store_Created_ReturnsTheCreatedInstance()
    {
        // Arrange

        // Act
        var store = _sut.Store;

        // Assert
        store.Should().BeSameAs(_translationStore);
    }

    [Fact]
    public void CultureSource_Created_ReturnsTheCreatedInstance()
    {
        // Arrange

        // Act
        var cultureSource = _sut.CultureSource;

        // Assert
        cultureSource.Should().BeSameAs(_cultureSource);
    }

    [Fact]
    public void TranslatorFactory_Created_ReturnsTheCreatedInstance()
    {
        // Arrange

        // Act
        var translatorFactory = _sut.TranslatorFactory;

        // Assert
        translatorFactory.Should().BeSameAs(_translatorFactory);
    }

    [Fact]
    public async Task InitializeAsync_NotInitialized_QueriesAllTheSources()
    {
        // Arrange

        // Act
        await _sut.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        await _source1.ReceivedWithAnyArgs(1).LoadCatalogsAsync(Arg.Any<CancellationToken>()).ConsumeAsync();
        await _source2.ReceivedWithAnyArgs(1).LoadCatalogsAsync(Arg.Any<CancellationToken>()).ConsumeAsync();
    }

    [Fact]
    public async Task InitializeAsync_NotInitialized_InitializeTheStoreWithCatalogsInCorrectOrder()
    {
        // Arrange
        var catalogs1 = new[]
        {
            CreateCatalog("my-catalog-a"),
            CreateCatalog("my-catalog-c"),
        };

        var catalogs2 = new[]
        {
            CreateCatalog("my-catalog-b"),
            CreateCatalog("my-catalog-d"),
        };

        _source1.LoadCatalogsAsync(Arg.Any<CancellationToken>()).Returns(catalogs1.ToAsyncEnumerable());
        _source2.LoadCatalogsAsync(Arg.Any<CancellationToken>()).Returns(catalogs2.ToAsyncEnumerable());

        IEnumerable<Catalog>? initializedCatalogs = null;
        _translationStore.Initialize(Arg.Do<IEnumerable<Catalog>>(x => initializedCatalogs = x));

        // Act
        await _sut.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _translationStore.ReceivedWithAnyArgs(1).Initialize(default!);
        initializedCatalogs!.Should().BeEquivalentTo([..catalogs1, ..catalogs2], options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task InitializeAsync_SourceReturnsANullCatalog_Throws()
    {
        // Arrange
        _source1.LoadCatalogsAsync(Arg.Any<CancellationToken>()).Returns(_ => new[] { CreateCatalog() }.ToAsyncEnumerable());
        _source2.LoadCatalogsAsync(Arg.Any<CancellationToken>()).Returns(new Catalog[] { null! }.ToAsyncEnumerable());

        // Act
        var action = () => _sut.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        await action.Should().ThrowExactlyAsync<InvalidOperationException>()
            .WithMessage($"The catalog source at index 1 ({_source2}) returned a null catalog.");
    }

    [Fact]
    public async Task InitializeAsync_InitializationInProgress_Throws()
    {
        // Arrange
        var firstCallWait = new ManualResetEventSlim(false);
        var firstCallStarted = new ManualResetEventSlim(false);

        _source1.LoadCatalogsAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(
            _ =>
            {
                firstCallStarted.Set();
                firstCallWait.Wait(TestContext.Current.CancellationToken);
                return AsyncEnumerable.Empty<Catalog>();
            },
            _ => AsyncEnumerable.Empty<Catalog>());

        var pendingInitTask = Task.Run(() => _sut.InitializeAsync(TestContext.Current.CancellationToken), TestContext.Current.CancellationToken);
        firstCallStarted.Wait(TestContext.Current.CancellationToken);

        // Act
        var action = () => _sut.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        try
        {
            await action.Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage("The runtime is already initializing.");
        }
        finally
        {
            firstCallWait.Set();
            await pendingInitTask;
        }
    }

    [Fact]
    public async Task InitializeAsync_PreviousInitializationFailed_InitializeAgain()
    {
        // Arrange
        _source1.LoadCatalogsAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(_ => throw new Exception("💥Kaboom💥"), _ => AsyncEnumerable.Empty<Catalog>());

        await Assert.ThrowsAnyAsync<Exception>(() => _sut.InitializeAsync(TestContext.Current.CancellationToken));

        _source1.ClearReceivedCalls();
        _source2.ClearReceivedCalls();
        _translationStore.ClearReceivedCalls();

        // Act
        await _sut.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        await _source1.ReceivedWithAnyArgs(1).LoadCatalogsAsync(Arg.Any<CancellationToken>()).ConsumeAsync();
        await _source2.ReceivedWithAnyArgs(1).LoadCatalogsAsync(Arg.Any<CancellationToken>()).ConsumeAsync();
        _translationStore.ReceivedWithAnyArgs(1).Initialize(default!);
    }

    [Fact]
    public async Task InitializeAsync_AlreadyInitialized_Throws()
    {
        // Arrange
        _source1.LoadCatalogsAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(AsyncEnumerable.Empty<Catalog>());
        await _sut.InitializeAsync(CancellationToken.None);

        _source1.ClearReceivedCalls();
        _source2.ClearReceivedCalls();
        _translationStore.ClearReceivedCalls();

        // Act
        var action = () => _sut.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        await action.Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage("The runtime is already initialized.");

        await _source1.DidNotReceiveWithAnyArgs().LoadCatalogsAsync(Arg.Any<CancellationToken>()).ConsumeAsync();
        await _source2.DidNotReceiveWithAnyArgs().LoadCatalogsAsync(Arg.Any<CancellationToken>()).ConsumeAsync();
        _translationStore.DidNotReceiveWithAnyArgs().Initialize(default!);
    }

    [Fact]
    public async Task InitializeAsync_OperationCancelledExceptionOccurs_Throws()
    {
        // Arrange
        _source1.LoadCatalogsAsync(Arg.Any<CancellationToken>()).ThrowsForAnyArgs<OperationCanceledException>();
        _source2.LoadCatalogsAsync(Arg.Any<CancellationToken>()).ThrowsForAnyArgs<OperationCanceledException>();

        // Act
        var action = () => _sut.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        await action.Should().ThrowAsync<OperationCanceledException>();
        _translationStore.DidNotReceiveWithAnyArgs().Initialize(default!);
    }

    [Fact]
    public async Task InitializeAsync_SourceFails_Throws()
    {
        // Arrange
        var error = new Exception("💥Kaboom💥");
        _source1.LoadCatalogsAsync(Arg.Any<CancellationToken>()).ThrowsForAnyArgs(error);

        // Act
        var action = () => _sut.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        (await action.Should().ThrowExactlyAsync<Exception>()).Which.Should().BeSameAs(error);
        _translationStore.DidNotReceiveWithAnyArgs().Initialize(default!);
    }

    [Fact]
    public async Task InitializeAsync_TranslationStoreFails_Throws()
    {
        // Arrange
        var error = new Exception("💥Kaboom💥");
        _translationStore.WhenForAnyArgs(o => o.Initialize(default!)).Throw(error);

        // Act
        var action = () => _sut.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        (await action.Should().ThrowExactlyAsync<Exception>()).Which.Should().BeSameAs(error);
    }

    #region Helpers

    // ReSharper disable all

    private static Catalog CreateCatalog(string? uid = null)
    {
        var pluralRule = Substitute.For<IPluralRule>();
        pluralRule.PluralCount.Returns(1);
        pluralRule.GetPluralForm(default).ReturnsForAnyArgs(0);

        var catalog = new Catalog(uid ?? "my-catalog-uid", "my-catalog", new CultureInfo("qps-Ploc"), pluralRule, []);
        return catalog;
    }

    #endregion
}