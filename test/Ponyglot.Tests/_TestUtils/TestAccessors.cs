using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Ponyglot.Tests._TestUtils;

/// <summary>
/// Exposes test accessors for non-public fields
/// </summary>
/// <remarks>
/// Due to backward compatibility requirements with the legacy .NET Framework 4.8 used to run the tests,
/// the accessors are implemented using good old reflection rather than with the more modern UnsafeAccessor attribute.
/// </remarks>
[SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = "Unsafe accessors without explicit implementation")]
public static class TestAccessors
{
    private static readonly ConcurrentDictionary<(Type Type, string Name), FieldInfo> FieldCache = new();

    /// <summary>
    /// Defines the test accessors for the <see cref="Translator"/> instances.
    /// </summary>
    /// <param name="translator">The <see cref="Translator"/> instance to extend.</param>
    extension(Translator translator)
    {
        internal ITranslationStore GetTranslationStore() => ReadField<ITranslationStore>(translator, "_translationStore");
        internal ICultureSource GetCultureSource() => ReadField<ICultureSource>(translator, "_cultureSource");
    }

    private static T ReadField<T>(object instance, string name)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var fieldInfo = FieldCache.GetOrAdd(
            (instance.GetType(), name),
            key => key.Type.GetField(name, flags) ?? throw new MissingFieldException(key.Type.FullName, name));

        return (T)fieldInfo.GetValue(instance)!;
    }
}