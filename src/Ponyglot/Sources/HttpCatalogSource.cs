using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Ponyglot.Sources;

/// <summary>
/// A <see cref="ICatalogSource"/> that discovers and reads catalogs from an HTTP server using a manifest file.
/// </summary>
/// <remarks>
///     <para>
///     This source downloads a manifest from <see cref="ManifestUri"/> and interprets it as a list of catalog URLs.
///     Each listed URL is then fetched and passed to the configured <see cref="ICatalogReader"/>.
///     </para>
///     <para>
///     When requesting a manifest, the <c>Accept</c> header is set to the media types specified by the
///     <see cref="HttpCatalogSourceOptions.ManifestReaders"/>, unless the <see cref="HttpClient"/> already
///     defines default <c>Accept</c> header(s). In that case, the manifest request’s <c>Accept</c>
///     header is left unchanged.
///     </para>
///     <para>
///     The manifest format is automatically selected from the HTTP <c>Content-Type</c> response header.
///     See <see cref="HttpCatalogSourceOptions.ManifestReaders"/> for more details.
///     </para>
/// </remarks>
public class HttpCatalogSource : StreamCatalogSource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpCatalogSource"/> class.
    /// </summary>
    /// <param name="catalogReader">The <see cref="ICatalogReader"/> that reads catalogs from streams.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use to perform http requests.</param>
    /// <param name="manifestUri">The <see cref="Uri"/> of the manifest that provides the list of catalog resources to load.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="catalogReader"/>
    /// -or-
    /// <paramref name="httpClient"/>
    /// -or-
    /// <paramref name="manifestUri"/>
    /// is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="manifestUri"/> is not an absolute URI.
    /// </exception>
    public HttpCatalogSource(ICatalogReader catalogReader, HttpClient httpClient, Uri manifestUri)
        : this(catalogReader, httpClient, manifestUri, new HttpCatalogSourceOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpCatalogSource"/> class.
    /// </summary>
    /// <param name="catalogReader">The <see cref="ICatalogReader"/> that reads catalogs from streams.</param>
    /// <param name="httpClient">The <see cref="HttpClient"/> to use to perform http requests.</param>
    /// <param name="manifestUri">The <see cref="Uri"/> of the manifest that provides the list of catalog resources to load.</param>
    /// <param name="options">The <see cref="HttpCatalogSourceOptions"/> that can be used to customize the source behavior.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="catalogReader"/>
    /// -or-
    /// <paramref name="httpClient"/>
    /// -or-
    /// <paramref name="manifestUri"/>
    /// -or-
    /// <paramref name="options"/>
    /// is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="manifestUri"/> is not an absolute URI.
    /// -or-
    /// <paramref name="options"/> does not specify at least one manifest reader.
    /// -or-
    /// <paramref name="options"/> defines a <c>null</c> manifest reader.
    /// </exception>
    public HttpCatalogSource(ICatalogReader catalogReader, HttpClient httpClient, Uri manifestUri, HttpCatalogSourceOptions options)
        : base(catalogReader)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Client = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ManifestUri = manifestUri ?? throw new ArgumentNullException(nameof(manifestUri));

        if (!ManifestUri.IsAbsoluteUri)
        {
            throw new ArgumentException($"The manifest URI ({manifestUri}) must be absolute.", nameof(manifestUri));
        }

        if (options.ManifestReaders.Count == 0)
        {
            throw new ArgumentException("At least one manifest reader must be specified.", nameof(options));
        }

        var nullManifestReaderIndex = options.ManifestReaders.IndexOf(null!);
        if (nullManifestReaderIndex >= 0)
        {
            throw new ArgumentException($"The manifest reader at index {nullManifestReaderIndex} is null.", nameof(options));
        }
    }

    /// <summary>
    /// The <see cref="HttpClient"/> to use to perform http requests.
    /// </summary>
    protected HttpClient Client { get; }

    /// <summary>
    /// The <see cref="Uri"/> of the manifest that provides the list of catalog resources to load.
    /// </summary>
    protected Uri ManifestUri { get; }

    /// <summary>
    /// The <see cref="HttpCatalogSourceOptions"/> that can be used to customize the source behavior.
    /// </summary>
    protected HttpCatalogSourceOptions Options { get; }

    /// <inheritdoc/>
    protected override async IAsyncEnumerable<StreamResource> EnumerateResourcesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Request the manifest (HttpCompletionOption.ResponseHeadersRead => no buffering on the body) 
        using var request = CreateManifestRequest();
        using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        // Select the correct reader
        var reader = SelectManifestReader(response);

        // Read, decode and yield
        var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using (responseStream.ConfigureAwait(false))
        {
            var catalogNameResolver = Options.CatalogNameResolver ?? ResolveCatalogName;
            var count = 0;
            await foreach (var rawUri in reader.ReadAsync(responseStream, cancellationToken).ConfigureAwait(false))
            {
                ++count;
                if (!Uri.TryCreate(rawUri, UriKind.Absolute, out var uri))
                {
                    throw new UriFormatException($"The catalog URI ({rawUri}) is not a valid URI.");
                }

                if (Options.Filter != null && !Options.Filter(uri))
                {
                    continue;
                }

                ValidateCatalogUri(count, uri);

                var catalogName = catalogNameResolver(uri);
                var uid = $"Http:Uri={uri}";
                yield return new Resource(Client, uid, uri, catalogName, Options.ConfigureCatalogRequest);
            }
        }
    }

    /// <summary>
    /// Selects the manifest reader to use based on the response media type.
    /// </summary>
    /// <param name="response">The <see cref="HttpResponseMessage"/></param>
    /// <returns>The selected <see cref="IHttpCatalogManifestReader"/></returns>
    /// <exception cref="NotSupportedException">No matching reader could be found.</exception>
    private IHttpCatalogManifestReader SelectManifestReader(HttpResponseMessage response)
    {
        var responseMediaType = response.Content.Headers.ContentType?.MediaType;
        var reader = Options.ManifestReaders.FirstOrDefault(r => r.MediaTypes.Contains(responseMediaType, StringComparer.OrdinalIgnoreCase));
        if (reader == null)
        {
            var acceptedMediaTypes = string.Join(", ", GetAcceptedManifestMediaTypes().Select(m => $"'{m}'"));
            throw new NotSupportedException($"Unsupported manifest Content-Type '{responseMediaType ?? "<null>"}'. Expected one of {acceptedMediaTypes}.");
        }

        return reader;
    }

    /// <summary>
    /// Creates the HTTP request to fetch the manifest.
    /// </summary>
    /// <returns>The <see cref="HttpRequestMessage"/> to use to fetch the manifest.</returns>
    private HttpRequestMessage CreateManifestRequest()
    {
        HttpRequestMessage? request = null;
        try
        {
            // Creates the request and set the Accept headers to the media-types accepted by the manifest readers (if no Accept header is configured by default on the HTTP client)
            request = new HttpRequestMessage(HttpMethod.Get, ManifestUri);
            if (Client.DefaultRequestHeaders.Accept.Count == 0)
            {
                foreach (var mediaType in GetAcceptedManifestMediaTypes())
                {
                    request.Headers.Accept.ParseAdd(mediaType);
                }
            }

            // Allow for request customization
            Options.ConfigureManifestRequest?.Invoke(request);

            return request;
        }
        catch
        {
            request?.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Validates the specified catalog URI.
    /// </summary>
    /// <param name="count">The count of catalog URIs processed so far (including the one being validated).</param>
    /// <param name="uri">The <see cref="Uri"/> to validate.</param>
    /// <exception cref="InvalidOperationException">The URI does not match the expectations.</exception>
    private void ValidateCatalogUri(int count, Uri uri)
    {
        if (count > Options.MaxCatalogs)
        {
            throw new InvalidOperationException($"Maximum number of catalogs ({Options.MaxCatalogs}) exceeded. " +
                                                $"Set {nameof(Options.MaxCatalogs)} to a higher value to allow more catalogs.");
        }

        if (Options.SameOrigin && Uri.Compare(uri, ManifestUri, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) != 0)
        {
            throw new InvalidOperationException($"The catalog URI ({uri}) is not of the same origin as the manifest URI ({ManifestUri}). " +
                                                $"Set {nameof(Options.SameOrigin)} to false to allow cross-origin catalog URIs.");
        }
    }

    /// <summary>
    /// Builds the list of accepted media types for the manifest.
    /// </summary>
    /// <returns>The collection of accepted media types for the manifest.</returns>
    private IEnumerable<string> GetAcceptedManifestMediaTypes() => Options.ManifestReaders.SelectMany(r => r.MediaTypes).Distinct(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The default catalog name resolver implementation.
    /// </summary>
    /// <param name="uri">The <see cref="Uri"/> of the catalog resource to resolve.</param>
    /// <returns>The catalog name for the specified catalog resource.</returns>
    private static string ResolveCatalogName(Uri uri)
    {
        var name = Uri.UnescapeDataString(uri.Segments[^1].Trim('/'));

        var startIndex = name.StartsWith('.') ? 1 : 0;
        return name.IndexOf('.', startIndex) switch
        {
            < 0 => name[startIndex..],
            var i => name[startIndex..i],
        };
    }

    private sealed class Resource : StreamResource
    {
        private readonly HttpClient _httpClient;
        private readonly Uri _uri;
        private readonly Action<HttpRequestMessage>? _configureRequest;

        public Resource(HttpClient httpClient, string uid, Uri uri, string catalogName, Action<HttpRequestMessage>? configureRequest)
            : base(uid, uri.ToString(), catalogName)
        {
            _httpClient = httpClient;
            _uri = uri;
            _configureRequest = configureRequest;
        }

        public override async ValueTask<Stream> OpenAsync(CancellationToken cancellationToken)
        {
            // Create the request
            using var request = new HttpRequestMessage(HttpMethod.Get, _uri);
            _configureRequest?.Invoke(request);

            // Get the response (HttpCompletionOption.ResponseHeadersRead => no buffering on the body)
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            try
            {
                response.EnsureSuccessStatusCode();
                var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                return stream;
            }
            catch
            {
                response.Dispose();
                throw;
            }
        }
    }
}