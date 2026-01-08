using System;

namespace Ponyglot;

/// <summary>
/// Provides the stable convention mapping a <see cref="Type"/> to a translation catalog name and context.
/// </summary>
public static class TranslatorConventions
{
    /// <summary>
    /// Resolves the catalog name and context for the specified type.
    /// </summary>
    /// <param name="type">The type to resolve the catalog name and context for.</param>
    /// <returns>A tuple containing the resolved catalog name and context for the specified type.</returns>
    public static (string CatalogName, string Context) ResolveType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var catalogName = type.Assembly.GetName().Name ?? "";
        var context = type.FullName ?? "";
        return (catalogName, context);
    }
}