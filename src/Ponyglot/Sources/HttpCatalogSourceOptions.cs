using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;

namespace Ponyglot.Sources;

/// <summary>
/// Defines the options for the <see cref="HttpCatalogSource"/>.
/// </summary>
public class HttpCatalogSourceOptions
{
    private static readonly IHttpCatalogManifestReader[] DefaultReaders = [new HttpCatalogTextManifestReader(), new HttpCatalogJsonManifestReader()];

    /// <summary>
    /// The list of <see cref="IHttpCatalogManifestReader"/> to use for parsing the manifest in the order they should be evaluated.
    /// The first reader matching the content type of the response is used for decoding.
    /// The default value is [<see cref="HttpCatalogTextManifestReader"/>, <see cref="HttpCatalogJsonManifestReader"/>].
    /// </summary>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "This should be modifiable and assignable.")]
    public Collection<IHttpCatalogManifestReader> ManifestReaders { get; set; } = [..DefaultReaders];

    /// <summary>
    /// Requires the catalog source to be same-origin with the manifest URI. Default is <c>true</c>.
    /// </summary>
    public bool SameOrigin { get; set; } = true;

    /// <summary>
    /// The maximum number of catalogs that are allowed to be discovered using the manifest. Default is <c>1000</c>.
    /// </summary>
    public int MaxCatalogs { get; set; } = 1000;

    /// <summary>
    /// Optional filter applied on manifest URLs. Must be fast and must not perform I/O.
    /// </summary>
    public Func<Uri, bool>? Filter { get; set; }

    /// <summary>
    /// The optional resolver that can extract the catalog name from the <see cref="Uri"/> of the catalog resource. Default is <c>null</c>.
    /// </summary>
    /// <remarks>
    /// When <c>null</c>, the default behavior is applied.
    /// <para>
    /// The default catalog name extraction behavior uses the last URI path segment (<c>uri.Segments[^1]</c>) as the file name.
    /// Then the part of the file name before the first dot or the file name if it contains no dot (the initial dot is skipped) is used as the catalog name.
    /// </para>
    /// For example, all the following file names would result in the catalog name "my-catalog":
    /// <list type="bullet">
    ///     <item>
    ///         <term>my-catalog.en.txt</term>
    ///     </item>
    ///     <item>
    ///         <term>my-catalog</term>
    ///     </item>
    ///     <item>
    ///         <term>.my-catalog.en.txt</term>
    ///     </item>
    ///     <item>
    ///         <term>.my-catalog</term>
    ///     </item>
    /// </list>
    /// </remarks>
    public Func<Uri, string>? CatalogNameResolver { get; set; }

    /// <summary>
    /// The optional action that can be used to configure the <see cref="HttpRequestMessage"/> used to fetch the manifest.
    /// </summary>
    /// <remarks>This delegate is invoked with the <see cref="HttpRequestMessage"/> just before the HTTP request is sent.</remarks>
    public Action<HttpRequestMessage>? ConfigureManifestRequest { get; set; }

    /// <summary>
    /// The optional action that can be used to configure the <see cref="HttpRequestMessage"/> used to fetch a catalog resource.
    /// </summary>
    /// <remarks>This delegate is invoked with the <see cref="HttpRequestMessage"/> just before the HTTP request is sent.</remarks>
    public Action<HttpRequestMessage>? ConfigureCatalogRequest { get; set; }
}