using System.Collections.Generic;
using System.Threading;

namespace Ponyglot.Sources;

/// <summary>
/// A source that can provide translation catalogs.
/// </summary>
public interface ICatalogSource
{
    /// <summary>
    /// Loads the catalogs from the current source.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>A collection of <see cref="Catalog"/> loaded from the current source.</returns>
    IAsyncEnumerable<Catalog> LoadCatalogsAsync(CancellationToken cancellationToken = default);
}