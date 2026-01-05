using AwesomeAssertions;
using Ponyglot.Sources.PortableObject.PluralRule;
using Xunit;

namespace Ponyglot.Tests.Sources.PortableObject.PluralRule;

public class PluralRuleTokenTest
{
    [Fact]
    public void Constructor_WhenCalled_InitializesProperties()
    {
        // Arrange

        // Act
        var sut = new PluralRuleToken(PluralRuleTokenType.Number, 10, "my-text");

        // Assert
        sut.Type.Should().Be(PluralRuleTokenType.Number);
        sut.Text.Should().Be("my-text");
    }

    [Theory]
    [InlineData(PluralRuleTokenType.Number, 0, "123", "123 [0, Number]")]
    [InlineData(PluralRuleTokenType.NVariable, 5, "n", "n [5, NVariable]")]
    [InlineData(PluralRuleTokenType.Equal, 10, "==", "== [10, Equal]")]
    [InlineData(PluralRuleTokenType.End, 20, "end of expression", "end of expression [20, End]")]
    internal void ToString_WhenCalled_ReturnsExpectedFormat(PluralRuleTokenType type, int offset, string text, string expected)
    {
        // Arrange
        var sut = new PluralRuleToken(type, offset, text);

        // Act
        var result = sut.ToString();

        // Assert
        result.Should().Be(expected);
    }
}