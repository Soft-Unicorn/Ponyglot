using System;
using System.Diagnostics.CodeAnalysis;

namespace Ponyglot;

/// <summary>
/// Convenience extensions for <see cref="ITranslatorFactory"/>.
/// </summary>
[SuppressMessage("ReSharper", "ConvertToExtensionBlock", Justification = "Not compatible with non .net 10 SDK. See https://github.com/dotnet/roslyn/issues/81943")]
public static class TranslatorFactoryExtensions
{
    /// <summary>
    /// Creates a new <see cref="ITranslator"/> instance for <typeparamref name="T"/>, deriving the catalog name and context from the <see cref="Type"/>.
    /// </summary>
    /// <param name="factory">The <see cref="ITranslatorFactory"/> to extend.</param>
    /// <typeparam name="T">The <see cref="Type"/> to create a translator for.</typeparam>
    /// <returns>A new <see cref="ITranslator"/> instance.</returns>
    /// <remarks>The catalog name and context are derived from the specified <typeparamref name="T"/> type using the <see cref="TranslatorConventions.ResolveType"/> method.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="factory"/> is <c>null</c>.</exception>
    public static ITranslator Create<T>(this ITranslatorFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var (catalogName, context) = TranslatorConventions.ResolveType(typeof(T));
        return factory.Create(catalogName, context);
    }

    /// <summary>
    /// Creates a new <see cref="ITranslator"/> instance for <paramref name="type"/>, deriving the catalog name and context from the <see cref="Type"/>.
    /// </summary>
    /// <param name="factory">The <see cref="ITranslatorFactory"/> to extend.</param>
    /// <param name="type">The <see cref="Type"/> to create a translator for.</param>
    /// <returns>A new <see cref="ITranslator"/> instance.</returns>
    /// <remarks>The catalog name and context are derived from the specified <paramref name="type"/> using the <see cref="TranslatorConventions.ResolveType"/> method.</remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="type"/>
    /// -or-
    /// <paramref name="factory"/>
    /// is <c>null</c>.
    /// </exception>
    public static ITranslator Create(this ITranslatorFactory factory, Type type)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(type);

        var (catalogName, context) = TranslatorConventions.ResolveType(type);
        return factory.Create(catalogName, context);
    }
}