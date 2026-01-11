using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ponyglot.Loading;

namespace Ponyglot;

/// <summary>
/// Boots Ponyglot by locating and loading catalogs, then initializing the translation store.
/// </summary>
public class PonyglotRuntime
{
    private const int StateUninitialized = 0;
    private const int StateInitializing = 1;
    private const int StateInitialized = 2;

    private readonly ICatalogLocator[] _locators;
    private readonly ICatalogLoader[] _loaders;
    private volatile int _state;
    private readonly TranslationStore _store;

    /// <summary>
    /// Initialize a new instance of the <see cref="PonyglotRuntime"/> class.
    /// </summary>
    /// <param name="translationStore">The <see cref="TranslationStore"/> that will be initialized with the loaded catalogs and provide the translations at runtime.</param>
    /// <param name="cultureSource">The <see cref="ICultureSource"/> to use as a single source of truth for the current culture.</param>
    /// <param name="translationFactoryProvider">
    /// The function that constructs an <see cref="ITranslatorFactory"/> using the supplied <paramref name="translationStore"/> and <paramref name="cultureSource"/>.
    /// </param>
    /// <param name="locators">The collection of <see cref="ICatalogLocator"/> to use to locate the translation resources to load using the <paramref name="loaders"/>.</param>
    /// <param name="loaders">The collection of <see cref="ICatalogLoader"/> to use to load the translation resources found by the <paramref name="locators"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="translationStore"/> or <paramref name="cultureSource"/> or <paramref name="translationFactoryProvider"/> or <paramref name="locators"/> or <paramref name="loaders"/>
    /// is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="locators"/> or <paramref name="loaders"/> is empty or contains a null item.
    /// </exception>
    public PonyglotRuntime(
        TranslationStore translationStore,
        ICultureSource cultureSource,
        Func<TranslationStore, ICultureSource, ITranslatorFactory> translationFactoryProvider,
        IEnumerable<ICatalogLocator> locators,
        IEnumerable<ICatalogLoader> loaders)
    {
        ArgumentNullException.ThrowIfNull(translationStore);
        ArgumentNullException.ThrowIfNull(cultureSource);
        ArgumentNullException.ThrowIfNull(translationFactoryProvider);
        ArgumentNullException.ThrowIfNull(locators);
        ArgumentNullException.ThrowIfNull(loaders);

        _store = translationStore;
        CultureSource = cultureSource;
        TranslatorFactory = translationFactoryProvider(translationStore, cultureSource)
                            ?? throw new ArgumentException("The translation factory provider returned null.", nameof(translationFactoryProvider));

        _locators = locators.Select((l, i) => l != null ? l : throw new ArgumentException($"The locator at index {i} is null.", nameof(locators))).ToArray();
        if (_locators.Length == 0) throw new ArgumentException("The collection of locators is empty.", nameof(locators));

        _loaders = loaders.Select((l, i) => l != null ? l : throw new ArgumentException($"The loader at index {i} is null.", nameof(loaders))).ToArray();
        if (_loaders.Length == 0) throw new ArgumentException("The collection of loaders is empty.", nameof(loaders));

        _state = StateUninitialized;
    }

    /// <summary>
    /// Gets whether the runtime has been initialized successfully.
    /// </summary>
    public bool IsInitialized => _state == StateInitialized;

    /// <summary>
    /// The <see cref="TranslationStore"/> that will be initialized with the loaded catalogs and provide the translations at runtime.
    /// </summary>
    /// <exception cref="InvalidOperationException">The runtime has not been initialized.</exception>
    public TranslationStore Store => EnsureInitialized(_store);

    /// <summary>
    /// The <see cref="ICultureSource"/> to use as a single source of truth for the current culture.
    /// </summary>
    /// <exception cref="InvalidOperationException">The runtime has not been initialized.</exception>
    public ICultureSource CultureSource => EnsureInitialized(field);

    /// <summary>
    /// Gets the translator factory. Only available after successful initialization.
    /// </summary>
    /// <exception cref="InvalidOperationException">The runtime has not been initialized.</exception>
    public ITranslatorFactory TranslatorFactory => EnsureInitialized(field);

    /// <summary>
    /// Initializes the runtime by locating and loading catalogs, then initializing the translation store.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="InvalidOperationException">The method is called more than once or concurrently.</exception>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Set state to initializing or fail if already initialized or initializing
        switch (Interlocked.CompareExchange(ref _state, StateInitializing, StateUninitialized))
        {
            case StateUninitialized:
                // OK
                break;
            case StateInitializing:
                throw new InvalidOperationException("The runtime is already initializing.");
            case StateInitialized:
                throw new InvalidOperationException("The runtime is already initialized.");
        }

        try
        {
            // Discover catalog resources
            var resources = await FindCatalogsAsync(cancellationToken).ConfigureAwait(false);

            // Load catalogs
            var catalogs = await LoadCatalogsAsync(resources, cancellationToken).ConfigureAwait(false);

            // Initialize the store
            _store.Initialize(catalogs);

            // Mark as initialized
            Interlocked.Exchange(ref _state, StateInitialized);
        }
        catch
        {
            // Reset state to uninitialized on errors
            Interlocked.Exchange(ref _state, StateUninitialized);
            throw;
        }
    }

    /// <summary>
    /// Finds the list of catalog resources to load.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>The collection of found <see cref="CatalogResource"/>.</returns>
    /// <remarks>The returned <see cref="CatalogResource"/> is the result of the concatenation of the resources returned by each <see cref="ICatalogLocator"/> in their order of registration.</remarks>
    protected virtual async Task<IReadOnlyList<CatalogResource>> FindCatalogsAsync(CancellationToken cancellationToken)
    {
        // Locate in parallel
        var locatorResults = new IReadOnlyCollection<CatalogResource>[_locators.Length];
        await Parallel.ForAsync(
            fromInclusive: 0,
            toExclusive: _locators.Length,
            cancellationToken: cancellationToken,
            body: async (i, ct) =>
            {
                var locator = _locators[i];

                // Search & Verify that the locator returned a non-null result
                locatorResults[i] = await locator.FindCatalogsAsync(ct).ConfigureAwait(false);
            }).ConfigureAwait(false);

        // Put back the results in the correct order and return them (be extra safe ignoring potential nulls)
        var result = locatorResults
            .Where(e => (IReadOnlyCollection<CatalogResource>?)e != null)
            .SelectMany(e => e)
            .Where(e => (CatalogResource?)e != null)
            .ToList();

        return result;
    }

    /// <summary>
    /// Loads the specified catalog <paramref name="resources"/> in parallel.
    /// </summary>
    /// <param name="resources">The collection of <see cref="CatalogResource"/> to load.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>The collection of <see cref="Catalog"/> loaded from the specified <paramref name="resources"/>.</returns>
    /// <remarks>The returned <see cref="Catalog"/> are in the same order that the <paramref name="resources"/> were provided.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="resources"/> is <c>null</c>.</exception>
    protected virtual async Task<IReadOnlyList<Catalog>> LoadCatalogsAsync(IReadOnlyList<CatalogResource> resources, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resources);

        // Loads in parallel
        var results = new Catalog[resources.Count];

        await Parallel.ForAsync(
            0,
            resources.Count,
            cancellationToken,
            async (i, ct) =>
            {
                var resource = resources[i];
                var loader = await GetLoaderAsync(resource, ct).ConfigureAwait(false)
                             ?? throw new InvalidOperationException($"The loader selected for the resource '{resource}' is null.");
                results[i] = await loader.LoadAsync(resource, ct).ConfigureAwait(false)
                             ?? throw new InvalidOperationException($"The loader ({loader}) returned a null catalog for the resource '{resource}'.");
            }).ConfigureAwait(false);

        // Return the results
        return results;
    }

    /// <summary>
    /// Finds the loader that can load the specified <paramref name="resource"/>.
    /// </summary>
    /// <param name="resource">The <see cref="CatalogResource"/> to load.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>The <see cref="ICatalogLoader"/> to use to load the specified <paramref name="resource"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="resource"/> is <c>null</c>.</exception>
    protected virtual async Task<ICatalogLoader> GetLoaderAsync(CatalogResource resource, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);

        var matchingLoaders = new List<ICatalogLoader>(_loaders.Length);
        foreach (var loader in _loaders)
        {
            if (await loader.CanLoadAsync(resource, cancellationToken).ConfigureAwait(false))
            {
                matchingLoaders.Add(loader);
            }
        }

        return matchingLoaders.Count switch
        {
            0 => throw new InvalidOperationException($"No catalog loader found to load the resource '{resource}'. Registered loaders are {string.Join(", ", _loaders.Select(l => $"'{l}'"))}."),
            1 => matchingLoaders[0],
            _ => throw new InvalidOperationException($"Multiple catalog loaders found to load the resource '{resource}': {string.Join(", ", matchingLoaders.Select(l => $"'{l}'"))}."),
        };
    }

    /// <summary>
    /// Ensures that the runtime has been initialized successfully and returns the specified <paramref name="value"/> if it has been.
    /// </summary>
    /// <param name="value">The value to return if the runtime has been initialized successfully.</param>
    /// <typeparam name="T">The <see cref="Type"/> of the value.</typeparam>
    /// <returns>The specified <paramref name="value"/>-</returns>
    /// <exception cref="InvalidOperationException">The runtime has not been initialized.</exception>
    private T EnsureInitialized<T>(T value)
    {
        return _state == StateInitialized ? value : throw new InvalidOperationException($"The runtime has not been initialized. Has {nameof(InitializeAsync)} been called?");
    }
}