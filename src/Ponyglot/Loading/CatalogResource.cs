using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ponyglot.Loading;

/// <summary>
/// Describes a translation catalog resource.
/// </summary>
public abstract class CatalogResource
{
    /// <summary>
    /// Initialize a new instance of the <see cref="CatalogResource"/> class.
    /// </summary>
    /// <param name="uri">The <see cref="Uri"/> that identifies the catalog.</param>
    /// <exception cref="ArgumentNullException"><paramref name="uri"/> is <c>null</c>.</exception>
    protected CatalogResource(Uri uri)
    {
        Uri = uri ?? throw new ArgumentNullException(nameof(uri));
    }

    /// <summary>
    /// The <see cref="Uri"/> that identifies the catalog.
    /// </summary>
    public Uri Uri { get; }

    /// <summary>
    /// Opens the catalog for reading.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns></returns>
    public abstract Task<Stream> OpenAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public override string ToString() => Uri.ToString();
}