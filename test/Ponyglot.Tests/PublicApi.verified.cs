[assembly: System.CLSCompliant(true)]
[assembly: System.Reflection.AssemblyMetadata("RepositoryUrl", "https://github.com/Soft-Unicorn/Ponyglot")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Ponyglot.Tests")]
namespace Ponyglot
{
    public class Catalog
    {
        public Catalog() { }
        public virtual string CatalogName { get; }
        public virtual System.Globalization.CultureInfo Culture { get; }
        public virtual string Uid { get; }
        public virtual bool TryGet(string context, long? count, string messageId, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Ponyglot.TranslationForm? translation) { }
    }
    public sealed class DefaultCultureSource : Ponyglot.ICultureSource
    {
        public DefaultCultureSource() { }
        public System.Globalization.CultureInfo Culture { get; set; }
    }
    public interface ICultureSource
    {
        System.Globalization.CultureInfo Culture { get; set; }
    }
    public interface ITranslator
    {
        string CatalogName { get; }
        string Context { get; }
        Ponyglot.ITranslator ForCulture(System.Globalization.CultureInfo culture);
        string N(long count, string messageId, string pluralId, params object?[]? args);
        string T(string messageId, params object?[]? args);
    }
    public interface ITranslatorFactory
    {
        Ponyglot.ITranslator Create(string catalogName, string context);
    }
    public class TranslationForm
    {
        public bool IsCompositeFormat { get; }
        public string Message { get; }
        public override string ToString() { }
        public static Ponyglot.TranslationForm CompositeFormat(string message) { }
        public static Ponyglot.TranslationForm Text(string message) { }
    }
    public class TranslationStore
    {
        public TranslationStore() { }
        public virtual void Initialize(System.Collections.Generic.IEnumerable<Ponyglot.Catalog> catalogs) { }
        public virtual bool TryGet(string catalogName, System.Globalization.CultureInfo culture, string context, long? count, string messageId, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Ponyglot.TranslationForm? translation) { }
    }
    public class Translator : Ponyglot.ITranslator
    {
        public Translator(Ponyglot.TranslationStore translationStore, Ponyglot.ICultureSource cultureSource, string catalogName, string context) { }
        public string CatalogName { get; }
        public string Context { get; }
        protected Ponyglot.ICultureSource CultureSource { get; }
        protected Ponyglot.TranslationStore Store { get; }
        public Ponyglot.ITranslator ForCulture(System.Globalization.CultureInfo culture) { }
        protected virtual string GetTranslation(long? count, string messageId, string defaultMessage, object?[]? args) { }
        public string N(long count, string messageId, string pluralId, params object?[]? args) { }
        public string T(string messageId, params object?[]? args) { }
    }
    public static class TranslatorConventions
    {
        [return: System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "CatalogName",
                "Context"})]
        public static System.ValueTuple<string, string> ResolveType(System.Type type) { }
    }
    public class TranslatorFactory : Ponyglot.ITranslatorFactory
    {
        public TranslatorFactory(Ponyglot.TranslationStore translationStore, Ponyglot.ICultureSource cultureSource) { }
        public virtual Ponyglot.ITranslator Create(string catalogName, string context) { }
    }
    public static class TranslatorFactoryExtensions
    {
        public static Ponyglot.ITranslator Create(this Ponyglot.ITranslatorFactory factory, System.Type type) { }
        public static Ponyglot.ITranslator Create<T>(this Ponyglot.ITranslatorFactory factory) { }
    }
}