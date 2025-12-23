using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Ponyglot;

/// <summary>
/// The store that provides the translations.
/// </summary>
public interface ITranslationStore
{
    /// <summary>
    /// Tries to find a translated value for the given message key.
    /// </summary>
    /// <param name="domain">The domain (catalog) to look up.</param>
    /// <param name="culture">The requested UI culture (implementations should apply parent fallback).</param>
    /// <param name="context">The context to use when searching for translations provided by this instance.</param>
    /// <param name="messageId">The message to translate.</param>
    /// <param name="translation">When this method returns <c>true</c>, contains the translated string; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if a translation is found; otherwise, <c>false</c>.</returns>
    bool TryGet(string domain, CultureInfo culture, string context, string messageId, [NotNullWhen(true)] out string? translation);

    /// <summary>
    /// Tries to find a translated plural value for the given message key.
    /// </summary>
    /// <param name="domain">The domain (catalog) to look up.</param>
    /// <param name="culture">The requested UI culture (implementations should apply parent fallback).</param>
    /// <param name="context">The context to use when searching for translations provided by this instance.</param>
    /// <param name="count">The count used for plural selection.</param>
    /// <param name="messageId">The message to translate.</param>
    /// <param name="translation">When this method returns <c>true</c>, contains the translated string; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if a translation is found; otherwise, <c>false</c>.</returns>
    bool TryGetPlural(string domain, CultureInfo culture, string context, long count, string messageId, [NotNullWhen(true)] out string? translation);
}