#if !NETSTANDARD
using System.Collections.Frozen;
using System.Linq;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;

namespace Ponyglot;

/// <inheritdoc/>
public class TranslationStore : ITranslationStore
{
#if NETSTANDARD
    private Dictionary<string, Dictionary<string, ICatalog>>? _catalogsIndex;
#else
    private FrozenDictionary<string, FrozenDictionary<string, ICatalog>>? _catalogsIndex;
#endif

    /// <summary>
    /// Sets the catalogs that provide the translations.
    /// </summary>
    /// <param name="catalogs">The collection of <see cref="ICatalog"/>.</param>
    public void Initialize(IEnumerable<ICatalog> catalogs)
    {
        ArgumentNullException.ThrowIfNull(catalogs);

        var tempIndex = new Dictionary<string, Dictionary<string, ICatalog>>(StringComparer.OrdinalIgnoreCase);
        foreach (var catalog in catalogs)
        {
            if (!tempIndex.TryGetValue(catalog.Domain, out var cultureIndex))
            {
                cultureIndex = new Dictionary<string, ICatalog>(StringComparer.Ordinal);
                tempIndex[catalog.Domain] = cultureIndex;
            }

            if (!cultureIndex.TryAdd(catalog.Culture.Name, catalog))
            {
                throw new ArgumentException($"The list of catalogs contains more than one catalog for domain '{catalog.Domain}' and culture '{catalog.Culture.Name}'.", nameof(catalogs));
            }
        }

#if NETSTANDARD
        var catalogsIndex = tempIndex;
#else
        var catalogsIndex = tempIndex.Select(e => KeyValuePair.Create(e.Key, e.Value.ToFrozenDictionary())).ToFrozenDictionary();
#endif

        if (Interlocked.CompareExchange(ref _catalogsIndex, catalogsIndex, null) != null)
        {
            throw new InvalidOperationException("The translation store has already been initialized.");
        }
    }

    /// <inheritdoc/>
    public bool TryGet(string domain, CultureInfo culture, string context, string messageId, [NotNullWhen(true)] out string? translation)
    {
        ArgumentNullException.ThrowIfNull(domain);
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(messageId);

        foreach (var catalog in GetCatalogs(domain, culture))
        {
            if (catalog.TryGet(context, messageId, out translation))
            {
                return true;
            }
        }

        translation = null;
        return false;
    }

    /// <inheritdoc/>
    public bool TryGetPlural(string domain, CultureInfo culture, string context, long count, string messageId, [NotNullWhen(true)] out string? translation)
    {
        ArgumentNullException.ThrowIfNull(domain);
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(messageId);

        foreach (var catalog in GetCatalogs(domain, culture))
        {
            if (catalog.TryGetPlural(context, count, messageId, out translation))
            {
                return true;
            }
        }

        translation = null;
        return false;
    }

    private IEnumerable<ICatalog> GetCatalogs(string domain, CultureInfo culture)
    {
        if (_catalogsIndex == null)
        {
            throw new InvalidOperationException($"The translation store has not been initialized. The `{nameof(Initialize)}` method should be called before attempting to get translations.");
        }

        if (_catalogsIndex.TryGetValue(domain, out var cultureIndex))
        {
            // Lookup for culture with fallback to parent culture
            do
            {
                if (cultureIndex.TryGetValue(culture.Name, out var catalog))
                {
                    yield return catalog;
                }

                culture = culture.Parent;
            } while (!CultureInfo.InvariantCulture.Equals(culture));
        }
    }
}