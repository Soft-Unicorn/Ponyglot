using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.Extensions;
using Ponyglot.Loading;
using Xunit;

namespace Ponyglot.Tests;

public class PonyglotRuntimeTest
{
    private readonly TranslationStore _translationStore;
    private readonly ICultureSource _cultureSource;
    private readonly ITranslatorFactory _translatorFactory;
    private readonly Func<TranslationStore, ICultureSource, ITranslatorFactory> _translationFactoryProvider;
    private readonly ICatalogLocator _locator1;
    private readonly ICatalogLocator _locator2;
    private readonly ICatalogLoader _loader1;
    private readonly ICatalogLoader _loader2;
    private readonly PonyglotRuntimeDouble _sut;

    public PonyglotRuntimeTest()
    {
        _translationStore = Substitute.For<TranslationStore>();
        _cultureSource = Substitute.For<ICultureSource>();
        _translatorFactory = Substitute.For<ITranslatorFactory>();
        _translationFactoryProvider = Substitute.For<Func<TranslationStore, ICultureSource, ITranslatorFactory>>();
        _translationFactoryProvider.Invoke(_translationStore, _cultureSource).Returns(_translatorFactory);

        _locator1 = Substitute.For<ICatalogLocator>();
        _locator2 = Substitute.For<ICatalogLocator>();

        var defaultCatalog = CreateCatalog();
        _loader1 = Substitute.For<ICatalogLoader>();
        _loader1.ReturnsForAll(Task.FromResult(defaultCatalog));
        _loader2 = Substitute.For<ICatalogLoader>();
        _loader2.ReturnsForAll(Task.FromResult(defaultCatalog));

        _sut = Substitute.ForPartsOf<PonyglotRuntimeDouble>(
            _translationStore,
            _cultureSource,
            _translationFactoryProvider,
            new[] { _locator1, _locator2 },
            new[] { _loader1, _loader2 });
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
    [InlineData("locators")]
    [InlineData("loaders")]
    public void Constructor_ArgumentIsNull_Throws(string parameterName)
    {
        // Arrange

        // Act
        var action = () => new PonyglotRuntime(
            parameterName == "translationStore" ? null! : _translationStore,
            parameterName == "cultureSource" ? null! : _cultureSource,
            parameterName == "translationFactoryProvider" ? null! : _translationFactoryProvider,
            parameterName == "locators" ? null! : [_locator1],
            parameterName == "loaders" ? null! : [_loader1]);

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName(parameterName);
    }

    [Fact]
    public void Constructor_LocatorsIsEmpty_Throws()
    {
        // Arrange

        // Act
        var action = () => new PonyglotRuntime(_translationStore, _cultureSource, _translationFactoryProvider, [], [_loader1]);

        // Assert
        action.Should().ThrowExactly<ArgumentException>().WithMessage("The collection of locators is empty.*");
    }

    [Fact]
    public void Constructor_LocatorsContainsANullItem_Throws()
    {
        // Arrange
        var locators = new[]
        {
            Substitute.For<ICatalogLocator>(),
            Substitute.For<ICatalogLocator>(),
            Substitute.For<ICatalogLocator>(),
            null!,
            Substitute.For<ICatalogLocator>(),
        };

        // Act
        var action = () => new PonyglotRuntime(_translationStore, _cultureSource, _translationFactoryProvider, locators, [_loader1]);

        // Assert
        action.Should().ThrowExactly<ArgumentException>().WithMessage("The locator at index 3 is null.*");
    }

    [Fact]
    public void Constructor_LoadersIsEmpty_Throws()
    {
        // Arrange

        // Act
        var action = () => new PonyglotRuntime(_translationStore, _cultureSource, _translationFactoryProvider, [_locator1], []);

        // Assert
        action.Should().ThrowExactly<ArgumentException>().WithMessage("The collection of loaders is empty.*");
    }

    [Fact]
    public void Constructor_LoadersContainsANullItem_Throws()
    {
        // Arrange
        var loaders = new[]
        {
            Substitute.For<ICatalogLoader>(),
            Substitute.For<ICatalogLoader>(),
            Substitute.For<ICatalogLoader>(),
            null!,
            Substitute.For<ICatalogLoader>(),
        };

        // Act
        var action = () => new PonyglotRuntime(_translationStore, _cultureSource, _translationFactoryProvider, [_locator1], loaders);

        // Assert
        action.Should().ThrowExactly<ArgumentException>().WithMessage("The loader at index 3 is null.*");
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
    public async Task IsInitialized_InitializationInProgress_ReturnsTrue()
    {
        // Arrange
        var waitLock = new SemaphoreSlim(0, 1);

        _sut.Configure().FindCatalogsAsync_(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(async ct =>
        {
            await waitLock.WaitAsync(ct.Arg<CancellationToken>());
            return [];
        });
        _sut.Configure().LoadCatalogsAsync_(default!, Arg.Any<CancellationToken>()).ReturnsForAnyArgs([]);

        var pendingInitTask = _sut.InitializeAsync(TestContext.Current.CancellationToken);

        // Act
        var isInitialized = _sut.IsInitialized;

        // Assert
        try
        {
            isInitialized.Should().BeFalse();
        }
        finally
        {
            waitLock.Release();
            await pendingInitTask;
        }
    }

    [Fact]
    public async Task IsInitialized_InitializationFailed_ReturnsFalse()
    {
        // Arrange
        _sut.Configure().FindCatalogsAsync_(Arg.Any<CancellationToken>()).ThrowsAsyncForAnyArgs<Exception>();
        _sut.Configure().LoadCatalogsAsync_(default!, Arg.Any<CancellationToken>()).ReturnsForAnyArgs([]);
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
        _sut.Configure().FindCatalogsAsync_(Arg.Any<CancellationToken>()).ReturnsForAnyArgs([]);
        _sut.Configure().LoadCatalogsAsync_(default!, Arg.Any<CancellationToken>()).ReturnsForAnyArgs([]);
        await _sut.InitializeAsync(CancellationToken.None);

        // Act
        var isInitialized = _sut.IsInitialized;

        // Assert
        isInitialized.Should().BeTrue();
    }

    [Fact]
    public async Task Store_Initialized_ReturnsTheCreatedInstance()
    {
        // Arrange
        _sut.Configure().FindCatalogsAsync_(Arg.Any<CancellationToken>()).ReturnsForAnyArgs([]);
        _sut.Configure().LoadCatalogsAsync_(default!, Arg.Any<CancellationToken>()).ReturnsForAnyArgs([]);
        await _sut.InitializeAsync(CancellationToken.None);

        // Act
        var store = _sut.Store;

        // Assert
        store.Should().BeSameAs(_translationStore);
    }

    [Fact]
    public void Store_NotInitialized_Throws()
    {
        // Arrange

        // Act
        var action = () => _sut.Store;

        // Assert
        action.Should().ThrowExactly<InvalidOperationException>().WithMessage("The runtime has not been initialized. Has InitializeAsync been called?");
    }

    [Fact]
    public async Task CultureSource_Initialized_ReturnsTheCreatedInstance()
    {
        // Arrange
        _sut.Configure().FindCatalogsAsync_(Arg.Any<CancellationToken>()).ReturnsForAnyArgs([]);
        _sut.Configure().LoadCatalogsAsync_(default!, Arg.Any<CancellationToken>()).ReturnsForAnyArgs([]);
        await _sut.InitializeAsync(CancellationToken.None);

        // Act
        var cultureSource = _sut.CultureSource;

        // Assert
        cultureSource.Should().BeSameAs(_cultureSource);
    }

    [Fact]
    public void CultureSource_NotInitialized_Throws()
    {
        // Arrange

        // Act
        var action = () => _sut.CultureSource;

        // Assert
        action.Should().ThrowExactly<InvalidOperationException>().WithMessage("The runtime has not been initialized. Has InitializeAsync been called?");
    }

    [Fact]
    public async Task TranslatorFactory_Initialized_ReturnsTheCreatedInstance()
    {
        // Arrange
        _sut.Configure().FindCatalogsAsync_(Arg.Any<CancellationToken>()).ReturnsForAnyArgs([]);
        _sut.Configure().LoadCatalogsAsync_(default!, Arg.Any<CancellationToken>()).ReturnsForAnyArgs([]);
        await _sut.InitializeAsync(CancellationToken.None);

        // Act
        var translatorFactory = _sut.TranslatorFactory;

        // Assert
        translatorFactory.Should().BeSameAs(_translatorFactory);
    }

    [Fact]
    public void TranslatorFactory_NotInitialized_Throws()
    {
        // Arrange

        // Act
        var action = () => _sut.TranslatorFactory;

        // Assert
        action.Should().ThrowExactly<InvalidOperationException>().WithMessage("The runtime has not been initialized. Has InitializeAsync been called?");
    }

    [Fact]
    public async Task InitializeAsync_NotInitialized_LocatesTheCatalogs()
    {
        // Arrange
        _sut.Configure().FindCatalogsAsync_(Arg.Any<CancellationToken>()).ReturnsForAnyArgs([]);
        _sut.Configure().LoadCatalogsAsync_(default!, Arg.Any<CancellationToken>()).ReturnsForAnyArgs([]);

        // Act
        await _sut.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        await _sut.Received(1).FindCatalogsAsync_(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task InitializeAsync_NotInitialized_LoadsTheResources()
    {
        // Arrange
        var resources = new[] { CreateCatalogResource("test://my-resource") };

        _sut.Configure().FindCatalogsAsync_(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(resources);
        _sut.Configure().LoadCatalogsAsync_(default!, Arg.Any<CancellationToken>()).ReturnsForAnyArgs([]);

        // Act
        await _sut.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        await _sut.Received(1).LoadCatalogsAsync_(resources, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task InitializeAsync_NotInitialized_InitializeTheStore()
    {
        // Arrange
        var catalogs = new[] { CreateCatalog() };

        _sut.Configure().FindCatalogsAsync_(Arg.Any<CancellationToken>()).ReturnsForAnyArgs([]);
        _sut.Configure().LoadCatalogsAsync_(default!, Arg.Any<CancellationToken>()).ReturnsForAnyArgs(catalogs);

        // Act
        await _sut.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        _translationStore.Received(1).Initialize(catalogs);
    }

    [Fact]
    public async Task InitializeAsync_InitializationInProgress_Throws()
    {
        // Arrange
        var waitLock = new SemaphoreSlim(0, 1);

        _sut.Configure().FindCatalogsAsync_(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(async ct =>
        {
            await waitLock.WaitAsync(ct.Arg<CancellationToken>());
            return [];
        });
        _sut.Configure().LoadCatalogsAsync_(default!, Arg.Any<CancellationToken>()).ReturnsForAnyArgs([]);

        var pendingInitTask = _sut.InitializeAsync(TestContext.Current.CancellationToken);

        // Act
        var action = () => _sut.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        try
        {
            await action.Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage("The runtime is already initializing.");
        }
        finally
        {
            waitLock.Release();
            await pendingInitTask;
        }
    }

    [Fact]
    public async Task InitializeAsync_PreviousInitializationFailed_InitializeAgain()
    {
        // Arrange
        _sut.Configure().FindCatalogsAsync_(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(_ => throw new Exception("💥Kaboom💥"), _ => []);
        _sut.Configure().LoadCatalogsAsync_(default!, Arg.Any<CancellationToken>()).ReturnsForAnyArgs([]);

        await Assert.ThrowsAnyAsync<Exception>(() => _sut.InitializeAsync(TestContext.Current.CancellationToken));

        _sut.ClearReceivedCalls();

        // Act
        await _sut.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        await _sut.ReceivedWithAnyArgs(1).FindCatalogsAsync_(Arg.Any<CancellationToken>());
        await _sut.ReceivedWithAnyArgs(1).LoadCatalogsAsync_(default!, Arg.Any<CancellationToken>());
        _translationStore.ReceivedWithAnyArgs(1).Initialize(default!);
    }

    [Fact]
    public async Task InitializeAsync_AlreadyInitialized_Throws()
    {
        // Arrange
        _sut.Configure().FindCatalogsAsync_(Arg.Any<CancellationToken>()).ReturnsForAnyArgs([]);
        _sut.Configure().LoadCatalogsAsync_(default!, Arg.Any<CancellationToken>()).ReturnsForAnyArgs([]);
        await _sut.InitializeAsync(CancellationToken.None);

        _sut.ClearReceivedCalls();
        _translationStore.ClearReceivedCalls();

        // Act
        var action = () => _sut.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        await action.Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage("The runtime is already initialized.");

        await _sut.DidNotReceiveWithAnyArgs().FindCatalogsAsync_(Arg.Any<CancellationToken>());
        await _sut.DidNotReceiveWithAnyArgs().LoadCatalogsAsync_(default!, Arg.Any<CancellationToken>());
        _translationStore.DidNotReceiveWithAnyArgs().Initialize(default!);
    }

    [Theory]
    [CombinatorialData]
    public async Task InitializeAsync_InitializationFails_Throws(
        [CombinatorialValues("Find catalogs", "Load catalogs", "Store initialization")] string failurePoint,
        bool isCancellationException)
    {
        // Arrange
        _sut.Configure().FindCatalogsAsync_(Arg.Any<CancellationToken>()).ReturnsForAnyArgs([]);
        _sut.Configure().LoadCatalogsAsync_(default!, Arg.Any<CancellationToken>()).ReturnsForAnyArgs([]);

        Exception error = isCancellationException ? new OperationCanceledException() : new InvalidOperationException("💥Kaboom💥");
        switch (failurePoint)
        {
            case "Find catalogs":
                _sut.Configure().FindCatalogsAsync_(Arg.Any<CancellationToken>()).ThrowsAsyncForAnyArgs(error);
                break;
            case "Load catalogs":
                _sut.Configure().LoadCatalogsAsync_(default!, Arg.Any<CancellationToken>()).ThrowsAsyncForAnyArgs(error);
                break;
            case "Store initialization":
                _translationStore.WhenForAnyArgs(o => o.Initialize(default!)).Throw(error);
                break;
            default:
                throw new NotSupportedException($"Unexpected failure point: {failurePoint}");
        }

        // Act
        var action = () => _sut.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        if (isCancellationException)
            (await action.Should().ThrowExactlyAsync<OperationCanceledException>()).Which.Should().BeSameAs(error);
        else
            (await action.Should().ThrowExactlyAsync<InvalidOperationException>()).Which.Should().BeSameAs(error);
    }

    [Fact]
    public async Task FindCatalogsAsync_NoDuplicates_ReturnsResourcesInCorrectOrder()
    {
        // Arrange
        var resources = new[]
        {
            CreateCatalogResource("test://my-resource-a"),
            CreateCatalogResource("test://my-resource-c"),
            CreateCatalogResource("test://my-resource-b"),
            CreateCatalogResource("test://my-resource-d"),
        };

        _locator1.FindCatalogsAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(resources[..2]);
        _locator2.FindCatalogsAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(resources[2..]);

        // Act
        var result = await _sut.FindCatalogsAsync_(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo(resources);
    }

    [Fact]
    public async Task FindCatalogsAsync_OneDuplicate_ReturnsResourcesInCorrectOrder()
    {
        // Arrange
        var resources = new[]
        {
            CreateCatalogResource("test://my-resource-a"),
            CreateCatalogResource("test://my-resource-b"),
            CreateCatalogResource("test://my-resource-b"),
            CreateCatalogResource("test://my-resource-c"),
        };

        _locator1.FindCatalogsAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(resources[..2]);
        _locator2.FindCatalogsAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(resources[2..]);

        // Act
        var result = await _sut.FindCatalogsAsync_(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo(resources);
    }

    [Fact]
    public async Task FindCatalogsAsync_OneLocatorReturnsEmptyList_ReturnsResourcesInCorrectOrder()
    {
        // Arrange
        var resources = new[]
        {
            CreateCatalogResource("test://my-resource-a"),
            CreateCatalogResource("test://my-resource-b"),
        };

        _locator1.FindCatalogsAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs([]);
        _locator2.FindCatalogsAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(resources);

        // Act
        var result = await _sut.FindCatalogsAsync_(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo(resources);
    }

    [Fact]
    public async Task FindCatalogsAsync_OneLocatorReturnsANullResourceList_ReturnsResourcesInCorrectOrder()
    {
        // Arrange
        var resources = new[]
        {
            CreateCatalogResource("test://my-resource-a"),
            CreateCatalogResource("test://my-resource-b"),
        };

        _locator1.FindCatalogsAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs((IReadOnlyCollection<CatalogResource>)null!);
        _locator2.FindCatalogsAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(resources);

        // Act
        var result = await _sut.FindCatalogsAsync_(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo(resources);
    }

    [Fact]
    public async Task FindCatalogsAsync_OneLocatorReturnsAResourceListWithANullItem_ReturnsResourcesInCorrectOrder()
    {
        // Arrange
        var resources = new[]
        {
            CreateCatalogResource("test://my-resource-a"),
            CreateCatalogResource("test://my-resource-b"),
            CreateCatalogResource("test://my-resource-b"),
            CreateCatalogResource("test://my-resource-c"),
        };

        _locator1.FindCatalogsAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs([resources[0]]);
        _locator2.FindCatalogsAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs([
            resources[1],
            resources[2],
            null!,
            resources[3],
        ]);

        // Act
        var result = await _sut.FindCatalogsAsync_(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo(resources);
    }

    [Fact]
    public async Task FindCatalogsAsync_CancellationOccurs_Throws()
    {
        // Arrange
        var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        _locator1.WhenForAnyArgs(o => o.FindCatalogsAsync(Arg.Any<CancellationToken>())).Do(_ => cts.Cancel());
        _locator2.WhenForAnyArgs(o => o.FindCatalogsAsync(Arg.Any<CancellationToken>())).Do(_ => cts.Token.ThrowIfCancellationRequested());

        // Act
        var action = () => _sut.FindCatalogsAsync_(cts.Token);

        // Assert
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task FindCatalogsAsync_LocatorFails_Throws()
    {
        // Arrange
        var error = new Exception("💥Kaboom💥");
        _locator1.FindCatalogsAsync(Arg.Any<CancellationToken>()).ThrowsAsyncForAnyArgs(error);

        // Act
        var action = () => _sut.FindCatalogsAsync_(TestContext.Current.CancellationToken);

        // Assert
        (await action.Should().ThrowExactlyAsync<Exception>()).Which.Should().BeSameAs(error);
    }

    [Fact]
    public async Task LoadCatalogsAsync_TwoResources_SelectsTheLoaderForEachResource()
    {
        // Arrange
        _sut.Configure().GetLoaderAsync_(default!, Arg.Any<CancellationToken>()).ReturnsForAnyArgs(_loader1);

        var resources = new[]
        {
            CreateCatalogResource("test://my-resource-a"),
            CreateCatalogResource("test://my-resource-b"),
        };

        // Act
        await _sut.LoadCatalogsAsync_(resources, TestContext.Current.CancellationToken);

        // Assert
        await _sut.ReceivedWithAnyArgs(2).GetLoaderAsync_(default!, Arg.Any<CancellationToken>());
        await _sut.Received(1).GetLoaderAsync_(resources[0], Arg.Any<CancellationToken>());
        await _sut.Received(1).GetLoaderAsync_(resources[1], Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadCatalogsAsync_TwoResources_LoadsEachResourceWithTheSelectedLoader()
    {
        // Arrange
        var resources = new[]
        {
            CreateCatalogResource("test://my-resource-a"),
            CreateCatalogResource("test://my-resource-b"),
        };

        _sut.Configure().GetLoaderAsync_(resources[0], Arg.Any<CancellationToken>()).Returns(_loader1);
        _sut.Configure().GetLoaderAsync_(resources[1], Arg.Any<CancellationToken>()).Returns(_loader2);

        // Act
        await _sut.LoadCatalogsAsync_(resources, TestContext.Current.CancellationToken);

        // Assert
        await _loader1.Received(1).LoadAsync(resources[0], Arg.Any<CancellationToken>());
        await _loader2.Received(1).LoadAsync(resources[1], Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadCatalogsAsync_FourResources_ReturnsTheCatalogsInCorrectOrder()
    {
        // Arrange
        var resources = new[]
        {
            CreateCatalogResource("test://my-resource-a"),
            CreateCatalogResource("test://my-resource-c"),
            CreateCatalogResource("test://my-resource-b"),
            CreateCatalogResource("test://my-resource-d"),
        };

        _sut.Configure().GetLoaderAsync_(resources[0], Arg.Any<CancellationToken>()).Returns(_loader1);
        _sut.Configure().GetLoaderAsync_(resources[1], Arg.Any<CancellationToken>()).Returns(_loader1);
        _sut.Configure().GetLoaderAsync_(resources[2], Arg.Any<CancellationToken>()).Returns(_loader2);
        _sut.Configure().GetLoaderAsync_(resources[3], Arg.Any<CancellationToken>()).Returns(_loader2);

        var catalogs = new[]
        {
            CreateCatalog("test://my-catalog-a"),
            CreateCatalog("test://my-catalog-c"),
            CreateCatalog("test://my-catalog-b"),
            CreateCatalog("test://my-catalog-d"),
        };

        _loader1.LoadAsync(resources[0], Arg.Any<CancellationToken>()).Returns(catalogs[0]);
        _loader1.LoadAsync(resources[1], Arg.Any<CancellationToken>()).Returns(catalogs[1]);
        _loader2.LoadAsync(resources[2], Arg.Any<CancellationToken>()).Returns(catalogs[2]);
        _loader2.LoadAsync(resources[3], Arg.Any<CancellationToken>()).Returns(catalogs[3]);

        // Act
        var result = await _sut.LoadCatalogsAsync_(resources, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEquivalentTo(catalogs);
    }

    [Fact]
    public async Task LoadCatalogsAsync_EmptyResourceCollection_ReturnsEmptyCollection()
    {
        // Arrange

        // Act
        var result = await _sut.LoadCatalogsAsync_([], TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadCatalogsAsync_ResourcesCollectionIsNull_Throws()
    {
        // Arrange

        // Act
        var action = () => _sut.LoadCatalogsAsync_(null!, TestContext.Current.CancellationToken);

        // Assert
        await action.Should().ThrowExactlyAsync<ArgumentNullException>().WithParameterName("resources");
    }

    [Fact]
    public async Task LoadCatalogsAsync_SelectedLoaderIsNull_Throws()
    {
        // Arrange
        var resources = new[] { CreateCatalogResource("test://my-resource") };

        _sut.Configure().GetLoaderAsync_(default!, Arg.Any<CancellationToken>()).ReturnsForAnyArgs((ICatalogLoader)null!);

        // Act
        var action = () => _sut.LoadCatalogsAsync_(resources, TestContext.Current.CancellationToken);

        // Assert
        await action.Should().ThrowExactlyAsync<InvalidOperationException>()
            .WithMessage("The loader selected for the resource 'test://my-resource/' is null.");
    }

    [Fact]
    public async Task LoadCatalogsAsync_SelectedLoaderReturnsNullCatalog_Throws()
    {
        // Arrange
        var resources = new[] { CreateCatalogResource("test://my-resource") };

        _sut.Configure().GetLoaderAsync_(default!, Arg.Any<CancellationToken>()).ReturnsForAnyArgs(_loader1);
        _loader1.LoadAsync(default!, Arg.Any<CancellationToken>()).ReturnsForAnyArgs((Catalog)null!);

        // Act
        var action = () => _sut.LoadCatalogsAsync_(resources, TestContext.Current.CancellationToken);

        // Assert
        await action.Should().ThrowExactlyAsync<InvalidOperationException>()
            .WithMessage($"The loader ({_loader1}) returned a null catalog for the resource 'test://my-resource/'.");
    }

    [Fact]
    public async Task LoadCatalogsAsync_CancellationOccurs_Throws()
    {
        // Arrange
        var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        var resources = new[]
        {
            CreateCatalogResource("test://my-resource-a"),
            CreateCatalogResource("test://my-resource-b"),
        };

        _sut.Configure().GetLoaderAsync_(default!, Arg.Any<CancellationToken>()).ReturnsForAnyArgs(_loader1);

        _loader1.WhenForAnyArgs(o => o.LoadAsync(default!, Arg.Any<CancellationToken>())).Do(_ => cts.Cancel());
        _loader1.WhenForAnyArgs(o => o.LoadAsync(default!, Arg.Any<CancellationToken>())).Do(_ => cts.Token.ThrowIfCancellationRequested());

        // Act
        var action = () => _sut.LoadCatalogsAsync_(resources, cts.Token);

        // Assert
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task LoadCatalogsAsync_LoaderSelectionFails_Throws()
    {
        // Arrange
        var resources = new[] { CreateCatalogResource("test://my-resource-a") };

        var error = new Exception("💥Kaboom💥");
        _sut.Configure().GetLoaderAsync_(default!, Arg.Any<CancellationToken>()).ThrowsAsyncForAnyArgs(error);

        // Act
        var action = () => _sut.LoadCatalogsAsync_(resources, TestContext.Current.CancellationToken);

        // Assert
        (await action.Should().ThrowExactlyAsync<Exception>()).Which.Should().BeSameAs(error);
    }

    [Fact]
    public async Task LoadCatalogsAsync_LoaderFails_Throws()
    {
        // Arrange
        var resources = new[] { CreateCatalogResource("test://my-resource-a") };

        _sut.Configure().GetLoaderAsync_(default!, Arg.Any<CancellationToken>()).ReturnsForAnyArgs(_loader1);

        var error = new Exception("💥Kaboom💥");
        _loader1.LoadAsync(default!, Arg.Any<CancellationToken>()).ThrowsAsyncForAnyArgs(error);

        // Act
        var action = () => _sut.LoadCatalogsAsync_(resources, TestContext.Current.CancellationToken);

        // Assert
        (await action.Should().ThrowExactlyAsync<Exception>()).Which.Should().BeSameAs(error);
    }

    [Fact]
    public async Task GetLoaderAsync_OneLoaderFound_EvaluatesEachLoaderAndReturnsTheSelectedLoader()
    {
        // Arrange
        var resource = CreateCatalogResource();

        _loader1.CanLoadAsync(default!, Arg.Any<CancellationToken>()).ReturnsForAnyArgs(true);
        _loader2.CanLoadAsync(default!, Arg.Any<CancellationToken>()).ReturnsForAnyArgs(false);

        // Act
        var result = await _sut.GetLoaderAsync_(resource, TestContext.Current.CancellationToken);

        // Assert
        await _loader1.Received(1).CanLoadAsync(resource, TestContext.Current.CancellationToken);
        await _loader2.Received(1).CanLoadAsync(resource, TestContext.Current.CancellationToken);
        result.Should().BeSameAs(_loader1);
    }

    [Fact]
    public async Task GetLoaderAsync_ResourceIsNull_Throws()
    {
        // Arrange

        // Act
        var action = () => _sut.GetLoaderAsync_(null!, TestContext.Current.CancellationToken);

        // Assert
        await action.Should().ThrowExactlyAsync<ArgumentNullException>().WithParameterName("resource");
    }

    [Fact]
    public async Task GetLoaderAsync_NoLoaderFound_Throws()
    {
        // Arrange
        var resource = CreateCatalogResource();

        _loader1.CanLoadAsync(default!, Arg.Any<CancellationToken>()).ReturnsForAnyArgs(false);
        _loader2.CanLoadAsync(default!, Arg.Any<CancellationToken>()).ReturnsForAnyArgs(false);

        // Act
        var action = () => _sut.GetLoaderAsync_(resource, TestContext.Current.CancellationToken);

        // Assert
        await action.Should().ThrowExactlyAsync<InvalidOperationException>()
            .WithMessage($"No catalog loader found to load the resource '{resource}'. Registered loaders are '{_loader1}', '{_loader2}'.");
    }

    [Fact]
    public async Task GetLoaderAsync_MoreThanOneLoaderFound_Throws()
    {
        // Arrange
        var resource = CreateCatalogResource();

        _loader1.CanLoadAsync(default!, Arg.Any<CancellationToken>()).ReturnsForAnyArgs(true);
        _loader2.CanLoadAsync(default!, Arg.Any<CancellationToken>()).ReturnsForAnyArgs(true);

        // Act
        var action = () => _sut.GetLoaderAsync_(resource, TestContext.Current.CancellationToken);

        // Assert
        await action.Should().ThrowExactlyAsync<InvalidOperationException>()
            .WithMessage($"Multiple catalog loaders found to load the resource '{resource}': '{_loader1}', '{_loader2}'.");
    }

    [Fact]
    public async Task GetLoaderAsync_CanLoadFails_Throws()
    {
        // Arrange
        var resource = CreateCatalogResource();

        var error = new Exception("💥Kaboom💥");

        _loader1.CanLoadAsync(default!, Arg.Any<CancellationToken>()).ThrowsAsyncForAnyArgs(error);

        // Act
        var action = () => _sut.GetLoaderAsync_(resource, TestContext.Current.CancellationToken);

        // Assert
        (await action.Should().ThrowExactlyAsync<Exception>())
            .Which.Should().BeSameAs(error);
    }

    #region Helpers

    // ReSharper disable all

    private static Catalog CreateCatalog(string? uri = null)
    {
        var pluralRule = Substitute.For<IPluralRule>();
        pluralRule.PluralCount.Returns(1);
        pluralRule.GetPluralForm(default).ReturnsForAnyArgs(0);

        return new Catalog(new Uri(uri ?? "test://my-domain/qps-Ploc"), "my-domain", new CultureInfo("qps-Ploc"), pluralRule, []);
    }

    private static CatalogResource CreateCatalogResource(string? uri = null)
    {
        return Substitute.For<CatalogResource>(new Uri(uri ?? "test://default-resource"));
    }

    internal class PonyglotRuntimeDouble : PonyglotRuntime
    {
        public PonyglotRuntimeDouble(
            TranslationStore translationStore,
            ICultureSource cultureSource,
            Func<TranslationStore, ICultureSource, ITranslatorFactory> translationFactoryProvider,
            IReadOnlyList<ICatalogLocator> locators,
            IReadOnlyList<ICatalogLoader> loaders)
            : base(translationStore, cultureSource, translationFactoryProvider, locators, loaders)
        {
        }

        public virtual Task<IReadOnlyList<CatalogResource>> FindCatalogsAsync_(CancellationToken cancellationToken) => base.FindCatalogsAsync(cancellationToken);
        protected sealed override Task<IReadOnlyList<CatalogResource>> FindCatalogsAsync(CancellationToken cancellationToken) => FindCatalogsAsync_(cancellationToken);

        public virtual Task<IReadOnlyList<Catalog>> LoadCatalogsAsync_(IReadOnlyList<CatalogResource> resources, CancellationToken cancellationToken)
            => base.LoadCatalogsAsync(resources, cancellationToken);

        protected sealed override Task<IReadOnlyList<Catalog>> LoadCatalogsAsync(IReadOnlyList<CatalogResource> resources, CancellationToken cancellationToken)
            => LoadCatalogsAsync_(resources, cancellationToken);

        public virtual Task<ICatalogLoader> GetLoaderAsync_(CatalogResource resource, CancellationToken cancellationToken) => base.GetLoaderAsync(resource, cancellationToken);
        protected sealed override Task<ICatalogLoader> GetLoaderAsync(CatalogResource resource, CancellationToken cancellationToken) => GetLoaderAsync_(resource, cancellationToken);
    }

    #endregion
}