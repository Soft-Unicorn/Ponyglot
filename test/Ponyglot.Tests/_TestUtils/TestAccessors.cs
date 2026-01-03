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
}