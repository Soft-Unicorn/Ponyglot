using System;
using System.Globalization;
using AwesomeAssertions;
using Xunit;

namespace Ponyglot.Tests;

public sealed class DefaultCultureSourceTest : IDisposable
{
    private readonly (CultureInfo Current, CultureInfo CurrentUI) _savedCultures;
    private readonly DefaultCultureSource _sut;

    public DefaultCultureSourceTest()
    {
        _sut = new DefaultCultureSource();

        _savedCultures = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    public void Dispose()
    {
        (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture) = _savedCultures;
    }

    [Fact]
    public void Culture_Created_ReturnsTheCurrentUICulture()
    {
        // Arrange
        var culture = new CultureInfo("qps-Ploc");
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        // Act
        var currentUiCulture = _sut.Culture;

        // Assert
        currentUiCulture.Should().Be(culture);
    }

    [Fact]
    public void CurrentUiCulture_Set_SetsTheCurrentUICulture()
    {
        // Arrange
        var culture = new CultureInfo("qps-Ploc");

        // Act
        _sut.Culture = culture;

        // Assert
        CultureInfo.CurrentUICulture.Should().Be(culture);
    }

    [Fact]
    public void CurrentUiCulture_Set_DoesNotSetTheCurrentCulture()
    {
        // Arrange
        var culture = new CultureInfo("qps-Ploc");

        // Act
        _sut.Culture = culture;

        // Assert
        CultureInfo.CurrentCulture.Should().NotBe(culture);
    }
}