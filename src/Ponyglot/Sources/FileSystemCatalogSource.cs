using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Ponyglot.Sources;

/// <summary>
/// A <see cref="ICatalogSource"/> that discovers and reads catalogs from the file system.
/// </summary>
public class FileSystemCatalogSource : StreamCatalogSource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemCatalogSource"/> class.
    /// </summary>
    /// <param name="catalogReader">The <see cref="ICatalogReader"/> that reads catalogs from streams.</param>
    /// <param name="rootDirectory">The <see cref="DirectoryInfo"/> that points to the directory containing the catalogs to discover.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="catalogReader"/>
    /// -or-
    /// <paramref name="rootDirectory"/>
    /// is <c>null</c>.
    /// </exception>
    public FileSystemCatalogSource(ICatalogReader catalogReader, DirectoryInfo rootDirectory)
        : this(catalogReader, rootDirectory, new FileSystemCatalogSourceOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemCatalogSource"/> class.
    /// </summary>
    /// <param name="catalogReader">The <see cref="ICatalogReader"/> that reads catalogs from streams.</param>
    /// <param name="rootDirectory">The <see cref="DirectoryInfo"/> that points to the directory containing the catalogs to discover.</param>
    /// <param name="options">The <see cref="FileSystemCatalogSourceOptions"/> that can be used to customize the source behavior.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="catalogReader"/>
    /// -or-
    /// <paramref name="rootDirectory"/>
    /// -or-
    /// <paramref name="options"/>
    /// is <c>null</c>.
    /// </exception>
    public FileSystemCatalogSource(ICatalogReader catalogReader, DirectoryInfo rootDirectory, FileSystemCatalogSourceOptions options)
        : base(catalogReader)
    {
        RootDirectory = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// The <see cref="DirectoryInfo"/> that points to the directory containing the catalogs to discover.
    /// </summary>
    protected DirectoryInfo RootDirectory { get; }

    /// <summary>
    /// The <see cref="FileSystemCatalogSourceOptions"/> that can be used to customize the source behavior.
    /// </summary>
    protected FileSystemCatalogSourceOptions Options { get; }

    /// <inheritdoc/>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously - File system enumeration is synchronous only
    protected override async IAsyncEnumerable<StreamResource> EnumerateResourcesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
#pragma warning restore CS1998
    {
        var domainResolver = Options.DomainResolver ?? ResolveDomain;

        foreach (var file in RootDirectory.EnumerateFiles("*", Options.FileSearchOptions))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (file.Length > 0 && (Options.Filter == null || Options.Filter(file)))
            {
                var uid = $"FileSystem:File={file.FullName}";
                var domain = domainResolver(file);
                yield return new Resource(uid, file.FullName, domain);
            }
        }
    }

    /// <summary>
    /// The default domain resolver implementation.
    /// </summary>
    /// <param name="file">The <see cref="FileInfo"/> for which the domain should be resolved.</param>
    /// <returns>The domain name for the specified file.</returns>
    private static string ResolveDomain(FileInfo file)
    {
        var startIndex = file.Name.StartsWith('.') ? 1 : 0;
        return file.Name.IndexOf('.', startIndex) switch
        {
            < 0 => file.Name[startIndex..],
            var i => file.Name[startIndex..i],
        };
    }

    /// <summary>
    /// The <see cref="StreamResource"/> that represents a file.
    /// </summary>
    private class Resource : StreamResource
    {
        private static readonly FileStreamOptions OpenOptions = new() { Mode = FileMode.Open, Access = FileAccess.Read, Share = FileShare.Read, Options = FileOptions.Asynchronous };

        /// <inheritdoc/>
        public Resource(string uid, string name, string domain)
            : base(uid, name, domain)
        {
        }

        /// <inheritdoc/>
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The caller is responsible for closing the stream.")]
        public override ValueTask<Stream> OpenAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Stream stream = new FileStream(Name, OpenOptions);
            return ValueTask.FromResult(stream);
        }
    }
}