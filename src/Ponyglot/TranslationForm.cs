using System;

namespace Ponyglot;

/// <summary>
/// Represents a single message translation form.
/// </summary>
public class TranslationForm
{
    /// <summary>
    /// Forbids the creation of new <see cref="TranslationForm"/> instances.
    /// </summary>
    /// <param name="message">The translated message.</param>
    /// <param name="isCompositeFormat">Indicates whether the translation is a composite format string.</param>
    private TranslationForm(string message, bool isCompositeFormat)
    {
        IsCompositeFormat = isCompositeFormat;
        Message = message;
    }

    /// <summary>
    /// The translated message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Indicates whether the translation is a composite format string.
    /// </summary>
    public bool IsCompositeFormat { get; }

    /// <summary>
    /// Creates a translation form for a simple text message.
    /// </summary>
    /// <param name="message">The translated message.</param>
    /// <returns>The created <see cref="TranslationForm"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is <c>null</c>.</exception>
    public static TranslationForm Text(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return new TranslationForm(message, isCompositeFormat: false);
    }

    /// <summary>
    /// Creates a translation form for a composite format string.
    /// </summary>
    /// <param name="message">The translated message format.</param>
    /// <returns>The created <see cref="TranslationForm"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is <c>null</c>.</exception>
    public static TranslationForm CompositeFormat(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return new TranslationForm(message, isCompositeFormat: true);
    }

    /// <inheritdoc/>
    public override string ToString() => Message;
}