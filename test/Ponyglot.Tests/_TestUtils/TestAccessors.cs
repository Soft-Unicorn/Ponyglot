#if NETCOREAPP
using System.Collections.Frozen;
#else
using System.Collections.Generic;
#endif
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Ponyglot.Sources.PortableObject.PluralRule;

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
    private static readonly ConcurrentDictionary<(Type Type, string Signature), ConstructorInfo> ConstructorCache = new();

    /// <summary>
    /// Defines the test accessors for <see cref="Translator"/> instances.
    /// </summary>
    /// <param name="translator">The <see cref="Translator"/> instance to extend.</param>
    extension(Translator translator)
    {
        internal ITranslationStore GetTranslationStore() => ReadField<ITranslationStore>(translator, "_translationStore");
        internal ICultureSource GetCultureSource() => ReadField<ICultureSource>(translator, "_cultureSource");
    }

    extension(TranslationStore store)
    {
#if NETCOREAPP
        internal FrozenDictionary<string, FrozenDictionary<string, ICatalog>>? GetCatalogsIndex() => ReadField<FrozenDictionary<string, FrozenDictionary<string, ICatalog>>?>(store, "_catalogsIndex");
#else
        internal Dictionary<string, Dictionary<string, ICatalog>>? GetCatalogsIndex() => ReadField<Dictionary<string, Dictionary<string, ICatalog>>?>(store, "_catalogsIndex");
#endif
    }

    /// <summary>
    /// Defines extension methods for the <see cref="PluralRuleExpression"/> type.
    /// </summary>
    extension(PluralRuleExpression)
    {
        internal static PluralRuleExpression New(string symbol, PluralRuleExpression[] children, Func<long, PluralRuleExpression[], long> evaluator) =>
            Create<PluralRuleExpression>(
                [typeof(string), typeof(PluralRuleExpression[]), typeof(Func<long, PluralRuleExpression[], long>)],
                [symbol, children, evaluator]);
    }

    /// <summary>
    /// Defines the test accessors for <see cref="PluralRuleExpression"/> instances.
    /// </summary>
    /// <param name="expression">The <see cref="PluralRuleExpression"/> instance to extend.</param>
    extension(PluralRuleExpression expression)
    {
        internal string GetSymbol() => ReadField<string>(expression, "_symbol");
        internal PluralRuleExpression[] GetChildren() => ReadField<PluralRuleExpression[]>(expression, "_children");
    }

    private static T ReadField<T>(object instance, string name)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var fieldInfo = FieldCache.GetOrAdd(
            (instance.GetType(), name),
            key => key.Type.GetField(name, flags) ?? throw new MissingFieldException(key.Type.FullName, name));

        return (T)fieldInfo.GetValue(instance)!;
    }

    private static T Create<T>(Type[] argTypes, object[] argValues)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var signature = string.Join(", ", argTypes.Select(t => t.FullName));

        var ctorInfo = ConstructorCache.GetOrAdd(
            (typeof(T), signature),
            key => key.Type.GetConstructor(flags, binder: null, types: argTypes, modifiers: null)
                   ?? throw new MissingMethodException(key.Type.FullName, ".ctor"));

        return (T)ctorInfo.Invoke(argValues);
    }
}