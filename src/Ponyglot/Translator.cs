using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace Ponyglot;

/// <inheritdoc/>
public class Translator : ITranslator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Translator"/> class.
    /// </summary>
    /// <param name="translationStore">The <see cref="TranslationStore"/> that will provide the translations.</param>
    /// <param name="cultureSource">The <see cref="ICultureSource"/> that provides the culture to use to provide the translations.</param>
    /// <param name="catalogName">The name that identifies the catalog to look up.</param>
    /// <param name="context">The context to use when searching for translations.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="translationStore"/>
    /// -or-
    /// <paramref name="cultureSource"/>
    /// -or-
    /// <paramref name="catalogName"/>
    /// -or-
    /// <paramref name="context"/>
    /// is <c>null</c>.
    /// </exception>
    public Translator(TranslationStore translationStore, ICultureSource cultureSource, string catalogName, string context)
    {
        Store = translationStore ?? throw new ArgumentNullException(nameof(translationStore));
        CultureSource = cultureSource ?? throw new ArgumentNullException(nameof(cultureSource));
        CatalogName = catalogName ?? throw new ArgumentNullException(nameof(catalogName));
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// The <see cref="TranslationStore"/> that will provide the translations.
    /// </summary>
    protected TranslationStore Store { get; }

    /// <summary>
    /// The <see cref="ICultureSource"/> that provides the culture to use to provide the translations.
    /// </summary>
    protected ICultureSource CultureSource { get; }

    /// <inheritdoc/>
    public string CatalogName { get; }

    /// <inheritdoc/>
    public string Context { get; }

    /// <inheritdoc/>
    public ITranslator ForCulture(CultureInfo culture) => new Translator(Store, new FixedCultureSource(culture), CatalogName, Context);

    /// <inheritdoc/>
    public string T(string messageId, params object?[]? args)
    {
        return GetTranslation(count: null, messageId: messageId, defaultMessage: messageId, args: args);
    }

    /// <inheritdoc/>
    public string N(long count, string messageId, string pluralId, params object?[]? args)
    {
        return GetTranslation(count: count, messageId: messageId, defaultMessage: count == 1 ? messageId : pluralId, args: args);
    }

    /// <summary>
    /// Gets the translation for the given message identifier and arguments.
    /// </summary>
    /// <param name="count">The optional count used for plural selection in plural scenarios.</param>
    /// <param name="messageId">The message id that identifies the translation to locate.</param>
    /// <param name="defaultMessage">The default message to use if no translation is found.</param>
    /// <param name="args">The optional composite format arguments.</param>
    /// <returns>The translation, formatted if applicable.</returns>
    /// <exception cref="FormatException">A message formatting error occurred.</exception>
    /// <exception cref="InvalidOperationException">An error occurred during the lookup of the translation.</exception>
    protected virtual string GetTranslation(long? count, string messageId, string defaultMessage, object?[]? args)
    {
        args ??= [];
        var message = defaultMessage;
        try
        {
            var isCompositeFormat = args.Length > 0;
            if (Store.TryGet(CatalogName, CultureSource.Culture, Context, count, messageId, out var foundTranslation))
            {
                isCompositeFormat = foundTranslation.IsCompositeFormat;
                message = foundTranslation.Message;
            }

            var formattedTranslation = isCompositeFormat ? string.Format(CultureSource.Culture, message, args) : message;
            return formattedTranslation;
        }
        catch (FormatException exception)
        {
            var prettyArgs = args.Select(a => a switch
            {
                string s => $"\"{s.Replace("\"", "\\\"", StringComparison.OrdinalIgnoreCase)}\"",
                true => "true",
                false => "false",
                _ => a?.ToString() ?? "␀",
            });

            throw new FormatException(GetError(couldNotBe: $"formatted with the arguments [{string.Join(", ", prettyArgs)}]."), exception);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(GetError(couldNotBe: "loaded from the store."), exception);
        }

        string GetError(string couldNotBe) => $"The {CultureSource.Culture.Name} translation for '{messageId}' {(message == messageId ? "" : $"({message}) ")}could not be {couldNotBe}";
    }

    /// <summary>
    /// A fixed culture source, used for <see cref="ITranslator.ForCulture(CultureInfo)"/>.
    /// </summary>
    /// <param name="culture">The fixed UI culture.</param>
    [ExcludeFromCodeCoverage]
    private sealed class FixedCultureSource(CultureInfo culture) : ICultureSource
    {
        /// <inheritdoc/>
        public CultureInfo Culture
        {
            get => culture;
            set => throw new NotSupportedException($"The culture of a {nameof(FixedCultureSource)} cannot be changed.");
        }
    }
}