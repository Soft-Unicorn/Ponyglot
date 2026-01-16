using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Ponyglot.Sources;

/// <summary>
/// A <see cref="ICatalogSource"/> that discovers and reads catalogs from embedded assembly resources.
/// </summary>
public class AssemblyCatalogSource : StreamCatalogSource
{
    private readonly string _defaultCatalogName;
    private readonly string _assemblyFullName;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssemblyCatalogSource"/> class.
    /// </summary>
    /// <param name="catalogReader">The <see cref="ICatalogReader"/> that reads catalogs from streams.</param>
    /// <param name="assembly">The <see cref="System.Reflection.Assembly"/> containing the embedded resources catalogs to discover.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="catalogReader"/>
    /// -or-
    /// <paramref name="assembly"/>
    /// is <c>null</c>.
    /// </exception>
    public AssemblyCatalogSource(ICatalogReader catalogReader, Assembly assembly)
        : this(catalogReader, assembly, new AssemblyCatalogSourceOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AssemblyCatalogSource"/> class.
    /// </summary>
    /// <param name="catalogReader">The <see cref="ICatalogReader"/> that reads catalogs from streams.</param>
    /// <param name="assembly">The <see cref="System.Reflection.Assembly"/> containing the embedded resources catalogs to discover.</param>
    /// <param name="options">The <see cref="AssemblyCatalogSourceOptions"/> that can be used to customize the source behavior.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="catalogReader"/>
    /// -or-
    /// <paramref name="assembly"/>
    /// -or-
    /// <paramref name="options"/>
    /// is <c>null</c>.
    /// </exception>
    public AssemblyCatalogSource(ICatalogReader catalogReader, Assembly assembly, AssemblyCatalogSourceOptions options)
        : base(catalogReader)
    {
        Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        _defaultCatalogName = assembly.GetName().Name ?? "";
        _assemblyFullName = assembly.FullName ?? "";
    }

    /// <summary>
    /// The <see cref="System.Reflection.Assembly"/> containing the embedded resources catalogs to discover.
    /// </summary>
    protected Assembly Assembly { get; }

    /// <summary>
    /// The <see cref="AssemblyCatalogSourceOptions"/> that can be used to customize the source behavior.
    /// </summary>
    public AssemblyCatalogSourceOptions Options { get; }

    /// <inheritdoc/>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously - File system enumeration is synchronous only
    protected override async IAsyncEnumerable<StreamResource> EnumerateResourcesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
#pragma warning restore CS1998
    {
        foreach (var resourceName in Assembly.GetManifestResourceNames())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (Options.Filter == null || Options.Filter(Assembly, resourceName))
            {
                var uid = $"EmbeddedResource:Assembly={_assemblyFullName};Resource={resourceName}";
                var catalogName = Options.CatalogNameResolver?.Invoke(Assembly, resourceName) ?? _defaultCatalogName;
                yield return new Resource(Assembly, uid, resourceName, catalogName);
            }
        }
    }

    private class Resource : StreamResource
    {
        private readonly Assembly _assembly;

        public Resource(Assembly assembly, string uid, string name, string catalogName)
            : base(uid, name, catalogName)
        {
            _assembly = assembly;
        }

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The caller is responsible for closing the stream.")]
        public override ValueTask<Stream> OpenAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_assembly.GetManifestResourceStream(Name) is { } stream)
            {
                return new ValueTask<Stream>(stream);
            }

            throw new FileNotFoundException($"Resource '{Name}' not found in assembly '{_assembly.FullName}'.", Name);
        }
    }
}