using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Ponyglot.Sources;

/// <summary>
/// A <see cref="ICatalogSource"/> that discovers and reads catalogs from a <see cref="Stream"/>.
/// </summary>
public abstract class StreamCatalogSource : ICatalogSource
{
    private readonly ICatalogReader _catalogReader;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamCatalogSource"/> class.
    /// </summary>
    /// <param name="catalogReader">The <see cref="ICatalogReader"/> that reads catalogs from streams.</param>
    protected StreamCatalogSource(ICatalogReader catalogReader)
    {
        _catalogReader = catalogReader ?? throw new ArgumentNullException(nameof(catalogReader));
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<Catalog> LoadCatalogsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var resource in EnumerateResourcesAsync(cancellationToken).ConfigureAwait(false))
        {
            var catalog = await _catalogReader.TryReadCatalogAsync(resource, cancellationToken).ConfigureAwait(false);
            if (catalog != null)
            {
                yield return catalog;
            }
        }
    }

    /// <summary>
    /// Lists the available catalog resources.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>The collection of <see cref="StreamResource"/> that identifies the found catalogs.</returns>
    protected abstract IAsyncEnumerable<StreamResource> EnumerateResourcesAsync(CancellationToken cancellationToken = default);
}