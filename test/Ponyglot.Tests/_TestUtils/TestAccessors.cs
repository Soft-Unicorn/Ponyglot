using System.Collections.Frozen;
using System.Collections.Generic;
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

    extension(MessageEntry)
    {
        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        public static extern MessageEntry New(string messageId, string context, bool isPlural, IReadOnlyList<TranslationForm> translations);
    }

    extension(Catalog catalog)
    {
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_translationsIndex")]
        public extern ref FrozenDictionary<string, FrozenDictionary<string, IReadOnlyList<TranslationForm>>>? GetTranslationsIndex();
    }
}