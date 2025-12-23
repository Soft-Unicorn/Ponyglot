using System.Globalization;

namespace Ponyglot;

/// <summary>
/// Provides translation services for a given base message subContext and a domain (catalog).
/// </summary>
public interface ITranslator
{
    /// <summary>
    /// Returns a translation that uses the provided <paramref name="culture"/> to lookup for translations.
    /// </summary>
    /// <param name="culture">The <see cref="CultureInfo"/> to use to lookup for translations.</param>
    /// <returns>An <see cref="ITranslator"/> instance using the specified <paramref name="culture"/>.</returns>
    ITranslator ForCulture(CultureInfo culture);

    /// <summary>
    /// The name of the domain (catalog) to use when searching for translations provided by this instance.
    /// </summary>
    string Domain { get; }

    /// <summary>
    /// The optional context to use when searching for translations provided by this instance.
    /// </summary>
    string? Context { get; }

    /// <summary>
    /// Translates a message.
    /// </summary>
    /// <param name="messageId">The message or message format to translate.</param>
    /// <param name="args">An optional object array that contains zero or more objects to format.</param>
    /// <returns>The translated string, or <paramref name="messageId"/> if no translation exists.</returns>
    /// <remarks>
    /// <paramref name="messageId"/> can be a simple string or a message format adhering to the <see cref="M:string.Format(string, object[])"/> rules.
    /// </remarks>
    string T(string messageId, params object[]? args);

    /// <summary>
    /// Translates a plural message.
    /// </summary>
    /// <param name="count">The count used for plural selection.</param>
    /// <param name="messageId">The message or message format to translate when <paramref name="count"/> indicates a non-plural form.</param>
    /// <param name="pluralId">The message or message format to translate when <paramref name="count"/> indicates a plural form.</param>
    /// <param name="args">An optional object array that contains zero or more objects to format.</param>
    /// <returns>The translated string, or the formatted <paramref name="messageId"/> if no translation exists.</returns>
    /// <remarks>
    ///     <para>
    ///     <paramref name="messageId"/> and <paramref name="pluralId"/> can be a simple string or a message format adhering to the <see cref="M:string.Format(string, object[])"/> rules.
    ///     </para>
    ///     <para>
    ///     The specified <paramref name="args"/> are common to the <paramref name="messageId"/> and <paramref name="pluralId"/> message formats.
    ///     </para>
    /// </remarks>
    string N(long count, string messageId, string pluralId, params object[]? args);
}