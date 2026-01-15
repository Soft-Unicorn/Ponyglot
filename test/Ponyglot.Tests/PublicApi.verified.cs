[assembly: System.CLSCompliant(true)]
[assembly: System.Reflection.AssemblyMetadata("RepositoryUrl", "https://github.com/Soft-Unicorn/Ponyglot")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Ponyglot.Tests")]
namespace Ponyglot
{
    public class Catalog
    {
        public Catalog(string uid, string catalogName, System.Globalization.CultureInfo culture, Ponyglot.IPluralRule pluralRule, System.Collections.Generic.IEnumerable<Ponyglot.MessageEntry> entries) { }
        public virtual string CatalogName { get; }
        public virtual System.Globalization.CultureInfo Culture { get; }
        public virtual string Uid { get; }
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
        public PonyglotRuntime(Ponyglot.TranslationStore translationStore, Ponyglot.ICultureSource cultureSource, System.Func<Ponyglot.TranslationStore, Ponyglot.ICultureSource, Ponyglot.ITranslatorFactory> translationFactoryProvider, System.Collections.Generic.IEnumerable<Ponyglot.Sources.ICatalogSource> sources) { }
        public Ponyglot.ICultureSource CultureSource { get; }
        public bool IsInitialized { get; }
        public Ponyglot.TranslationStore Store { get; }
        public Ponyglot.ITranslatorFactory TranslatorFactory { get; }
        public System.Threading.Tasks.Task InitializeAsync(System.Threading.CancellationToken cancellationToken = default) { }
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
namespace Ponyglot.Sources
{
    public class FileSystemCatalogSource : Ponyglot.Sources.StreamCatalogSource
    {
        public FileSystemCatalogSource(Ponyglot.Sources.ICatalogReader catalogReader, System.IO.DirectoryInfo rootDirectory) { }
        public FileSystemCatalogSource(Ponyglot.Sources.ICatalogReader catalogReader, System.IO.DirectoryInfo rootDirectory, Ponyglot.Sources.FileSystemCatalogSourceOptions options) { }
        protected Ponyglot.Sources.FileSystemCatalogSourceOptions Options { get; }
        protected System.IO.DirectoryInfo RootDirectory { get; }
        [System.Runtime.CompilerServices.AsyncIteratorStateMachine(typeof(Ponyglot.Sources.FileSystemCatalogSource.<EnumerateResourcesAsync>d__8))]
        protected override System.Collections.Generic.IAsyncEnumerable<Ponyglot.Sources.StreamResource> EnumerateResourcesAsync([System.Runtime.CompilerServices.EnumeratorCancellation] System.Threading.CancellationToken cancellationToken = default) { }
    }
    public class FileSystemCatalogSourceOptions
    {
        public FileSystemCatalogSourceOptions() { }
        public System.Func<System.IO.FileInfo, string>? CatalogNameResolver { get; set; }
        public System.IO.EnumerationOptions FileSearchOptions { get; set; }
        public System.Func<System.IO.FileInfo, bool>? Filter { get; set; }
    }
    public interface ICatalogReader
    {
        System.Threading.Tasks.ValueTask<Ponyglot.Catalog?> TryReadCatalogAsync(Ponyglot.Sources.StreamResource resource, System.Threading.CancellationToken cancellationToken);
    }
    public interface ICatalogSource
    {
        System.Collections.Generic.IAsyncEnumerable<Ponyglot.Catalog> LoadCatalogsAsync(System.Threading.CancellationToken cancellationToken = default);
    }
    public abstract class StreamCatalogSource : Ponyglot.Sources.ICatalogSource
    {
        protected StreamCatalogSource(Ponyglot.Sources.ICatalogReader catalogReader) { }
        protected abstract System.Collections.Generic.IAsyncEnumerable<Ponyglot.Sources.StreamResource> EnumerateResourcesAsync(System.Threading.CancellationToken cancellationToken = default);
        [System.Runtime.CompilerServices.AsyncIteratorStateMachine(typeof(Ponyglot.Sources.StreamCatalogSource.<LoadCatalogsAsync>d__2))]
        public System.Collections.Generic.IAsyncEnumerable<Ponyglot.Catalog> LoadCatalogsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] System.Threading.CancellationToken cancellationToken = default) { }
    }
    public abstract class StreamResource
    {
        protected StreamResource(string uid, string name, string catalogName) { }
        public string CatalogName { get; }
        public string Name { get; }
        public string Uid { get; }
        public abstract System.Threading.Tasks.ValueTask<System.IO.Stream> OpenAsync(System.Threading.CancellationToken cancellationToken);
        public override string ToString() { }
    }
}