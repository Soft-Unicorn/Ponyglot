using System;

namespace Ponyglot;

/// <inheritdoc/>
public class TranslatorFactory : ITranslatorFactory
{
    private readonly ITranslationStore _translationStore;
    private readonly ICultureSource _cultureSource;

    /// <summary>
    /// Initialize a new instance of the <see cref="TranslatorFactory"/> class.
    /// </summary>
    /// <param name="translationStore">The <see cref="ITranslationStore"/> that will provide the translations.</param>
    /// <param name="cultureSource">The <see cref="ICultureSource"/> that provides the culture to use to provide the translations.</param>
    public TranslatorFactory(ITranslationStore translationStore, ICultureSource cultureSource)
    {
        _translationStore = translationStore ?? throw new ArgumentNullException(nameof(translationStore));
        _cultureSource = cultureSource ?? throw new ArgumentNullException(nameof(cultureSource));
    }

    /// <inheritdoc/>
    public virtual ITranslator Create<T>()
    {
        var (domain, context) = ResolveType(typeof(T));
        return Create(domain, context);
    }

    /// <inheritdoc/>
    public virtual ITranslator Create(string domain, string? context)
    {
        return new Translator(_translationStore, _cultureSource, domain, context);
    }

    /// <inheritdoc/>
    public virtual (string Domain, string? Context) ResolveType(Type type)
    {
        var domain = type.Assembly.GetName().Name ?? "";
        var context = type.FullName;
        return (domain, context);
    }
}