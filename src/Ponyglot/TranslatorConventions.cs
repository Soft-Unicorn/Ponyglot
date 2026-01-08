using System;

namespace Ponyglot;

/// <summary>
/// Provides the stable convention mapping a <see cref="Type"/> to a translation domain and context.
/// </summary>
public static class TranslatorConventions
{
    /// <summary>
    /// Resolves the domain and context for the specified type.
    /// </summary>
    /// <param name="type">The type to resolve the domain and context for.</param>
    /// <returns>A tuple containing resolved the domain and context for the specified type.</returns>
    public static (string Domain, string Context) ResolveType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var domain = type.Assembly.GetName().Name ?? "";
        var context = type.FullName ?? "";
        return (domain, context);
    }
}