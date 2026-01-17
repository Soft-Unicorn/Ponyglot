using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Ponyglot.Sources;

/// <summary>
/// Reads a list of catalog URLs from an HTTP manifest.
/// </summary>
public interface IHttpCatalogManifestReader
{
    /// <summary>
    /// Returns the list of media types handled by this reader.
    /// </summary>
    public IEnumerable<string> MediaTypes { get; }

    /// <summary>
    /// Reads the list of catalog URLs from the specified <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to read from.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the operation.</param>
    /// <returns>A collection of raw unparsed uri.</returns>
    IAsyncEnumerable<string> ReadAsync(Stream stream, CancellationToken cancellationToken = default);
}