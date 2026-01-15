using System;
using System.IO;

namespace Ponyglot.Sources;

/// <summary>
/// Defines the options for the <see cref="FileSystemCatalogSource"/>.
/// </summary>
public class FileSystemCatalogSourceOptions
{
    /// <summary>
    /// Deifnes the options to use when searching for files. Default is the default of <see cref="EnumerationOptions"/> with <see cref="EnumerationOptions.RecurseSubdirectories"/> set to <c>true</c>.
    /// </summary>
    public EnumerationOptions FileSearchOptions { get; set; } = new() { RecurseSubdirectories = true };

    /// <summary>
    /// The optional filter that can be used to exclude specific files. Default is <c>null</c>.
    /// </summary>
    /// <remarks>
    /// The filter has to be fast and deterministic. It will be called once for each discovered file.
    /// </remarks>
    public Func<FileInfo, bool>? Filter { get; set; }

    /// <summary>
    /// The optional resolver that can extract the domain name from the file name. Default is <c>null</c>.
    /// </summary>
    /// <remarks>
    /// When <c>null</c>, the default behavior is applied.
    /// <para>
    /// The default domain name extraction behavior is to use the part of the file name before the first dot,
    /// or the file name if it contains no dot (the initial dot is skipped) is used as the domain name.
    /// </para>
    /// For example, all the following file names would result in the domain name "my-domain":
    /// <list type="bullet">
    ///     <item>
    ///         <term>my-domain.en.txt</term>
    ///     </item>
    ///     <item>
    ///         <term>my-domain</term>
    ///     </item>
    ///     <item>
    ///         <term>.my-domain.en.txt</term>
    ///     </item>
    ///     <item>
    ///         <term>.my-domain</term>
    ///     </item>
    /// </list>
    /// </remarks>
    public Func<FileInfo, string>? DomainResolver { get; set; }
}