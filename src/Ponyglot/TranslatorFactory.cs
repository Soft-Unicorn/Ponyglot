using System;
using System.Diagnostics.CodeAnalysis;

namespace Ponyglot;

/// <inheritdoc/>
[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global", Justification = "Library")]
public class TranslatorFactory : ITranslatorFactory
{
    private readonly TranslationStore _translationStore;
    private readonly ICultureSource _cultureSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslatorFactory"/> class.
    /// </summary>
    /// <param name="translationStore">The <see cref="TranslationStore"/> that will provide the translations.</param>
    /// <param name="cultureSource">The <see cref="ICultureSource"/> that provides the culture to use to provide the translations.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="translationStore"/>
    /// -or-
    /// <paramref name="cultureSource"/>
    /// is <c>null</c>.
    /// </exception>
    public TranslatorFactory(TranslationStore translationStore, ICultureSource cultureSource)
    {
        _translationStore = translationStore ?? throw new ArgumentNullException(nameof(translationStore));
        _cultureSource = cultureSource ?? throw new ArgumentNullException(nameof(cultureSource));
    }

    /// <inheritdoc/>
    public virtual ITranslator Create(string catalogName, string context)
    {
        return new Translator(_translationStore, _cultureSource, catalogName, context);
    }
}