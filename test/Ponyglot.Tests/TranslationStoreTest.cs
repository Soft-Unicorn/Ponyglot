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
        var catalog1 = new TestCatalog("my-catalog-a", "qps-Ploc");
        var catalog2 = new TestCatalog("my-catalog-a", "en");
        var catalog3 = new TestCatalog("my-catalog-b", "qps-Ploc");
        var catalog4 = new TestCatalog("", "qps-Ploc");

        // Act
        _sut.Initialize([catalog1, catalog2, catalog3, catalog4]);

        // Assert
        var catalogsIndex = _sut.GetCatalogsIndex();
        catalogsIndex.Should().BeEquivalentTo(new Dictionary<string, Dictionary<string, Catalog>>
        {
            ["my-catalog-a"] = new()
            {
                [catalog1.Culture.Name] = catalog1,
                [catalog2.Culture.Name] = catalog2,
            },
            ["my-catalog-b"] = new()
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
    public void Initialize_TwoCatalogsWithSameCatalogNameAndCulture_Throws()
    {
        // Arrange
        var catalog1 = new TestCatalog("my-catalog", "qps-Ploc", "my-catalog-1");
        var catalog2 = new TestCatalog("my-catalog", "qps-Ploc", "my-catalog-2");

        // Act
        var action = () => _sut.Initialize([catalog1, catalog2]);

        // Assert
        action.Should().ThrowExactly<ArgumentException>()
            .WithParameterName("catalogs")
            .WithMessage("The list of catalogs contains more than one catalog with the name 'my-catalog' and culture 'qps-Ploc': 'my-catalog-1' and 'my-catalog-2'.*");
    }

    [Fact]
    public void Initialize_AlreadyInitialized_Throws()
    {
        // Arrange
        _sut.Initialize([new TestCatalog()]);

        // Act
        var action = () => _sut.Initialize([new TestCatalog()]);

        // Assert
        action.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage("The translation store has already been initialized.");
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

    [Fact]
    public void Initialize_CatalogsContainsANullItem_ValidationFails()
    {
        // Arrange
        var catalogs = new[]
        {
            new TestCatalog("my-catalog-1", "qps-Ploc", "my-catalog-1"),
            new TestCatalog("my-catalog-2", "qps-Ploc", "my-catalog-2"),
            new TestCatalog("my-catalog-3", "qps-Ploc", "my-catalog-2"),
            new TestCatalog("my-catalog-4", "qps-Ploc", "my-catalog-2"),
            new TestCatalog("my-catalog-5", "qps-Ploc", "my-catalog-2"),
            null!,
            new TestCatalog("my-catalog-6", "qps-Ploc", "my-catalog-2"),
        };

        // Act
        var action = () => _sut.Initialize(catalogs);

        // Assert
        action.Should().ThrowExactly<ArgumentException>()
            .WithParameterName("catalogs")
            .WithMessage("The catalog at index 5 is null.*");
    }

    [Theory]
    [CombinatorialData]
    public void TryGet_CatalogForExactCultureExists_CallsTheCorrectCatalog(
        [CombinatorialValues("", "my-catalog", "MY-CATALOG")] string catalogName,
        [CombinatorialValues("", "my-context")] string context,
        [CombinatorialValues(null, 0, 1, 2)] int? count)
    {
        // Arrange
        var catalog = Substitute.ForPartsOf<TestCatalog>(catalogName.ToLowerInvariant(), "zh-Hant", null);
        _sut.Initialize([
            new TestCatalog(catalogName, "zh-Hant-TW"),
            new TestCatalog(catalogName, "zh"),
            new TestCatalog(catalogName, "en"),
            new TestCatalog("other-catalog", "zh-Hant-TW"),
            catalog,
        ]);

        // Act
        _sut.TryGet(catalogName, new CultureInfo("zh-Hant"), context, count, "my-message", out _);

        // Assert
        catalog.Received(1).TryGet(context, count, "my-message", out _);
    }

    [Fact]
    public void TryGet_CatalogForBaseCultureAvailable_CallsTheCorrectCatalogs()
    {
        // Arrange
        var catalog1 = Substitute.ForPartsOf<TestCatalog>("my-catalog", "zh-Hant-TW", null);
        var catalog2 = Substitute.ForPartsOf<TestCatalog>("my-catalog", "zh-Hant", null);
        var catalog3 = Substitute.ForPartsOf<TestCatalog>("my-catalog", "zh", null);
        var catalog4 = Substitute.ForPartsOf<TestCatalog>("my-catalog", "", null);
        _sut.Initialize([catalog1, catalog2, catalog3, catalog4]);

        // Act
        _sut.TryGet("my-catalog", new CultureInfo("zh-Hant-TW"), "my-context", 123, "my-message", out _);

        // Assert
        catalog1.ReceivedWithAnyArgs(1).TryGet(default!, default, default!, out _);
        catalog2.ReceivedWithAnyArgs(1).TryGet(default!, default, default!, out _);
        catalog3.ReceivedWithAnyArgs(1).TryGet(default!, default, default!, out _);
        catalog4.ReceivedWithAnyArgs(1).TryGet(default!, default, default!, out _);
        Received.InOrder(() =>
        {
            catalog1.TryGet("my-context", 123, "my-message", out _);
            catalog2.TryGet("my-context", 123, "my-message", out _);
            catalog3.TryGet("my-context", 123, "my-message", out _);
            catalog4.TryGet("my-context", 123, "my-message", out _);
        });
    }

    [Theory]
    [InlineData("Exact culture")]
    [InlineData("First parent culture")]
    [InlineData("Second parent culture")]
    [InlineData("Invariant culture")]
    public void TryGet_CatalogFoundAndTranslationExists_ReturnsTrueAndTheTranslation(string textFoundOn)
    {
        // Arrange
        var catalog1 = new TestCatalog("my-catalog", "zh-Hant-TW");
        var catalog2 = new TestCatalog("my-catalog", "zh-Hant");
        var catalog3 = new TestCatalog("my-catalog", "zh");
        var catalog4 = new TestCatalog("my-catalog", "");

        _sut.Initialize([catalog1, catalog2, catalog3, catalog4]);

        var expectedTranslation = TranslationForm.Text("my-translation");
        switch (textFoundOn)
        {
            case "Exact culture":
                catalog1.SetTranslation(expectedTranslation);
                catalog2.SetTranslation(TranslationForm.Text("x-translation"));
                catalog3.SetTranslation(TranslationForm.Text("y-translation"));
                catalog4.SetTranslation(TranslationForm.Text("z-translation"));
                break;
            case "First parent culture":
                catalog2.SetTranslation(expectedTranslation);
                catalog3.SetTranslation(TranslationForm.Text("y-translation"));
                catalog4.SetTranslation(TranslationForm.Text("z-translation"));
                break;
            case "Second parent culture":
                catalog3.SetTranslation(expectedTranslation);
                catalog4.SetTranslation(TranslationForm.Text("z-translation"));
                break;
            case "Invariant culture":
                catalog4.SetTranslation(expectedTranslation);
                break;
            default:
                throw new NotSupportedException($"Unexpected test case: {textFoundOn}");
        }

        // Act
        var result = _sut.TryGet("my-catalog", new CultureInfo("zh-Hant-TW"), "", null, "my-message", out var translation);

        // Assert
        result.Should().BeTrue();
        translation.Should().BeEquivalentTo(TranslationForm.Text("my-translation"));
    }

    [Fact]
    public void TryGet_CatalogFoundAndTranslationDoesNotExist_ReturnsFalseAndNull()
    {
        // Arrange
        var catalog = new TestCatalog("my-catalog", "qps-Ploc");

        _sut.Initialize([catalog]);

        // Act
        var result = _sut.TryGet("my-catalog", new CultureInfo("qps-Ploc"), "", null, "my-message", out var translation);

        // Assert
        result.Should().BeFalse();
        translation.Should().BeNull();
    }

    [Fact]
    public void TryGet_CatalogNotFound_ReturnsFalseAndNull()
    {
        // Arrange
        var catalog = new TestCatalog("other-catalog", "en").SetTranslation(TranslationForm.Text("my-translation"));

        _sut.Initialize([catalog]);

        // Act
        var result = _sut.TryGet("my-catalog", new CultureInfo("qps-Ploc"), "", null, "my-message", out var translation);

        // Assert
        result.Should().BeFalse();
        translation.Should().BeNull();
    }

    [Theory]
    [InlineData("catalogName")]
    [InlineData("culture")]
    [InlineData("context")]
    [InlineData("messageId")]
    public void TryGet_ArgumentIsNull_Throws(string parameterName)
    {
        // Arrange
        _sut.Initialize([new TestCatalog()]);

        // Act
        var action = () => _sut.TryGet(
            parameterName == "catalogName" ? null! : "my-catalog",
            parameterName == "culture" ? null! : new CultureInfo("qps-Ploc"),
            parameterName == "context" ? null! : "my-context",
            null,
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
        var action = () => _sut.TryGet("", new CultureInfo("qps-Ploc"), "", 0, "", out _);

        // Assert
        action.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage("The translation store has not been initialized. The `Initialize` method should be called before attempting to get translations.");
    }

    #region Helpers

    // ReSharper disable all

    internal class TestCatalog : Catalog
    {
        private TranslationForm? _translation;

        public TestCatalog(string? catalogName = null, string? cultureName = null, string? uid = null)
        {
            CatalogName = catalogName ?? "x-catalog";
            Culture = new CultureInfo(cultureName ?? "qps-Ploc");
            Uid = uid ?? "x-catalog-uid";
        }

        public override string Uid { get; }

        public override string CatalogName { get; }
        public override CultureInfo Culture { get; }

        public override bool TryGet(string context, long? count, string messageId, [NotNullWhen(true)] out TranslationForm? translation)
        {
            translation = _translation;
            return translation != null;
        }

        public override string ToString() => $"{CatalogName}/{Culture.Name}";

        public TestCatalog SetTranslation(TranslationForm? translation)
        {
            _translation = translation;
            return this;
        }
    }

    #endregion
}