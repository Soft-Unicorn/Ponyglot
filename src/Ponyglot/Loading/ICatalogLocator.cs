using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ponyglot.Loading;

/// <summary>
/// Locates translation catalogs.
/// </summary>
public interface ICatalogLocator
{
    /// <summary>
    /// Searches for translation catalogs.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>The collection of <see cref="CatalogResource"/> that identifies the found catalogs.</returns>
    Task<IReadOnlyCollection<CatalogResource>> FindCatalogsAsync(CancellationToken cancellationToken = default);
}