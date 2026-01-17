using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Ponyglot.Sources;

/// <summary>
/// A <see cref="IHttpCatalogManifestReader"/> that reads a list of catalog URLs from a text based HTTP manifest format according to
/// <a href="https://www.rfc-editor.org/rfc/rfc2483.html#section-5">RFC2483 section 5</a>.
/// </summary>
/// <remarks>
/// <para>
/// The format expects one absolute URI string per line with CRLF as the line separator.
/// Lines starting with <c>#</c> are treated as comments and ignored.
/// Empty lines are tolerated and ignored.
/// </para>
/// The response content type should be one of:
/// <list type="bullet">
///     <item>
///         <description>
///             <c>text/uri-list</c>
///         </description>
///     </item>
///     <item>
///         <description>
///             <c>text/plain</c>
///         </description>
///     </item>
/// </list>
/// </remarks>
/// <example>
///     <code lang="text">
///     # This is a comment
///     https://cdn.example.com/i18n/messages.en.ext
///     https://cdn.example.com/i18n/messages.fr.ext
///     </code>
/// </example>
public class HttpCatalogTextManifestReader : IHttpCatalogManifestReader
{
    /// <inheritdoc/>
    public IEnumerable<string> MediaTypes { get; } = ["text/uri-list", MediaTypeNames.Text.Plain];

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> ReadAsync(Stream stream, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            if (!(string.IsNullOrWhiteSpace(line) || line.StartsWith('#')))
            {
                yield return line.Trim();
            }
        }
    }
}