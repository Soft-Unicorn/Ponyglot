using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Ponyglot.Tests._TestUtils;

[SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = "Unsafe accessors without explicit implementation")]
public static class TestAccessors
{
    /// <summary>
    /// Defines the test accessors for the <see cref="Translator"/> instances.
    /// </summary>
    /// <param name="translator">The <see cref="Translator"/> instance to extend.</param>
    extension(Translator translator)
    {
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_translationStore")]
        internal extern ref ITranslationStore GetTranslationStore();

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_cultureSource")]
        internal extern ref ICultureSource GetCultureSource();
    }
}