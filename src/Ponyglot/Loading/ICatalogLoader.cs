using System.Threading;
using System.Threading.Tasks;

namespace Ponyglot.Loading;

/// <summary>
/// A class that can load a catalog from a <see cref="CatalogResource"/>.
/// </summary>
public interface ICatalogLoader
{
    /// <summary>
    /// Determines whether this loader can load the specified catalog resource.
    /// </summary>
    /// <param name="catalogResource">The <see cref="CatalogResource"/> to load.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns><c>true</c> if the loader can load the specified catalog resource; otherwise, <c>false</c>.</returns>
    Task<bool> CanLoadAsync(CatalogResource catalogResource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a catalog from the specified resource.
    /// </summary>
    /// <param name="catalogResource">The <see cref="CatalogResource"/> to load.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>The loaded <see cref="Catalog"/>.</returns>
    Task<Catalog> LoadAsync(CatalogResource catalogResource, CancellationToken cancellationToken = default);
}