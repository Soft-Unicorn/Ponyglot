[assembly: System.CLSCompliant(true)]
[assembly: System.Reflection.AssemblyMetadata("RepositoryUrl", "https://github.com/Soft-Unicorn/Ponyglot")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Ponyglot.Tests")]
namespace Ponyglot
{
    public class Catalog
    {
        public Catalog(System.Uri uri, string domain, System.Globalization.CultureInfo culture, Ponyglot.IPluralRule pluralRule, System.Collections.Generic.IEnumerable<Ponyglot.MessageEntry> entries) { }
        public virtual System.Globalization.CultureInfo Culture { get; }
        public virtual string Domain { get; }
        public virtual System.Uri Uri { get; }
        public override string ToString() { }
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
    public interface IPluralRule
    {
        int PluralCount { get; }
        int GetPluralForm(long count);
    }
    public interface ITranslator
    {
        string Context { get; }
        string Domain { get; }
        Ponyglot.ITranslator ForCulture(System.Globalization.CultureInfo culture);
        string N(long count, string messageId, string pluralId, params object?[]? args);
        string T(string messageId, params object?[]? args);
    }
    public interface ITranslatorFactory
    {
        Ponyglot.ITranslator Create(string domain, string context);
    }
    public class MessageEntry
    {
        public string Context { get; }
        public bool IsPlural { get; }
        public string MessageId { get; }
        public System.Collections.Generic.IReadOnlyList<Ponyglot.TranslationForm> Translations { get; }
        public static Ponyglot.MessageEntry NonPlural(string context, string messageId, Ponyglot.TranslationForm translation) { }
        public static Ponyglot.MessageEntry Plural(string context, string messageId, System.Collections.Generic.IReadOnlyList<Ponyglot.TranslationForm> translations) { }
    }
    public class PonyglotRuntime
    {
        public PonyglotRuntime(Ponyglot.TranslationStore translationStore, Ponyglot.ICultureSource cultureSource, System.Func<Ponyglot.TranslationStore, Ponyglot.ICultureSource, Ponyglot.ITranslatorFactory> translationFactoryProvider, System.Collections.Generic.IEnumerable<Ponyglot.Loading.ICatalogLocator> locators, System.Collections.Generic.IEnumerable<Ponyglot.Loading.ICatalogLoader> loaders) { }
        public Ponyglot.ICultureSource CultureSource { get; }
        public bool IsInitialized { get; }
        public Ponyglot.TranslationStore Store { get; }
        public Ponyglot.ITranslatorFactory TranslatorFactory { get; }
        protected virtual System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<Ponyglot.Loading.CatalogResource>> FindCatalogsAsync(System.Threading.CancellationToken cancellationToken) { }
        protected virtual System.Threading.Tasks.Task<Ponyglot.Loading.ICatalogLoader> GetLoaderAsync(Ponyglot.Loading.CatalogResource resource, System.Threading.CancellationToken cancellationToken) { }
        public System.Threading.Tasks.Task InitializeAsync(System.Threading.CancellationToken cancellationToken = default) { }
        protected virtual System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<Ponyglot.Catalog>> LoadCatalogsAsync(System.Collections.Generic.IReadOnlyList<Ponyglot.Loading.CatalogResource> resources, System.Threading.CancellationToken cancellationToken) { }
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
        public virtual bool TryGet(string domain, System.Globalization.CultureInfo culture, string context, long? count, string messageId, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Ponyglot.TranslationForm? translation) { }
    }
    public class Translator : Ponyglot.ITranslator
    {
        public Translator(Ponyglot.TranslationStore translationStore, Ponyglot.ICultureSource cultureSource, string domain, string context) { }
        public string Context { get; }
        protected Ponyglot.ICultureSource CultureSource { get; }
        public string Domain { get; }
        protected Ponyglot.TranslationStore Store { get; }
        public Ponyglot.ITranslator ForCulture(System.Globalization.CultureInfo culture) { }
        protected virtual string GetTranslation(long? count, string messageId, string defaultMessage, object?[]? args) { }
        public string N(long count, string messageId, string pluralId, params object?[]? args) { }
        public string T(string messageId, params object?[]? args) { }
    }
    public static class TranslatorConventions
    {
        [return: System.Runtime.CompilerServices.TupleElementNames(new string[] {
                "Domain",
                "Context"})]
        public static System.ValueTuple<string, string> ResolveType(System.Type type) { }
    }
    public class TranslatorFactory : Ponyglot.ITranslatorFactory
    {
        public TranslatorFactory(Ponyglot.TranslationStore translationStore, Ponyglot.ICultureSource cultureSource) { }
        public virtual Ponyglot.ITranslator Create(string domain, string context) { }
    }
    public static class TranslatorFactoryExtensions
    {
        public static Ponyglot.ITranslator Create(this Ponyglot.ITranslatorFactory factory, System.Type type) { }
        public static Ponyglot.ITranslator Create<T>(this Ponyglot.ITranslatorFactory factory) { }
    }
}
namespace Ponyglot.Loading
{
    public abstract class CatalogResource
    {
        protected CatalogResource(System.Uri uri) { }
        public System.Uri Uri { get; }
        public abstract System.Threading.Tasks.Task<System.IO.Stream> OpenAsync(System.Threading.CancellationToken cancellationToken = default);
        public override string ToString() { }
    }
    public interface ICatalogLoader
    {
        System.Threading.Tasks.Task<bool> CanLoadAsync(Ponyglot.Loading.CatalogResource catalogResource, System.Threading.CancellationToken cancellationToken = default);
        System.Threading.Tasks.Task<Ponyglot.Catalog> LoadAsync(Ponyglot.Loading.CatalogResource catalogResource, System.Threading.CancellationToken cancellationToken = default);
    }
    public interface ICatalogLocator
    {
        System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyCollection<Ponyglot.Loading.CatalogResource>> FindCatalogsAsync(System.Threading.CancellationToken cancellationToken = default);
    }
}