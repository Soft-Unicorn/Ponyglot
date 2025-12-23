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
    /// <param name="context">The optional context to use when searching for translations provided by this instance.</param>
    /// <param name="messageId">The message to translate.</param>
    /// <returns>The translated string or <c>null</c> if no suitable translation is found.</returns>
    string? Find(string domain, CultureInfo culture, string? context, string messageId);

    /// <summary>
    /// Tries to find a translated plural value for the given message key.
    /// </summary>
    /// <param name="domain">The domain (catalog) to look up.</param>
    /// <param name="culture">The requested UI culture (implementations should apply parent fallback).</param>
    /// <param name="context">The optional context to use when searching for translations provided by this instance.</param>
    /// <param name="count">The count used for plural selection.</param>
    /// <param name="messageId">The message to translate.</param>
    /// <param name="pluralId">The plural message to translate.</param>
    /// <returns>The translated string or <c>null</c> if no suitable translation is found.</returns>
    string? FindPlural(string domain, CultureInfo culture, string? context, long count, string messageId, string pluralId);
}