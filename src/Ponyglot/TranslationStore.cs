using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Ponyglot;

/// <summary>
/// The store that provides the translations.
/// </summary>
[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global", Justification = "Library design allows for extensibility")]
public class TranslationStore
{
    private FrozenDictionary<string, FrozenDictionary<string, Catalog>>? _catalogsIndex;

    /// <summary>
    /// Sets the catalogs that provide the translations.
    /// </summary>
    /// <param name="catalogs">The collection of <see cref="Catalog"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="catalogs"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="catalogs"/> contains more than one catalog with the same name and culture or an invalid catalog.</exception>
    public virtual void Initialize(IEnumerable<Catalog> catalogs)
    {
        ArgumentNullException.ThrowIfNull(catalogs);

        var tempIndex = new Dictionary<string, Dictionary<string, Catalog>>(StringComparer.OrdinalIgnoreCase);
        var catalogIndex = 0;
        foreach (var catalog in catalogs)
        {
            // Validate
            if (catalog == null)
            {
                throw new ArgumentException($"The catalog at index {catalogIndex} is null.", nameof(catalogs));
            }

            // Find the culture index
            if (!tempIndex.TryGetValue(catalog.CatalogName, out var cultureIndex))
            {
                cultureIndex = new Dictionary<string, Catalog>(StringComparer.Ordinal);
                tempIndex[catalog.CatalogName] = cultureIndex;
            }

            // Add the catalog & check for duplicates
            if (!cultureIndex.TryAdd(catalog.Culture.Name, catalog))
            {
                var otherUid = cultureIndex[catalog.Culture.Name].Uid;
                var currentUid = catalog.Uid;
                throw new ArgumentException(
                    $"The list of catalogs contains more than one catalog with the name '{catalog.CatalogName}' and culture '{catalog.Culture.Name}': '{otherUid}' and '{currentUid}'.",
                    nameof(catalogs));
            }

            catalogIndex++;
        }

        var catalogsIndex = tempIndex
            .Select(e => new KeyValuePair<string, FrozenDictionary<string, Catalog>>(e.Key, e.Value.ToFrozenDictionary(e.Value.Comparer)))
            .ToFrozenDictionary(tempIndex.Comparer);

        if (Interlocked.CompareExchange(ref _catalogsIndex, catalogsIndex, null) != null)
        {
            throw new InvalidOperationException("The translation store has already been initialized.");
        }
    }

    /// <summary>
    /// Tries to find a translation.
    /// </summary>
    /// <param name="catalogName">The name that identifies the catalog to look up.</param>
    /// <param name="culture">The requested UI culture.</param>
    /// <param name="context">The context to use to discriminate similar translations.</param>
    /// <param name="count">For plural scenarios, the count used for plural selection; <c>null</c> for non-plural scenarios.</param>
    /// <param name="messageId">The identifier of the message to translate.</param>
    /// <param name="translation">When this method returns <c>true</c>, contains the found <see cref="TranslationForm"/>; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if a translation is found; otherwise, <c>false</c>.</returns>
    /// <remarks>If no translation is found for the specified <paramref name="culture"/> parent cultures are searched.</remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="catalogName"/>
    /// -or-
    /// <paramref name="culture"/>
    /// -or-
    /// <paramref name="context"/>
    /// -or-
    /// <paramref name="messageId"/>
    /// is <c>null</c>.
    /// </exception>
    public virtual bool TryGet(string catalogName, CultureInfo culture, string context, long? count, string messageId, [NotNullWhen(true)] out TranslationForm? translation)
    {
        ArgumentNullException.ThrowIfNull(catalogName);
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(messageId);

        if (_catalogsIndex == null)
        {
            throw new InvalidOperationException($"The translation store has not been initialized. The `{nameof(Initialize)}` method should be called before attempting to get translations.");
        }

        if (_catalogsIndex.TryGetValue(catalogName, out var cultureIndex))
        {
            // Lookup for culture with fallback to the parent culture, including the invariant culture
            while (true)
            {
                if (cultureIndex.TryGetValue(culture.Name, out var catalog) && catalog.TryGet(context, count, messageId, out translation))
                {
                    return true;
                }

                if (culture.Name.Length == 0)
                {
                    break; // Invariant culture has been looked up
                }

                // The next culture is the parent culture
                culture = culture.Parent;
            }
        }

        translation = null;
        return false;
    }
}