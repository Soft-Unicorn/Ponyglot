using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using AwesomeAssertions;
using NSubstitute;
using Ponyglot.Tests._TestUtils;
using Xunit;

namespace Ponyglot.Tests;

public class TranslationStoreTest
{
    private readonly TranslationStore _sut = new();

    [Fact]
    public void Initialize_CatalogsSpecified_InitializeTheIndex()
    {
        // Arrange
        var catalog1 = new TestCatalog("my-domain-a", "qps-Ploc");
        var catalog2 = new TestCatalog("my-domain-a", "en");
        var catalog3 = new TestCatalog("my-domain-b", "qps-Ploc");
        var catalog4 = new TestCatalog("", "qps-Ploc");

        // Act
        _sut.Initialize([catalog1, catalog2, catalog3, catalog4]);

        // Assert
        var catalogsIndex = _sut.GetCatalogsIndex();
        catalogsIndex.Should().BeEquivalentTo(new Dictionary<string, Dictionary<string, ICatalog>>
        {
            ["my-domain-a"] = new()
            {
                [catalog1.Culture.Name] = catalog1,
                [catalog2.Culture.Name] = catalog2,
            },
            ["my-domain-b"] = new()
            {
                [catalog3.Culture.Name] = catalog3,
            },
            [""] = new()
            {
                [catalog4.Culture.Name] = catalog4,
            },
        });
    }

    [Fact]
    public void Initialize_TwoCatalogsWithSameDomainAndCulture_Throws()
    {
        // Arrange
        var catalog1 = new TestCatalog("my-domain", "qps-Ploc");
        var catalog2 = new TestCatalog("my-domain", "qps-Ploc");

        // Act
        var action = () => _sut.Initialize([catalog1, catalog2]);

        // Assert
        action.Should().ThrowExactly<ArgumentException>()
            .WithParameterName("catalogs")
            .WithMessage("*more than one*'my-domain'*'qps-Ploc'*");
    }

    [Fact]
    public void Initialize_AlreadyInitialized_Throws()
    {
        // Arrange
        _sut.Initialize([new TestCatalog()]);

        // Act
        var action = () => _sut.Initialize([new TestCatalog()]);

        // Assert
        action.Should().ThrowExactly<InvalidOperationException>().WithMessage("*already*initialized*");
    }

    [Fact]
    public void Initialize_CatalogsIsNull_Throws()
    {
        // Arrange

        // Act
        var action = () => _sut.Initialize(null!);

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName("catalogs");
    }

    [Theory]
    [CombinatorialData]
    public void TryGet_CatalogForExactCultureExists_CallsTheCorrectCatalog(
        [CombinatorialValues("", "my-domain", "MY-DOMAIN")] string domain,
        [CombinatorialValues("", "my-context")] string context)
    {
        // Arrange
        var catalog = Substitute.ForPartsOf<TestCatalog>(domain.ToLowerInvariant(), "zh-Hant");
        _sut.Initialize([
            new TestCatalog(domain, "zh-Hant-TW"),
            new TestCatalog(domain, "zh"),
            new TestCatalog(domain, "en"),
            new TestCatalog("other-domain", "zh-Hant-TW"),
            catalog,
        ]);

        // Act
        _sut.TryGet(domain, new CultureInfo("zh-Hant"), context, "my-message", out _);

        // Assert
        catalog.Received(1).TryGet(context, "my-message", out _);
    }

    [Fact]
    public void TryGet_CatalogForBaseCultureAvailable_CallsTheCorrectCatalogs()
    {
        // Arrange
        var catalog1 = Substitute.ForPartsOf<TestCatalog>("my-domain", "zh-Hant-TW");
        var catalog2 = Substitute.ForPartsOf<TestCatalog>("my-domain", "zh-Hant");
        var catalog3 = Substitute.ForPartsOf<TestCatalog>("my-domain", "zh");
        _sut.Initialize([catalog1, catalog2, catalog3]);

        // Act
        _sut.TryGet("my-domain", new CultureInfo("zh-Hant-TW"), "my-context", "my-message", out _);

        // Assert
        Received.InOrder(() =>
        {
            catalog1.TryGet("my-context", "my-message", out _);
            catalog2.TryGet("my-context", "my-message", out _);
            catalog3.TryGet("my-context", "my-message", out _);
        });
    }

    [Theory]
    [InlineData("Exact culture")]
    [InlineData("First parent culture")]
    [InlineData("Second parent culture")]
    public void TryGet_CatalogFoundAndTranslationExists_ReturnsTrueAndTheTranslation(string textFoundOn)
    {
        // Arrange
        var catalog1 = new TestCatalog("my-domain", "zh-Hant-TW");
        var catalog2 = new TestCatalog("my-domain", "zh-Hant");
        var catalog3 = new TestCatalog("my-domain", "zh");

        _sut.Initialize([catalog1, catalog2, catalog3]);

        switch (textFoundOn)
        {
            case "Exact culture":
                catalog1.Add("my-message", "my-translation");
                break;
            case "First parent culture":
                catalog2.Add("my-message", "my-translation");
                break;
            case "Second parent culture":
                catalog3.Add("my-message", "my-translation");
                break;
        }

        // Act
        var result = _sut.TryGet("my-domain", new CultureInfo("zh-Hant-TW"), "", "my-message", out var translation);

        // Assert
        result.Should().BeTrue();
        translation.Should().Be("my-translation");
    }

    [Fact]
    public void TryGet_CatalogFoundAndTranslationDoesNotExist_ReturnsFalseAndNull()
    {
        // Arrange
        var catalog = new TestCatalog("my-domain", "qps-Ploc");

        _sut.Initialize([catalog]);

        // Act
        var result = _sut.TryGet("my-domain", new CultureInfo("qps-Ploc"), "", "my-message", out var translation);

        // Assert
        result.Should().BeFalse();
        translation.Should().BeNull();
    }

    [Fact]
    public void TryGet_CatalogNotFound_ReturnsFalseAndNull()
    {
        // Arrange
        var catalog = new TestCatalog("other-domain", "en")
        {
            ["my-message"] = "my-translation",
        };

        _sut.Initialize([catalog]);

        // Act
        var result = _sut.TryGet("my-domain", new CultureInfo("qps-Ploc"), "", "my-message", out var translation);

        // Assert
        result.Should().BeFalse();
        translation.Should().BeNull();
    }

    [Theory]
    [InlineData("domain")]
    [InlineData("culture")]
    [InlineData("messageId")]
    public void TryGet_ArgumentIsNull_Throws(string parameterName)
    {
        // Arrange
        _sut.Initialize([new TestCatalog()]);

        // Act
        var action = () => _sut.TryGet(
            parameterName == "domain" ? null! : "my-domain",
            parameterName == "culture" ? null! : new CultureInfo("qps-Ploc"),
            "my-context",
            parameterName == "messageId" ? null! : "my-message",
            out _);

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName(parameterName);
    }

    [Fact]
    public void TryGet_CatalogsNotSet_Throws()
    {
        // Arrange

        // Act
        var action = () => _sut.TryGet("my-domain", new CultureInfo("qps-Ploc"), "my-context", "my-message", out _);

        // Assert
        action.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage($"*store*not been initialized*`{nameof(_sut.Initialize)}` method*");
    }

    [Theory]
    [CombinatorialData]
    public void TryGetPlural_CatalogForExactCultureExists_CallsTheCorrectCatalog(
        [CombinatorialValues("", "my-domain", "MY-DOMAIN")] string domain,
        [CombinatorialValues("", "my-context")] string context)
    {
        // Arrange
        var catalog = Substitute.ForPartsOf<TestCatalog>(domain.ToLowerInvariant(), "zh-Hant");
        _sut.Initialize([
            new TestCatalog(domain, "zh-Hant-TW"),
            new TestCatalog(domain, "zh"),
            new TestCatalog(domain, "en"),
            new TestCatalog("other-domain", "zh-Hant-TW"),
            catalog,
        ]);

        // Act
        _sut.TryGetPlural(domain, new CultureInfo("zh-Hant"), context, 123, "my-message", out _);

        // Assert
        catalog.Received(1).TryGetPlural(context, 123, "my-message", out _);
    }

    [Fact]
    public void TryGetPlural_CatalogForBaseCultureAvailable_CallsTheCorrectCatalogs()
    {
        // Arrange
        var catalog1 = Substitute.ForPartsOf<TestCatalog>("my-domain", "zh-Hant-TW");
        var catalog2 = Substitute.ForPartsOf<TestCatalog>("my-domain", "zh-Hant");
        var catalog3 = Substitute.ForPartsOf<TestCatalog>("my-domain", "zh");
        _sut.Initialize([catalog1, catalog2, catalog3]);

        // Act
        _sut.TryGetPlural("my-domain", new CultureInfo("zh-Hant-TW"), "my-context", 123, "my-message", out _);

        // Assert
        Received.InOrder(() =>
        {
            catalog1.TryGetPlural("my-context", 123, "my-message", out _);
            catalog2.TryGetPlural("my-context", 123, "my-message", out _);
            catalog3.TryGetPlural("my-context", 123, "my-message", out _);
        });
    }

    [Theory]
    [InlineData("Exact culture")]
    [InlineData("First parent culture")]
    [InlineData("Second parent culture")]
    public void TryGetPlural_CatalogFoundAndTranslationExists_ReturnsTrueAndTheTranslation(string textFoundOn)
    {
        // Arrange
        var catalog1 = new TestCatalog("my-domain", "zh-Hant-TW");
        var catalog2 = new TestCatalog("my-domain", "zh-Hant");
        var catalog3 = new TestCatalog("my-domain", "zh");

        _sut.Initialize([catalog1, catalog2, catalog3]);

        switch (textFoundOn)
        {
            case "Exact culture":
                catalog1.Add("my-message", "my-translation");
                break;
            case "First parent culture":
                catalog2.Add("my-message", "my-translation");
                break;
            case "Second parent culture":
                catalog3.Add("my-message", "my-translation");
                break;
        }

        // Act
        var result = _sut.TryGetPlural("my-domain", new CultureInfo("zh-Hant-TW"), "", 123, "my-message", out var translation);

        // Assert
        result.Should().BeTrue();
        translation.Should().Be("my-translation");
    }

    [Fact]
    public void TryGetPlural_CatalogFoundAndTranslationDoesNotExist_ReturnsFalseAndNull()
    {
        // Arrange
        var catalog = new TestCatalog("my-domain", "qps-Ploc");

        _sut.Initialize([catalog]);

        // Act
        var result = _sut.TryGetPlural("my-domain", new CultureInfo("qps-Ploc"), "", 123, "my-message", out var translation);

        // Assert
        result.Should().BeFalse();
        translation.Should().BeNull();
    }

    [Fact]
    public void TryGetPlural_CatalogNotFound_ReturnsFalseAndNull()
    {
        // Arrange
        var catalog = new TestCatalog("other-domain", "en")
        {
            ["my-message"] = "my-translation",
        };

        _sut.Initialize([catalog]);

        // Act
        var result = _sut.TryGetPlural("my-domain", new CultureInfo("qps-Ploc"), "", 123, "my-message", out var translation);

        // Assert
        result.Should().BeFalse();
        translation.Should().BeNull();
    }

    [Theory]
    [InlineData("domain")]
    [InlineData("culture")]
    [InlineData("context")]
    [InlineData("messageId")]
    public void TryGetPlural_ArgumentIsNull_Throws(string parameterName)
    {
        // Arrange
        _sut.Initialize([new TestCatalog()]);

        // Act
        var action = () => _sut.TryGetPlural(
            parameterName == "domain" ? null! : "my-domain",
            parameterName == "culture" ? null! : new CultureInfo("qps-Ploc"),
            parameterName == "context" ? null! : "my-context",
            123,
            parameterName == "messageId" ? null! : "my-message",
            out _);

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName(parameterName);
    }

    [Fact]
    public void TryGetPlural_CatalogsNotSet_Throws()
    {
        // Arrange

        // Act
        var action = () => _sut.TryGetPlural("my-domain", new CultureInfo("qps-Ploc"), "my-context", 123, "my-message", out _);

        // Assert
        action.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage($"*store*not been initialized*`{nameof(_sut.Initialize)}` method*");
    }

    #region Helpers

    // ReSharper disable all

    internal class TestCatalog : Dictionary<string, string>, ICatalog
    {
        public TestCatalog(string? domain = null, string? cultureName = null)
        {
            Domain = domain ?? "x-domain";
            Culture = new CultureInfo(cultureName ?? "qps-Ploc");
            Uri = new Uri($"test://{Domain}/{Culture.Name}");
        }

        public Uri Uri { get; }
        public string Domain { get; }
        public CultureInfo Culture { get; }

        public virtual bool TryGet(string? context, string messageId, [NotNullWhen(true)] out string? translation)
        {
            if (TryGetValue(messageId, out translation))
            {
                return true;
            }

            translation = null;
            return false;
        }

        public virtual bool TryGetPlural(string? context, long count, string messageId, [NotNullWhen(true)] out string? translation)
        {
            if (TryGetValue(messageId, out translation))
            {
                return true;
            }

            translation = null;
            return false;
        }

        public override string ToString() => $"{Domain}/{Culture.Name}";
    }

    #endregion
}