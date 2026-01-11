using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ponyglot.Sources;

namespace Ponyglot;

/// <summary>
/// Boots Ponyglot by locating and loading catalogs, then initializing the translation store.
/// </summary>
public class PonyglotRuntime
{
    private const int StateUninitialized = 0;
    private const int StateInitializing = 1;
    private const int StateInitialized = 2;

    private readonly ICatalogSource[] _sources;
    private volatile int _state;

    /// <summary>
    /// Initializes a new instance of the <see cref="PonyglotRuntime"/> class.
    /// </summary>
    /// <param name="translationStore">The <see cref="TranslationStore"/> that will be initialized with the loaded catalogs and provide the translations at runtime.</param>
    /// <param name="cultureSource">The <see cref="ICultureSource"/> to use as a single source of truth for the current culture.</param>
    /// <param name="translationFactoryProvider">
    /// The function that constructs an <see cref="ITranslatorFactory"/> using the supplied <paramref name="translationStore"/> and <paramref name="cultureSource"/>.
    /// </param>
    /// <param name="sources">The collection of <see cref="ICatalogSource"/> to use to get the translation catalogs.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="translationStore"/>
    /// -or-
    /// <paramref name="cultureSource"/>
    /// -or-
    /// <paramref name="translationFactoryProvider"/>
    /// -or-
    /// <paramref name="sources"/>
    /// is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="sources"/> is empty or contains a null item.
    /// </exception>
    public PonyglotRuntime(
        TranslationStore translationStore,
        ICultureSource cultureSource,
        Func<TranslationStore, ICultureSource, ITranslatorFactory> translationFactoryProvider,
        IEnumerable<ICatalogSource> sources)
    {
        ArgumentNullException.ThrowIfNull(translationStore);
        ArgumentNullException.ThrowIfNull(cultureSource);
        ArgumentNullException.ThrowIfNull(translationFactoryProvider);
        ArgumentNullException.ThrowIfNull(sources);

        Store = translationStore;
        CultureSource = cultureSource;
        TranslatorFactory = translationFactoryProvider(translationStore, cultureSource)
                            ?? throw new ArgumentException("The translation factory provider returned null.", nameof(translationFactoryProvider));

        _sources = sources.Select((l, i) => l != null ? l : throw new ArgumentException($"The catalog source at index {i} is null.", nameof(sources))).ToArray();
        if (_sources.Length == 0) throw new ArgumentException("The collection of catalog sources is empty.", nameof(sources));

        _state = StateUninitialized;
    }

    /// <summary>
    /// Gets whether the runtime has been initialized successfully.
    /// </summary>
    public bool IsInitialized => _state == StateInitialized;

    /// <summary>
    /// The <see cref="TranslationStore"/> that will be initialized with the loaded catalogs and provide the translations at runtime.
    /// </summary>
    public TranslationStore Store { get; }

    /// <summary>
    /// The <see cref="ICultureSource"/> to use as a single source of truth for the current culture.
    /// </summary>
    public ICultureSource CultureSource { get; }

    /// <summary>
    /// Gets the translator factory. Only available after successful initialization.
    /// </summary>
    public ITranslatorFactory TranslatorFactory { get; }

    /// <summary>
    /// Initializes the runtime by locating and loading catalogs, then initializing the translation store.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    /// The method is called more than once or concurrently.
    /// -or-
    /// An error occurs during the initialization.
    /// </exception>
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
            // Prepares the results for each source
            var sourceResults = new List<Catalog>[_sources.Length];

            // Discover & load the catalogs
            await Parallel.ForAsync(0, _sources.Length, cancellationToken, async (i, ct) =>
            {
                sourceResults[i] = [];
                await foreach (var catalog in _sources[i].LoadCatalogsAsync(ct).ConfigureAwait(false))
                {
                    if (catalog == null)
                    {
                        throw new InvalidOperationException($"The catalog source at index {i} ({_sources[i]}) returned a null catalog.");
                    }

                    sourceResults[i].Add(catalog);
                }
            }).ConfigureAwait(false);

            // Initialize the store
            var catalogs = sourceResults.SelectMany(r => r);
            Store.Initialize(catalogs);

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
}