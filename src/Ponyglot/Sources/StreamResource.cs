using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ponyglot.Sources;

/// <summary>
/// Represents a potential stream-based resource that can be read by a <see cref="ICatalogReader"/>.
/// </summary>
public abstract class StreamResource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StreamResource"/> class.
    /// </summary>
    /// <param name="uid">An identifier that uniquely identifies the resource.</param>
    /// <param name="name">The name of the resource. This can, for example, be a file name, an HTTP address, or an embedded resource file name.</param>
    /// <param name="catalogName">The default catalog name to use if this information cannot be derived from the resource data.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="uid"/>
    /// -or-
    /// <paramref name="name"/>
    /// -or-
    /// <paramref name="catalogName"/>
    /// is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="uid"/>
    /// -or-
    /// <paramref name="name"/>
    /// is empty.
    /// </exception>
    protected StreamResource(string uid, string name, string catalogName)
    {
        ArgumentException.ThrowIfNullOrEmpty(uid);
        ArgumentException.ThrowIfNullOrEmpty(name);
        Uid = uid;
        Name = name;
        CatalogName = catalogName ?? throw new ArgumentNullException(nameof(catalogName));
    }

    /// <summary>
    /// An identifier that uniquely identifies the resource.
    /// </summary>
    public string Uid { get; }

    /// <summary>
    /// The name of the resource. This can, for example, be a file name, an HTTP address, or an embedded resource file name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The default catalog name to use if this information cannot be derived from the resource data.
    /// </summary>
    public string CatalogName { get; }

    /// <summary>
    /// Opens the resource <see cref="Stream"/> for reading.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>A <see cref="Stream"/> that can be used to read the resource.</returns>
    /// <remarks>The caller has the responsibility to close the returned stream.</remarks>
    public abstract ValueTask<Stream> OpenAsync(CancellationToken cancellationToken);

    /// <inheritdoc/>
    public override string ToString() => Uid;
}