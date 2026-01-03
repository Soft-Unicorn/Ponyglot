using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Ponyglot;

/// <inheritdoc/>
public class Translator : ITranslator
{
    private readonly ITranslationStore _translationStore;
    private readonly ICultureSource _cultureSource;

    /// <summary>
    /// Initialize a new instance of the <see cref="Translator"/> class.
    /// </summary>
    /// <param name="translationStore">The <see cref="ITranslationStore"/> that will provide the translations.</param>
    /// <param name="cultureSource">The <see cref="ICultureSource"/> that provides the culture to use to provide the translations.</param>
    /// <param name="domain">The domain (catalog) to use when searching for translations provided by this instance.</param>
    /// <param name="context">The context to use when searching for translations provided by this instance.</param>
    public Translator(ITranslationStore translationStore, ICultureSource cultureSource, string domain, string context)
    {
        _translationStore = translationStore ?? throw new ArgumentNullException(nameof(translationStore));
        _cultureSource = cultureSource ?? throw new ArgumentNullException(nameof(cultureSource));
        Domain = domain ?? throw new ArgumentNullException(nameof(domain));
        Context = context;
    }

    /// <inheritdoc/>
    public ITranslator ForCulture(CultureInfo culture) => new Translator(_translationStore, new FixedCultureSource(culture), Domain, Context);

    /// <inheritdoc/>
    public string Domain { get; }

    /// <inheritdoc/>
    public string Context { get; }

    /// <inheritdoc/>
    public string T(string messageId, params object?[]? args)
    {
        string? foundTranslation = null;
        try
        {
            var message = _translationStore.TryGet(Domain, _cultureSource.Culture, Context, messageId, out foundTranslation) ? foundTranslation : messageId;
            var formattedTranslation = string.Format(_cultureSource.Culture, message, args ?? []);
            return formattedTranslation;
        }
        catch (Exception exception)
        {
            throw BuildTranslationError(messageId, foundTranslation, args, exception);
        }
    }

    /// <inheritdoc/>
    public string N(long count, string messageId, string pluralId, params object?[]? args)
    {
        string? foundTranslation = null;
        var fallbackMessage = count == 1 ? messageId : pluralId;
        try
        {
            var message = _translationStore.TryGetPlural(Domain, _cultureSource.Culture, Context, count, messageId, out foundTranslation) ? foundTranslation : fallbackMessage;
            var formattedTranslation = string.Format(_cultureSource.Culture, message, args ?? []);
            return formattedTranslation;
        }
        catch (Exception exception)
        {
            throw BuildTranslationError(fallbackMessage, foundTranslation, args, exception);
        }
    }

    /// <summary>
    /// Builds the error to throw when a translation is not found or cannot be formatted.
    /// </summary>
    /// <param name="message">The original message that was being translated.</param>
    /// <param name="foundTranslation">The translation that was found or <c>null</c> if none was found.</param>
    /// <param name="formatArguments">The list of format arguments that were passed to the translation function.</param>
    /// <param name="exception">The exception that occurred while trying to format the translation.</param>
    /// <returns>The exception to throw.</returns>
    private Exception BuildTranslationError(string message, string? foundTranslation, object?[]? formatArguments, Exception exception)
    {
        var argumentList = formatArguments switch
        {
            null or { Length: 0 } => "",
            { Length: 1 } => $"{formatArguments[0]}",
            _ => $"{string.Join(", ", formatArguments)}",
        };

        var messageBuilder = new StringBuilder()
            .Append("The ").Append(_cultureSource.Culture.Name).Append(" translation for '").Append(message).Append("' ")
            .Append(foundTranslation != null ? $"({foundTranslation}) " : "");

        if (exception is FormatException)
        {
            messageBuilder.Append("could not be formatted with the arguments [").Append(argumentList).Append("].");
            return new FormatException(messageBuilder.ToString(), exception);
        }

        messageBuilder.Append("could not be loaded from the store.");
        return new InvalidOperationException(messageBuilder.ToString(), exception);
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