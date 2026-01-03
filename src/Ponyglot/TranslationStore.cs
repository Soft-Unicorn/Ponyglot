using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Ponyglot;

/// <summary>
/// The store that provides the translations.
/// </summary>
public class TranslationStore
{
    /// <summary>
    /// Tries to find a translation.
    /// </summary>
    /// <param name="domain">The domain that identifies the catalog to look up.</param>
    /// <param name="culture">The requested UI culture.</param>
    /// <param name="context">The context to use to discriminate similar translations.</param>
    /// <param name="count">For plural scenarios, the count used for plural selection; <c>null</c> for non-plural scenarios.</param>
    /// <param name="messageId">The identifier of the message to translate.</param>
    /// <param name="translation">When this method returns <c>true</c>, contains the found <see cref="TranslationForm"/>; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if a translation is found; otherwise, <c>false</c>.</returns>
    /// <remarks>If no translation is found for the specified <paramref name="culture"/> parent cultures are searched.</remarks>
    public virtual bool TryGet(string domain, CultureInfo culture, string context, long? count, string messageId, [NotNullWhen(true)] out TranslationForm? translation)
    {
        throw new NotImplementedException();
    }
}