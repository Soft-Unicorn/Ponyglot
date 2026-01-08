using System;
using System.Collections.Generic;
using System.Linq;

namespace Ponyglot;

/// <summary>
/// Represents a translated message.
/// </summary>
public class MessageEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageEntry"/> class.
    /// </summary>
    /// <param name="messageId">The message identifier (key).</param>
    /// <param name="context">The message context; an empty string for "no context".</param>
    /// <param name="isPlural">Indicates whether the entry represents a message with plural forms.</param>
    /// <param name="translations">The collection of <see cref="TranslationForm"/> representing the translated forms.</param>
    /// <exception cref="ArgumentNullException"><paramref name="messageId"/> or <paramref name="context"/> or <paramref name="translations"/> is <c>null</c>.</exception>
    private MessageEntry(string messageId, string context, bool isPlural, IReadOnlyList<TranslationForm> translations)
    {
        MessageId = messageId;
        Context = context;
        IsPlural = isPlural;
        Translations = translations;
    }

    /// <summary>
    /// The message identifier (key).
    /// </summary>
    public string MessageId { get; }

    /// <summary>
    /// The message context; an empty string for "no context".
    /// </summary>
    public string Context { get; }

    /// <summary>
    /// Indicates whether the entry represents a message with plural forms.
    /// </summary>
    public bool IsPlural { get; }

    /// <summary>
    /// The collection of <see cref="TranslationForm"/> representing the translated forms.
    /// </summary>
    /// <remarks>For an entry without plural forms (<see cref="IsPlural"/> is <c>false</c>), the <see cref="Translations"/> contains a single element.</remarks>
    public IReadOnlyList<TranslationForm> Translations { get; }

    /// <summary>
    /// Creates a new <see cref="MessageEntry"/> class that represents a message with plural forms.
    /// </summary>
    /// <param name="context">The message context; an empty string for "no context".</param>
    /// <param name="messageId">The message identifier (key).</param>
    /// <param name="translations">The collection of <see cref="TranslationForm"/> representing the translated forms.</param>
    /// <exception cref="ArgumentNullException"><paramref name="messageId"/> or <paramref name="context"/> or <paramref name="translations"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The <paramref name="translations"/> collection is empty or contains a <c>null</c> value.</exception>
    public static MessageEntry Plural(string context, string messageId, IReadOnlyList<TranslationForm> translations)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(messageId);
        ArgumentNullException.ThrowIfNull(translations);

        if (translations.Count == 0) throw new ArgumentException("The translations collection is empty.", nameof(translations));
        if (translations.Any(t => t is null)) throw new ArgumentException("The translations collection contains a null value.", nameof(translations));

        return new MessageEntry(messageId, context, isPlural: true, translations);
    }

    /// <summary>
    /// Creates a new <see cref="MessageEntry"/> class that represents a message without plural forms.
    /// </summary>
    /// <param name="context">The message context; an empty string for "no context".</param>
    /// <param name="messageId">The message identifier (key).</param>
    /// <param name="translation">The <see cref="TranslationForm"/> representing the translated message.</param>
    /// <exception cref="ArgumentNullException"><paramref name="messageId"/> or <paramref name="context"/> or <paramref name="translation"/> is <c>null</c>.</exception>
    public static MessageEntry NonPlural(string context, string messageId, TranslationForm translation)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(messageId);
        ArgumentNullException.ThrowIfNull(translation);

        return new MessageEntry(messageId, context, isPlural: false, [translation]);
    }
}