using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Ponyglot;

/// <summary>
/// A loaded translation catalog that can provide translations for a specific culture.
/// </summary>
public interface ICatalog
{
    /// <summary>
    /// The <see cref="Uri"/> that identifies the catalog.
    /// </summary>
    Uri Uri { get; }

    /// <summary>
    /// The domain or catalog name the translations belong to.
    /// </summary>
    string Domain { get; }

    /// <summary>
    /// The culture of the translations.
    /// </summary>
    CultureInfo Culture { get; }

    /// <summary>
    /// Tries to find a translated value for the given message key.
    /// </summary>
    /// <param name="context">The context to use when searching for translations provided by this instance.</param>
    /// <param name="messageId">The message to translate.</param>
    /// <param name="translation">When this method returns <c>true</c>, contains the translated string; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if a translation is found; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="messageId"/> is <c>null</c>.</exception>
    bool TryGet(string context, string messageId, [NotNullWhen(true)] out string? translation);

    /// <summary>
    /// Tries to find a translated plural value for the given message key.
    /// </summary>
    /// <param name="context">The context to use when searching for translations provided by this instance.</param>
    /// <param name="count">The count used for plural selection.</param>
    /// <param name="messageId">The message to translate.</param>
    /// <param name="translation">When this method returns <c>true</c>, contains the translated string; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if a translation is found; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="messageId"/> is <c>null</c>.</exception>
    bool TryGetPlural(string context, long count, string messageId, [NotNullWhen(true)] out string? translation);
}