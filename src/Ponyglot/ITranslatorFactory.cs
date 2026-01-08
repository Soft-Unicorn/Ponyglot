namespace Ponyglot;

/// <summary>
/// Factory used to create <see cref="ITranslator"/> instances.
/// </summary>
public interface ITranslatorFactory
{
    /// <summary>
    /// Creates a new <see cref="ITranslator"/> instance with an explicit <paramref name="catalogName"/> and <paramref name="context"/>.
    /// </summary>
    /// <param name="catalogName">The name that identifies the catalog to look up.</param>
    /// <param name="context">The context to use when searching for translations.</param>
    /// <returns>A new <see cref="ITranslator"/> instance.</returns>
    ITranslator Create(string catalogName, string context);
}