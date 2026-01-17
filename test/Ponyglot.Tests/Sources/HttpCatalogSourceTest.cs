using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.Extensions;
using Ponyglot.Sources;
using Ponyglot.Tests._TestUtils;
using Xunit;

namespace Ponyglot.Tests.Sources;

public class HttpCatalogSourceTest
{
    private readonly Uri _manifestUri;
    private readonly ICatalogReader _catalogReader;
    private readonly HttpCatalogSourceOptions _options;
    private readonly InMemoryHttpClient _httpClient;
    private HttpCatalogSourceDouble _sut;

    public HttpCatalogSourceTest()
    {
        _manifestUri = new Uri("https://my.server/i18n/manifest", UriKind.Absolute);
        _catalogReader = Substitute.For<ICatalogReader>();
        _options = new HttpCatalogSourceOptions { ManifestReaders = [CreateManifestReader(mediaTypes: ["x-reader/default"])] };

        _httpClient = new InMemoryHttpClient();
        _httpClient.ForRoute(_manifestUri).Respond(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = { Headers = { ContentType = req.Headers.Accept.FirstOrDefault() } },
        });

        _sut = new HttpCatalogSourceDouble(_catalogReader, _httpClient, _manifestUri, _options);
    }

    [Theory]
    [InlineData(true, "catalogReader")]
    [InlineData(true, "httpClient")]
    [InlineData(true, "manifestUri")]
    [InlineData(true, "options")]
    [InlineData(false, "catalogReader")]
    [InlineData(false, "httpClient")]
    [InlineData(false, "manifestUri")]
    public void Constructor_ArgumentIsNull_Throws(bool overloadWithOptions, string parameterName)
    {
        // Arrange

        // Act
        Action action = overloadWithOptions switch
        {
            true => () => _ = new HttpCatalogSource(
                parameterName == "catalogReader" ? null! : _catalogReader,
                parameterName == "httpClient" ? null! : _httpClient,
                parameterName == "manifestUri" ? null! : _manifestUri,
                parameterName == "options" ? null! : _options),
            false => () => _ = new HttpCatalogSource(
                parameterName == "catalogReader" ? null! : _catalogReader,
                parameterName == "httpClient" ? null! : _httpClient,
                parameterName == "manifestUri" ? null! : _manifestUri),
        };

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName(parameterName);
    }

    [Fact]
    public void Constructor_ManifestUriIsRelative_Throws()
    {
        // Arrange
        var uri = new Uri("/my-relative-uri", UriKind.Relative);

        // Act
        var action = () => new HttpCatalogSource(_catalogReader, _httpClient, uri, _options);

        // Assert
        action.Should().ThrowExactly<ArgumentException>()
            .WithParameterName("manifestUri")
            .WithMessage("The manifest URI (/my-relative-uri) must be absolute.*");
    }

    [Fact]
    public void Constructor_OptionsHasNoManifestReaders_Throws()
    {
        // Arrange
        _options.ManifestReaders = [];

        // Act
        var action = () => new HttpCatalogSource(_catalogReader, _httpClient, _manifestUri, _options);

        // Assert
        action.Should().ThrowExactly<ArgumentException>()
            .WithParameterName("options")
            .WithMessage("At least one manifest reader must be specified.*");
    }

    [Fact]
    public void Constructor_OptionsDefinesNullManifestReader_Throws()
    {
        // Arrange
        _options.ManifestReaders =
        [
            Substitute.For<IHttpCatalogManifestReader>(),
            Substitute.For<IHttpCatalogManifestReader>(),
            Substitute.For<IHttpCatalogManifestReader>(),
            null!,
            Substitute.For<IHttpCatalogManifestReader>(),
        ];

        // Act
        var action = () => new HttpCatalogSource(_catalogReader, _httpClient, _manifestUri, _options);

        // Assert
        action.Should().ThrowExactly<ArgumentException>()
            .WithParameterName("options")
            .WithMessage("The manifest reader at index 3 is null.*");
    }

    [Fact]
    public void Client_Created_DefaultsTheConstructorValue()
    {
        // Arrange

        // Act
        var client = _sut.Client;

        // Assert
        client.Should().BeSameAs(_httpClient);
    }

    [Fact]
    public void ManifestUri_Created_DefaultsTheConstructorValue()
    {
        // Arrange

        // Act
        var manifestUri = _sut.ManifestUri;

        // Assert
        manifestUri.Should().BeSameAs(_manifestUri);
    }

    [Fact]
    public void Options_CreatedWithOptions_ReturnsTheConstructorValue()
    {
        // Arrange
        _sut = new HttpCatalogSourceDouble(_catalogReader, _httpClient, _manifestUri, _options);

        // Act
        var options = _sut.Options;

        // Assert
        options.Should().BeSameAs(_options);
    }

    [Fact]
    public void Options_CreatedWithoutOptions_ReturnsTheDefaultOptions()
    {
        // Arrange
        _sut = new HttpCatalogSourceDouble(_catalogReader, _httpClient, _manifestUri);

        // Act
        var options = _sut.Options;

        // Assert
        options.Should().BeEquivalentTo(new HttpCatalogSourceOptions());
    }

    [Theory]
    [CombinatorialData]
    public async Task EnumerateResourcesAsync_Always_IssuesCorrectManifestRequest(bool clientHasDefaultAcceptHeaders)
    {
        // Arrange
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        if (clientHasDefaultAcceptHeaders)
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("x-reader/b"));

        _options.ManifestReaders =
        [
            CreateManifestReader(mediaTypes: ["x-reader/a", "x-reader/b"]),
            CreateManifestReader(mediaTypes: ["x-reader/c", "x-reader/d"]),
        ];

        // Act
        await _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).ConsumeAsync();

        // Assert

        _httpClient.ReceivedRequests.Should().SatisfyRespectively(single =>
        {
            single.Method.Should().Be(HttpMethod.Get);
            single.RequestUri.Should().BeEquivalentTo(_manifestUri);
            single.Headers.Should().ContainKey("Accept").WhoseValue.Should().BeEquivalentTo(clientHasDefaultAcceptHeaders switch
            {
                true => ["x-reader/b"],
                false => ["x-reader/a", "x-reader/b", "x-reader/c", "x-reader/d"],
            });
        });
    }

    [Theory]
    [CombinatorialData]
    public async Task EnumerateResourcesAsync_MatchingReaderFound_CallsFirstMatchingReaderWithManifestContent(bool acceptHeaderCaseDiffers)
    {
        // Arrange
        _httpClient.ForRoute(_manifestUri)
            .RespondOk(
                contentType: acceptHeaderCaseDiffers ? "X-READER/B" : "x-reader/b",
                content: "my-manifest-content");

        var readerA = CreateManifestReader(mediaTypes: ["x-reader/a"]);
        var readerB1 = CreateManifestReader(mediaTypes: ["x-reader/b"]);
        var readerB2 = CreateManifestReader(mediaTypes: ["x-reader/b"]);
        var readerC = CreateManifestReader(mediaTypes: ["x-reader/c"]);
        _options.ManifestReaders = [readerA, readerB1, readerB2, readerC];

        string? readBodyContent = null;
        readerB1.Configure().ReadAsync(Arg.Do<Stream>(x => readBodyContent = x.AsString()), TestContext.Current.CancellationToken);

        // Act
        await _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).ConsumeAsync();

        // Assert

        readerB1.Received(1).ReadAsync(Arg.Any<Stream>(), TestContext.Current.CancellationToken);
        readBodyContent.Should().Be("my-manifest-content");

        readerA.DidNotReceiveWithAnyArgs().ReadAsync(default!, Arg.Any<CancellationToken>());
        readerB2.DidNotReceiveWithAnyArgs().ReadAsync(default!, Arg.Any<CancellationToken>());
        readerC.DidNotReceiveWithAnyArgs().ReadAsync(default!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnumerateResourcesAsync_CatalogResourcesFound_ReturnsResourcesWithCorrectAttributes()
    {
        // Arrange
        var uris = new[]
        {
            new Uri(_manifestUri, ".my-uri-a.x.y.z").ToString(),
            new Uri(_manifestUri, ".my-uri-b").ToString(),
            new Uri(_manifestUri, "my-uri-c.x.y.z").ToString(),
            new Uri(_manifestUri, "my-uri-d").ToString(),
        };

        _options.ManifestReaders = [CreateManifestReader(mediaTypes: ["x-reader/a"], urls: uris)];

        // Act
        var result = await _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync();

        // Assert
        result.OrderBy(r => r.Name, StringComparer.Ordinal).Should().SatisfyRespectively(
            first =>
            {
                first.Uid.Should().Be($"Http:Uri={uris[0]}");
                first.Name.Should().Be(uris[0]);
                first.CatalogName.Should().Be("my-uri-a");
            },
            second =>
            {
                second.Uid.Should().Be($"Http:Uri={uris[1]}");
                second.Name.Should().Be(uris[1]);
                second.CatalogName.Should().Be("my-uri-b");
            },
            third =>
            {
                third.Uid.Should().Be($"Http:Uri={uris[2]}");
                third.Name.Should().Be(uris[2]);
                third.CatalogName.Should().Be("my-uri-c");
            },
            fourth =>
            {
                fourth.Uid.Should().Be($"Http:Uri={uris[3]}");
                fourth.Name.Should().Be(uris[3]);
                fourth.CatalogName.Should().Be("my-uri-d");
            });
    }

    [Fact]
    public async Task EnumerateResourcesAsync_FilterSet_CallsFilterForEachResource()
    {
        // Arrange
        var uris = new[]
        {
            new Uri(_manifestUri, "my-uri-a").ToString(),
            new Uri(_manifestUri, "my-uri-b").ToString(),
            new Uri(_manifestUri, "my-uri-c").ToString(),
            new Uri(_manifestUri, "my-uri-d").ToString(),
        };

        _options.ManifestReaders = [CreateManifestReader(mediaTypes: ["x-reader/a"], urls: uris)];

        var filteredUris = new List<Uri>();
        _options.Filter = u =>
        {
            filteredUris.Add(u);
            return true;
        };

        // Act
        await _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync();

        // Assert
        filteredUris.Should().BeEquivalentTo(
        [
            new Uri(_manifestUri, "my-uri-a"),
            new Uri(_manifestUri, "my-uri-b"),
            new Uri(_manifestUri, "my-uri-c"),
            new Uri(_manifestUri, "my-uri-d"),
        ]);
    }

    [Fact]
    public async Task EnumerateResourcesAsync_FilterSet_ReturnsOnlyResourcesThatSatisfyTheFilter()
    {
        // Arrange
        var uris = new[]
        {
            new Uri(_manifestUri, "my-uri-a").ToString(),
            new Uri(_manifestUri, "my-uri-b").ToString(),
            new Uri(_manifestUri, "my-uri-c").ToString(),
            new Uri(_manifestUri, "my-uri-d").ToString(),
        };

        _options.ManifestReaders = [CreateManifestReader(mediaTypes: ["x-reader/a"], urls: uris)];
        _options.Filter = f => f.AbsolutePath.EndsWith('a') || f.AbsolutePath.EndsWith('c');

        // Act
        var result = await _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync();

        // Assert
        result.Select(r => r.Name).Should().BeEquivalentTo(uris[0], uris[2]);
    }

    [Fact]
    public async Task EnumerateResourcesAsync_CatalogNameResolverSet_CallsResolverForEachResource()
    {
        // Arrange
        var uris = new[]
        {
            new Uri(_manifestUri, "my-uri-a").ToString(),
            new Uri(_manifestUri, "my-uri-b").ToString(),
            new Uri(_manifestUri, "my-uri-c").ToString(),
            new Uri(_manifestUri, "my-uri-d").ToString(),
        };

        _options.ManifestReaders = [CreateManifestReader(mediaTypes: ["x-reader/a"], urls: uris)];

        var resolvedUris = new List<Uri>();
        _options.CatalogNameResolver = u =>
        {
            resolvedUris.Add(u);
            return "";
        };

        // Act
        await _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync();

        // Assert
        resolvedUris.Should().BeEquivalentTo(
        [
            new Uri(_manifestUri, "my-uri-a"),
            new Uri(_manifestUri, "my-uri-b"),
            new Uri(_manifestUri, "my-uri-c"),
            new Uri(_manifestUri, "my-uri-d"),
        ]);
    }

    [Fact]
    public async Task EnumerateResourcesAsync_CatalogNameResolverSet_ReturnsResourcesWithTheResolvedCatalogName()
    {
        // Arrange
        var uris = new[]
        {
            new Uri(_manifestUri, "my-uri-a").ToString(),
            new Uri(_manifestUri, "my-uri-b").ToString(),
            new Uri(_manifestUri, "my-uri-c").ToString(),
            new Uri(_manifestUri, "my-uri-d").ToString(),
        };

        _options.ManifestReaders = [CreateManifestReader(mediaTypes: ["x-reader/a"], urls: uris)];
        _options.CatalogNameResolver = u => $"my-catalog-{u.AbsolutePath[^1]}";

        // Act
        var result = await _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync();

        // Assert
        result.Select(r => r.CatalogName).Should().BeEquivalentTo(
            "my-catalog-a",
            "my-catalog-b",
            "my-catalog-c",
            "my-catalog-d");
    }

    [Fact]
    public async Task EnumerateResourcesAsync_ConfigureManifestRequestSet_CallsTheDelegate()
    {
        // Arrange
        List<string>? acceptHeadersDuringConfiguration = null;
        _options.ConfigureCatalogRequest = _ => throw new Exception("The catalog request configuration delegate should not have been called !");
        _options.ConfigureManifestRequest = r =>
        {
            acceptHeadersDuringConfiguration = r.Headers.Accept.Select(a => a.ToString()).ToList();
            r.Headers.Add("x-custom", "x-custom-value");
        };

        // Act
        await _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync();

        // Assert
        _httpClient.ReceivedRequests.Should().SatisfyRespectively(
            [single => single.Headers.Should().ContainKey("x-custom").WhoseValue.Should().BeEquivalentTo("x-custom-value")],
            because: "The configure delegate should have been called before the request is sent.");

        acceptHeadersDuringConfiguration.Should().NotBeNull(because: "The configure delegate should have been called");
        acceptHeadersDuringConfiguration.Should().NotBeEmpty(because: "The configure delegate should have been called after the default configuration has been applied to the request.");
    }

    [Theory]
    [InlineData(false, "https://my.server/my-manifest", "https://my.server/my-catalog", false)]
    [InlineData(false, "https://my.server/my-manifest", "https://my.server:443/my-catalog", false)]
    [InlineData(false, "https://my.server/my-manifest", "https://other.server/my-catalog", false)]
    [InlineData(false, "https://my.server/my-manifest", "https://other.server:999/my-catalog", false)]
    [InlineData(false, "https://my.server:111/my-manifest", "https://other.server:222/my-catalog", false)]
    [InlineData(true, "https://my.server/my-manifest", "https://my.server/my-catalog", false)]
    [InlineData(true, "https://my.server/my-manifest", "https://my.server:443/my-catalog", false)]
    [InlineData(true, "https://my.server/my-manifest", "https://other.server/my-catalog", true)]
    [InlineData(true, "https://my.server/my-manifest", "https://other.server:999/my-catalog", true)]
    [InlineData(true, "https://my.server:111/my-manifest", "https://other.server:222/my-catalog", true)]
    public async Task EnumerateResourcesAsync_SameOriginFalse_ThrowIfTrueAndOriginDiffers(bool sameOrigin, string manifestUri, string catalogUri, bool shouldThrow)
    {
        // Arrange
        _options.SameOrigin = sameOrigin;
        _sut = new HttpCatalogSourceDouble(_catalogReader, _httpClient, new Uri(manifestUri), _options);

        _httpClient.ForRoute(manifestUri).RespondOk(contentType: "x-reader/a");

        _options.ManifestReaders = [CreateManifestReader(mediaTypes: ["x-reader/a"], urls: [catalogUri])];

        // Act
        var action = () => _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).ConsumeAsync();

        // Assert
        if (shouldThrow)
        {
            await action.Should().ThrowExactlyAsync<InvalidOperationException>()
                .WithMessage($"The catalog URI ({new Uri(catalogUri)}) is not of the same origin as the manifest URI ({new Uri(manifestUri)}). " +
                             $"Set {nameof(_options.SameOrigin)} to false to allow cross-origin catalog URIs.");
        }
        else
        {
            await action.Should().NotThrowAsync();
        }
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task EnumerateResourcesAsync_ManifestHttpResponseHasNonSuccessStatusCode_Throws(HttpStatusCode statusCode)
    {
        // Arrange
        _httpClient.ForRoute(_manifestUri).Respond(statusCode, null);

        // Act
        var action = () => _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).ConsumeAsync();

        // Assert
        await action.Should().ThrowExactlyAsync<HttpRequestException>().WithMessage($"*{(int)statusCode}*");
    }

    [Fact]
    public async Task EnumerateResourcesAsync_MatchingReaderNotFound_Throws()
    {
        // Arrange
        _httpClient.ForRoute(_manifestUri).RespondOk(contentType: "x-unknown/bad", content: "my-manifest-content");

        var readerA = CreateManifestReader(mediaTypes: ["x-reader/a"]);
        var readerB = CreateManifestReader(mediaTypes: ["x-reader/b"]);
        var readerC = CreateManifestReader(mediaTypes: ["x-reader/c"]);
        _options.ManifestReaders = [readerA, readerB, readerC];

        // Act
        var action = () => _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).ConsumeAsync();

        // Assert
        await action.Should().ThrowExactlyAsync<NotSupportedException>()
            .WithMessage("Unsupported manifest Content-Type 'x-unknown/bad'. Expected one of 'x-reader/a', 'x-reader/b', 'x-reader/c'.");
    }

    [Fact]
    public async Task EnumerateResourcesAsync_ReaderReturnsInvalidUri_Throws()
    {
        // Arrange

        _options.ManifestReaders = [CreateManifestReader(mediaTypes: ["x-reader/a"], urls: ["not-a-uri"])];

        // Act
        var action = () => _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).ConsumeAsync();

        // Assert
        await action.Should().ThrowExactlyAsync<UriFormatException>()
            .WithMessage("The catalog URI (not-a-uri) is not a valid URI.");
    }

    [Fact]
    public async Task EnumerateResourcesAsync_MaxCatalogsExceeded_Throws()
    {
        // Arrange
        _options.MaxCatalogs = 123;

        var uris = Enumerable.Range(1, 124).Select(i => $"https://my.server/i18n/messages-{i}.en").ToArray();
        _options.ManifestReaders = [CreateManifestReader(mediaTypes: ["x-reader/a"], urls: uris)];

        // Act
        var action = () => _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).ConsumeAsync();

        // Assert
        await action.Should().ThrowExactlyAsync<InvalidOperationException>()
            .WithMessage($"Maximum number of catalogs (123) exceeded. Set {nameof(_options.MaxCatalogs)} to a higher value to allow more catalogs.");
    }

    [Theory]
    [InlineData("Request creation")]
    [InlineData("HTTP client request")]
    public async Task EnumerateResourcesAsync_ErrorOccurs_Throws(string failurePoint)
    {
        // Arrange
        _options.MaxCatalogs = 123;
        _options.ManifestReaders = [CreateManifestReader(mediaTypes: ["text/plain"], urls: Enumerable.Range(1, 124).Select(i => $"https://my.server/i18n/messages-{i}.en").ToArray())];

        var error = new Exception("💥Kaboom💥");
        switch (failurePoint)
        {
            case "Request creation":
                _options.ManifestReaders[0].MediaTypes.ThrowsForAnyArgs(error);
                break;
            case "HTTP client request":
                _httpClient.ForRoute(_manifestUri).Respond(_ => throw error);
                break;
            default:
                throw new NotSupportedException($"Unknown failure point '{failurePoint}'.");
        }

        _httpClient.ForRoute(_manifestUri).Respond(_ => throw error);

        // Act
        var action = () => _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).ConsumeAsync();

        // Assert
        (await action.Should().ThrowExactlyAsync<Exception>())
            .Which.Should().BeSameAs(error);
    }

    [Fact]
    public async Task ReturnedResourceOpenAsync_RequestIsSuccessful_OpensTheCorrectUri()
    {
        // Arrange
        var uri = new Uri(_manifestUri, ".my-uri-a.b.c");

        _options.ManifestReaders = [CreateManifestReader(mediaTypes: ["x-reader/a"], urls: [uri.ToString()])];

        _httpClient.ForRoute(uri).RespondOk(contentType: "x-reader/a", content: "my-catalog-content");

        var resource = (await _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync()).Single();

        _httpClient.ReceivedRequests.Clear();

        // Act
        await using var stream = await resource.OpenAsync(TestContext.Current.CancellationToken);

        // Assert
        stream.AsString().Should().Be("my-catalog-content");

        _httpClient.ReceivedRequests.Should().SatisfyRespectively(single =>
        {
            single.Headers.Should().BeEmpty();
            single.Method.Should().Be(HttpMethod.Get);
            single.RequestUri.Should().Be(uri);
        });
    }

    [Fact]
    public async Task ReturnedResourceOpenAsync_ConfigureCatalogRequestSet_CallsTheDelegate()
    {
        // Arrange
        var uri = new Uri(_manifestUri, ".my-uri-a.b.c");

        _options.ManifestReaders = [CreateManifestReader(mediaTypes: ["x-reader/a"], urls: [uri.ToString()])];
        _options.ConfigureCatalogRequest = r => { r.Headers.Add("x-custom", "x-custom-value"); };

        _httpClient.ForRoute(uri).RespondOk(contentType: "x-reader/a");

        var resource = (await _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync()).Single();

        _options.ConfigureManifestRequest = _ => throw new Exception("The catalog request configuration delegate should not have been called !");

        _httpClient.ReceivedRequests.Clear();

        // Act
        await using var stream = await resource.OpenAsync(TestContext.Current.CancellationToken);

        // Assert
        _httpClient.ReceivedRequests.Should().SatisfyRespectively(
            [single => single.Headers.Should().ContainKey("x-custom").WhoseValue.Should().BeEquivalentTo("x-custom-value")],
            because: "The configure delegate should have been called before the request is sent.");
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task ReturnedResourceOpenAsync_RequestIsNotSuccessful_Throws(HttpStatusCode statusCode)
    {
        // Arrange
        var uri = new Uri(_manifestUri, ".my-uri-a.b.c");

        _options.ManifestReaders = [CreateManifestReader(mediaTypes: ["x-reader/a"], urls: [uri.ToString()])];

        _httpClient.ForRoute(uri).Respond(statusCode, null);

        // Act
        var result = await _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync();

        // Assert
        _httpClient.ReceivedRequests.Clear();

        var action = () => result.Single().OpenAsync(TestContext.Current.CancellationToken).AsTask();
        await action.Should().ThrowExactlyAsync<HttpRequestException>().WithMessage($"*{(int)statusCode}*");
    }

    #region Helpers

    private static IHttpCatalogManifestReader CreateManifestReader(string[] mediaTypes, string[]? urls = null)
    {
        var manifestReader = Substitute.For<IHttpCatalogManifestReader>();
        manifestReader.Configure().MediaTypes.Returns(mediaTypes.ToArray());
        manifestReader.Configure().ReadAsync(default!, Arg.Any<CancellationToken>()).ReturnsForAnyArgs((urls ?? []).ToAsyncEnumerable());
        return manifestReader;
    }

    private sealed class HttpCatalogSourceDouble : HttpCatalogSource
    {
        public HttpCatalogSourceDouble(ICatalogReader catalogReader, HttpClient httpClient, Uri manifestUri)
            : base(catalogReader, httpClient, manifestUri)
        {
        }

        public HttpCatalogSourceDouble(ICatalogReader catalogReader, HttpClient httpClient, Uri manifestUri, HttpCatalogSourceOptions options)
            : base(catalogReader, httpClient, manifestUri, options)
        {
        }

        public new HttpClient Client => base.Client;
        public new Uri ManifestUri => base.ManifestUri;
        public new HttpCatalogSourceOptions Options => base.Options;

        public new IAsyncEnumerable<StreamResource> EnumerateResourcesAsync(CancellationToken cancellationToken = default)
            => base.EnumerateResourcesAsync(cancellationToken);
    }

    private class InMemoryHttpClient : HttpClient
    {
        private readonly InMemoryHttpMessageHandler _messageHandler;

        public InMemoryHttpClient()
            : base(CreateHandler(out var handler))
        {
            _messageHandler = handler;
        }

        public HttpResponseBuilder ForRoute(Uri uri) => new(r => _messageHandler.Routes[uri] = r);
        public HttpResponseBuilder ForRoute(string uri) => new(r => _messageHandler.Routes[new Uri(uri)] = r);
        public List<HttpReceivedRequest> ReceivedRequests => _messageHandler.ReceivedRequests;

        private static InMemoryHttpMessageHandler CreateHandler(out InMemoryHttpMessageHandler handler) => handler = new InMemoryHttpMessageHandler();

        private class InMemoryHttpMessageHandler : HttpMessageHandler
        {
            public Dictionary<Uri, Func<HttpRequestMessage, HttpResponseMessage>> Routes { get; } = [];
            public List<HttpReceivedRequest> ReceivedRequests { get; } = [];

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                ReceivedRequests.Add(new HttpReceivedRequest
                {
                    Headers = request.Headers,
                    Method = request.Method,
                    RequestUri = request.RequestUri,
                });

                if (request.RequestUri == null)
                {
                    throw new InvalidOperationException("RequestUri is null.");
                }

                if (!Routes.TryGetValue(request.RequestUri, out var responseFactory))
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
                    {
                        Content = new StringContent($"No route registered for '{request.RequestUri}'.", Encoding.UTF8),
                    });
                }

                return Task.FromResult(responseFactory(request));
            }
        }
    }

    private class HttpResponseBuilder(Action<Func<HttpRequestMessage, HttpResponseMessage>> setResponseBuilder)
    {
        public void RespondOk(string contentType, string content = "OK") => Respond(HttpStatusCode.OK, contentType, content);

        public void Respond(HttpStatusCode statusCode, string? contentType, string content = "")
        {
            setResponseBuilder(_ => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8)
                {
                    Headers = { ContentType = string.IsNullOrEmpty(contentType) ? null : MediaTypeHeaderValue.Parse(contentType) },
                },
            });
        }

        public void Respond(Func<HttpRequestMessage, HttpResponseMessage> responseBuilder) => setResponseBuilder(responseBuilder);
    }

    private class HttpReceivedRequest
    {
        public required IEnumerable<KeyValuePair<string, IEnumerable<string>>> Headers { get; init; }
        public required HttpMethod Method { get; init; }
        public required Uri? RequestUri { get; init; }
    }

    #endregion
}