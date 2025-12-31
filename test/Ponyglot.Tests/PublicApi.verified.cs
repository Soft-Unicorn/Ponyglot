[assembly: System.CLSCompliant(true)]
[assembly: System.Reflection.AssemblyMetadata("RepositoryUrl", "https://github.com/Soft-Unicorn/Ponyglot")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Ponyglot.Tests")]
namespace Ponyglot
{
    public sealed class DefaultCultureSource : Ponyglot.ICultureSource
    {
        public DefaultCultureSource() { }
        public System.Globalization.CultureInfo Culture { get; set; }
    }
    public interface ICatalog
    {
        System.Globalization.CultureInfo Culture { get; }
        string Domain { get; }
        System.Uri Uri { get; }
        bool TryGet(string context, string messageId, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? translation);
        bool TryGetPlural(string context, long count, string messageId, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? translation);
    }
    public interface ICultureSource
    {
        System.Globalization.CultureInfo Culture { get; set; }
    }
    public interface ITranslationStore
    {
        bool TryGet(string domain, System.Globalization.CultureInfo culture, string context, string messageId, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? translation);
        bool TryGetPlural(string domain, System.Globalization.CultureInfo culture, string context, long count, string messageId, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? translation);
    }
    public interface ITranslator
    {
        string Context { get; }
        string Domain { get; }
        Ponyglot.ITranslator ForCulture(System.Globalization.CultureInfo culture);
        string N(long count, string messageId, string pluralId, params object[]? args);
        string T(string messageId, params object[]? args);
    }
    public interface ITranslatorFactory
    {
        Ponyglot.ITranslator Create(string domain, string context);
        Ponyglot.ITranslator Create<T>();
    }
    public class TranslationStore : Ponyglot.ITranslationStore
    {
        public TranslationStore() { }
        public void Initialize(System.Collections.Generic.IEnumerable<Ponyglot.ICatalog> catalogs) { }
        public bool TryGet(string domain, System.Globalization.CultureInfo culture, string context, string messageId, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? translation) { }
        public bool TryGetPlural(string domain, System.Globalization.CultureInfo culture, string context, long count, string messageId, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? translation) { }
    }
    public class Translator : Ponyglot.ITranslator
    {
        public Translator(Ponyglot.ITranslationStore translationStore, Ponyglot.ICultureSource cultureSource, string domain, string context) { }
        public string Context { get; }
        public string Domain { get; }
        public Ponyglot.ITranslator ForCulture(System.Globalization.CultureInfo culture) { }
        public string N(long count, string messageId, string pluralId, params object[]? args) { }
        public string T(string messageId, params object[]? args) { }
    }
}