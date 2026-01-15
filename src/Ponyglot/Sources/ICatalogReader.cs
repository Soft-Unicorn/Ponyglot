using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ponyglot.Sources;

/// <summary>
/// Reads and parses <see cref="Catalog"/> instances from a <see cref="StreamResource"/>.
/// </summary>
public interface ICatalogReader
{
    /// <summary>
    /// Attempts to read a <see cref="Catalog"/> from the given <paramref name="resource"/>.
    /// </summary>
    /// <param name="resource">The <see cref="StreamResource"/> that potentially stores a catalog.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>The loaded <see cref="Catalog"/>; <c>null</c> if the <paramref name="resource"/> does not represent a catalog.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="resource"/> is <c>null</c>.</exception>
    ValueTask<Catalog?> TryReadCatalogAsync(StreamResource resource, CancellationToken cancellationToken);
}