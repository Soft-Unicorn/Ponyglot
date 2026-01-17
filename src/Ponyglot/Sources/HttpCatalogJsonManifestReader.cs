using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Ponyglot.Sources;

/// <summary>
/// A <see cref="IHttpCatalogManifestReader"/> that reads a list of catalog URLs from a JSON format.
/// </summary>
/// <remarks>
/// <para>
/// The format expects a JSON array containing absolute URL strings. Empty and <c>null</c> strings are ignored.
/// </para>
/// The response content type should be:
/// <list type="bullet">
///     <item>
///         <description>
///             <c>application/json</c>
///         </description>
///     </item>
/// </list>
/// </remarks>
/// <example>
///     <code lang="text">
///     [
///       "https://cdn.example.com/i18n/messages.en.ext",
///       "https://cdn.example.com/i18n/messages.fr.ext",
///     ]
///     </code>
/// </example>
public class HttpCatalogJsonManifestReader : IHttpCatalogManifestReader
{
    private static readonly JsonSerializerOptions JsonOptions = new() { MaxDepth = 2, ReadCommentHandling = JsonCommentHandling.Disallow };

    /// <inheritdoc/>
    public IEnumerable<string> MediaTypes { get; } = [MediaTypeNames.Application.Json];

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> ReadAsync(Stream stream, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        await foreach (var uri in JsonSerializer.DeserializeAsyncEnumerable<string>(stream, JsonOptions, cancellationToken).ConfigureAwait(false))
        {
            if (uri?.Length > 0)
            {
                yield return uri;
            }
        }
    }
}