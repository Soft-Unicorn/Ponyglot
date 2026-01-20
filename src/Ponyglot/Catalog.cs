using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Ponyglot;

/// <summary>
/// A loaded translation catalog that can provide translations for a specific culture.
/// </summary>
public class Catalog
{
    private readonly IPluralRule _pluralRule;
    private readonly FrozenDictionary<string, FrozenDictionary<string, IReadOnlyList<TranslationForm>>> _translationsIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="Catalog"/> class.
    /// </summary>
    /// <param name="uid">The id that uniquely identifies the source the catalog was loaded from.</param>
    /// <param name="catalogName">The catalog name the translations belong to.</param>
    /// <param name="culture">The culture of the translations.</param>
    /// <param name="pluralRule">The plural rule used to select plural forms.</param>
    /// <param name="entries">The collection of <see cref="MessageEntry"/> to load into this catalog.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="uid"/>
    /// -or-
    /// <paramref name="catalogName"/>
    /// -or-
    /// <paramref name="culture"/>
    /// -or-
    /// <paramref name="pluralRule"/>
    /// -or-
    /// <paramref name="entries"/>
    /// is <c>null</c>
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="pluralRule"/> produces invalid values or <paramref name="entries"/> contains a duplicate or invalid entry.</exception>
    public Catalog(string uid, string catalogName, CultureInfo culture, IPluralRule pluralRule, IEnumerable<MessageEntry> entries)
    {
        Uid = uid ?? throw new ArgumentNullException(nameof(uid));
        CatalogName = catalogName ?? throw new ArgumentNullException(nameof(catalogName));
        Culture = culture ?? throw new ArgumentNullException(nameof(culture));
        _pluralRule = pluralRule ?? throw new ArgumentNullException(nameof(pluralRule));
        ArgumentNullException.ThrowIfNull(entries);

        // Validate the plural rule 
        if (!ValidatePluralRule(pluralRule, out var pluralRuleError))
        {
            throw new ArgumentException(pluralRuleError, nameof(pluralRule));
        }

        // Process the entries
        var tempIndex = new Dictionary<string, Dictionary<string, IReadOnlyList<TranslationForm>>>(StringComparer.Ordinal);
        var entryIndex = 0;
        foreach (var entry in entries)
        {
            // Validate
            if (!ValidateEntry(entry, entryIndex++, pluralRule, uid, out var entryError))
            {
                throw new ArgumentException(entryError, nameof(entries));
            }

            // Add to the temporary index
            if (!tempIndex.TryGetValue(entry.MessageId, out var contextIndex))
            {
                contextIndex = new Dictionary<string, IReadOnlyList<TranslationForm>>(StringComparer.Ordinal);
                tempIndex.Add(entry.MessageId, contextIndex);
            }

            if (!contextIndex.TryAdd(entry.Context, entry.Translations))
            {
                throw new ArgumentException($"Duplicate catalog entry for message '{entry.MessageId}' with context '{entry.Context}' in catalog '{uid}'.", nameof(entries));
            }
        }

        // Builds the final frozen index
        _translationsIndex = tempIndex
            .Select(e => new KeyValuePair<string, FrozenDictionary<string, IReadOnlyList<TranslationForm>>>(e.Key, e.Value.ToFrozenDictionary(e.Value.Comparer)))
            .ToFrozenDictionary(tempIndex.Comparer);
    }

    /// <summary>
    /// The id that uniquely identifies the source the catalog was loaded from.
    /// </summary>
    public virtual string Uid { get; }

    /// <summary>
    /// The name of the catalog.
    /// </summary>
    public virtual string CatalogName { get; }

    /// <summary>
    /// The culture of the translations.
    /// </summary>
    public virtual CultureInfo Culture { get; }

    /// <summary>
    /// Tries to find a translation.
    /// </summary>
    /// <param name="context">The context to use to discriminate similar translations.</param>
    /// <param name="count">For plural scenarios, the count used for plural selection; <c>null</c> for non-plural scenarios.</param>
    /// <param name="messageId">The identifier of the message to translate.</param>
    /// <param name="translation">When this method returns <c>true</c>, contains the found <see cref="TranslationForm"/>; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if a translation is found; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="context"/>
    /// -or-
    /// <paramref name="messageId"/>
    /// is <c>null</c>.
    /// </exception>
    public virtual bool TryGet(string context, long? count, string messageId, [NotNullWhen(true)] out TranslationForm? translation)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(messageId);

        if (_translationsIndex.TryGetValue(messageId, out var contextIndex) && contextIndex.TryGetValue(context, out var translations))
        {
            var index = count != null ? _pluralRule.GetPluralForm(count.Value) : 0;

            if (index >= 0 && index < translations.Count)
            {
                translation = translations[index];
                return true;
            }
        }

        translation = null;
        return false;
    }

    /// <summary>
    /// Validates the specified <paramref name="pluralRule"/>.
    /// </summary>
    /// <param name="pluralRule">The <see cref="IPluralRule"/> to validate.</param>
    /// <param name="errorMessage">When this method returns <c>false</c>, contains the error message; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the <paramref name="pluralRule"/> is valid; otherwise, <c>false</c>.</returns>
    private static bool ValidatePluralRule(IPluralRule pluralRule, [NotNullWhen(false)] out string? errorMessage)
    {
        // Test values chosen to exercise all known gettext plural rule families:
        // zero, singular, simple plural, modulo 10/100 rules, and Slavic 11–14 exceptions.
        long[] nValues = [0, 1, 2, 3, 4, 5, 10, 11, 12, 14, 20, 21, 22, 24, 100, 101, 102, 111, long.MaxValue];

        foreach (var n in nValues)
        {
            var formIndex = pluralRule.GetPluralForm(n);
            if (formIndex < 0 || formIndex >= pluralRule.PluralCount)
            {
                errorMessage = $"Plural rule {pluralRule} returned {formIndex} for count {n} (expected >= 0 and < {pluralRule.PluralCount}).";
                return false;
            }
        }

        errorMessage = null;
        return true;
    }

    /// <summary>
    /// Validates the specified <paramref name="entry"/>.
    /// </summary>
    /// <param name="entry">The <see cref="MessageEntry"/> to validate.</param>
    /// <param name="index">The zero-based index of the entry in the collection of entries.</param>
    /// <param name="pluralRule">The <see cref="IPluralRule"/> to use to validate the number of plural forms on plural entries.</param>
    /// <param name="uid">The source uid of the catalog used to produce meaningful error messages.</param>
    /// <param name="errorMessage">When this method returns <c>false</c>, contains the error message; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the <paramref name="entry"/> is valid; otherwise, <c>false</c>.</returns>
    private static bool ValidateEntry(MessageEntry? entry, int index, IPluralRule pluralRule, string uid, [NotNullWhen(false)] out string? errorMessage)
    {
        if (entry == null)
        {
            errorMessage = $"The catalog entry at index {index} is null.";
            return false;
        }

        // Validate that translations exist
        if (entry.Translations.Count == 0)
        {
            errorMessage = $"No translations defined for message '{entry.MessageId}' with context '{entry.Context}' in catalog '{uid}'.";
            return false;
        }

        // Validate plural count consistency
        if (entry.IsPlural && entry.Translations.Count != pluralRule.PluralCount)
        {
            var actualCount = entry.Translations.Count;
            var expectedCount = pluralRule.PluralCount;
            errorMessage = $"Plurals count ({actualCount}) mismatch for message '{entry.MessageId}' with context '{entry.Context}' in catalog '{uid}' (expected {expectedCount}).";
            return false;
        }

        // Validates translation format strings
        for (var i = 0; i < entry.Translations.Count; i++)
        {
            if (entry.Translations[i] is { IsCompositeFormat: true } translation)
            {
                try
                {
                    _ = CompositeFormat.Parse(translation.Message);
                }
                catch (FormatException exception)
                {
                    errorMessage = $"Invalid translation format string ({translation.Message}) " +
                                   $"at index {i} for message '{entry.MessageId}' with context '{entry.Context}' in catalog '{uid}': " +
                                   $"{exception.Message}";
                    return false;
                }
            }
        }

        errorMessage = null;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => Uid;
}