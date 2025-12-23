[assembly: System.CLSCompliant(true)]
[assembly: System.Reflection.AssemblyMetadata("RepositoryUrl", "https://github.com/Soft-Unicorn/Ponyglot")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Ponyglot.Tests")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETStandard,Version=v2.0", FrameworkDisplayName=".NET Standard 2.0")]
namespace Ponyglot
{
    public sealed class DefaultCultureSource : Ponyglot.ICultureSource
    {
        public DefaultCultureSource() { }
        public System.Globalization.CultureInfo Culture { get; set; }
    }
    public interface ICultureSource
    {
        System.Globalization.CultureInfo Culture { get; set; }
    }
    public interface ITranslationStore
    {
        string? Find(string domain, System.Globalization.CultureInfo culture, string? context, string messageId);
        string? FindPlural(string domain, System.Globalization.CultureInfo culture, string? context, long count, string messageId, string pluralId);
    }
    public interface ITranslator
    {
        string? Context { get; }
        string Domain { get; }
        Ponyglot.ITranslator ForCulture(System.Globalization.CultureInfo culture);
        string N(long count, string messageId, string pluralId, params object[]? args);
        string T(string messageId, params object[]? args);
    }
    public interface ITranslatorFactory
    {
        Ponyglot.ITranslator Create(string domain, string? context);
        Ponyglot.ITranslator Create<T>();
    }
}