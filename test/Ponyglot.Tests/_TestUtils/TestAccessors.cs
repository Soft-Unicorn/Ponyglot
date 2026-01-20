using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Ponyglot.Tests._TestUtils;

[SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = "Unsafe accessors without explicit implementation")]
internal static class TestAccessors
{
    extension(Translator translator)
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Store")]
        public extern TranslationStore GetTranslationStore();

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_CultureSource")]
        public extern ICultureSource GetCultureSource();
    }

    extension(TranslationStore store)
    {
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_catalogsIndex")]
        public extern ref FrozenDictionary<string, FrozenDictionary<string, Catalog>>? GetCatalogsIndex();
    }
}