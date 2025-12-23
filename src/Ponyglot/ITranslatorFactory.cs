namespace Ponyglot;

/// <summary>
/// Factory used to create <see cref="ITranslator"/> instances.
/// </summary>
public interface ITranslatorFactory
{
    /// <summary>
    /// Creates a new <see cref="ITranslator"/> instance for <typeparamref name="T"/>, deriving the domain and context from the type.
    /// </summary>
    /// <typeparam name="T">The type used to derive the catalog name the context.</typeparam>
    /// <returns>A new <see cref="ITranslator"/> instance.</returns>
    /// <remarks>
    /// The created <see cref="ITranslator"/> uses the assembly name of <typeparamref name="T"/> as domain name and the full type name as context.
    /// </remarks>
    ITranslator Create<T>();

    /// <summary>
    /// Creates a new <see cref="ITranslator"/> instance with an explicit domain and context.
    /// </summary>
    /// <param name="domain">The domain or catalog to search for translations.</param>
    /// <param name="context">The context to use when searching for translations provided by this instance.</param>
    /// <returns>A new <see cref="ITranslator"/> instance.</returns>
    ITranslator Create(string domain, string context);
}