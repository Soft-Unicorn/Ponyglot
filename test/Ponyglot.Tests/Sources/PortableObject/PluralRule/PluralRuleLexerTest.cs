using System;
using System.Collections.Generic;
using AwesomeAssertions;
using Ponyglot.Sources.PortableObject.PluralRule;
using Xunit;

namespace Ponyglot.Tests.Sources.PortableObject.PluralRule;

public class PluralRuleLexerTest
{
    [Theory]
    [InlineData("123", PluralRuleTokenType.Number, "123")]
    [InlineData("n", PluralRuleTokenType.NVariable, "n")]
    [InlineData("!", PluralRuleTokenType.Not, "!")]
    [InlineData("!=", PluralRuleTokenType.NotEqual, "!=")]
    [InlineData("==", PluralRuleTokenType.Equal, "==")]
    [InlineData("<", PluralRuleTokenType.LessThan, "<")]
    [InlineData("<=", PluralRuleTokenType.LessThanOrEqual, "<=")]
    [InlineData(">", PluralRuleTokenType.GreaterThan, ">")]
    [InlineData(">=", PluralRuleTokenType.GreaterThanOrEqual, ">=")]
    [InlineData("&&", PluralRuleTokenType.And, "&&")]
    [InlineData("||", PluralRuleTokenType.Or, "||")]
    [InlineData("+", PluralRuleTokenType.Plus, "+")]
    [InlineData("-", PluralRuleTokenType.Minus, "-")]
    [InlineData("*", PluralRuleTokenType.Multiplication, "*")]
    [InlineData("/", PluralRuleTokenType.Division, "/")]
    [InlineData("%", PluralRuleTokenType.Modulo, "%")]
    [InlineData("(", PluralRuleTokenType.OpenParenthesis, "(")]
    [InlineData(")", PluralRuleTokenType.CloseParenthesis, ")")]
    [InlineData("?", PluralRuleTokenType.QuestionMark, "?")]
    [InlineData(":", PluralRuleTokenType.Colon, ":")]
    [InlineData("", PluralRuleTokenType.End, "end of expression")]
    internal void Consume_SingleToken_ReturnsExpectedToken(string expression, PluralRuleTokenType expectedType, string expectedText)
    {
        // Arrange
        var sut = new PluralRuleLexer(expression);

        // Act
        sut.Consume();

        // Assert
        sut.Current.Type.Should().Be(expectedType);
        sut.Current.Text.Should().Be(expectedText);
    }

    [Fact]
    public void Consume_Whitespaces_AreIgnored()
    {
        // Arrange
        var sut = new PluralRuleLexer("  123   ");

        // Act
        sut.Consume();

        // Assert
        sut.Current.Type.Should().Be(PluralRuleTokenType.Number);
        sut.Current.Text.Should().Be("123");
    }

    [Theory]
    [InlineData("@", "Unknown character '@'")]
    [InlineData("& ", "Probable incomplete '&&'?")]
    [InlineData("| ", "Probable incomplete '||'?")]
    [InlineData("= ", "Probable incomplete '=='?")]
    public void Consume_InvalidInput_ThrowsFormatException(string expression, string expectedMessagePart)
    {
        // Arrange
        var sut = new PluralRuleLexer(expression);

        // Act
        var act = () => sut.Consume();

        // Assert
        act.Should().Throw<FormatException>().WithMessage($"*{expectedMessagePart}*");
    }

    [Fact]
    public void Expect_WrongType_ThrowsFormatException()
    {
        // Arrange
        var sut = new PluralRuleLexer("123");
        sut.Consume();

        // Act
        var act = () => sut.Expect(PluralRuleTokenType.NVariable);

        // Assert
        act.Should().Throw<FormatException>().WithMessage("*Expected 'n' but found '123'*");
    }

    [Fact]
    public void Lex_AllTokens_ReturnsExpectedSequence()
    {
        // Arrange
        var expression = "n != == <= >= && || ( ) ? : + - * / % ! < > 123";
        var sut = new PluralRuleLexer(expression);

        // Act
        var tokens = new List<PluralRuleToken>();
        do
        {
            sut.Consume();
            tokens.Add(sut.Current);
        } while (sut.Current.Type != PluralRuleTokenType.End);

        // Assert
        tokens.Should().BeEquivalentTo([
            new { Type = PluralRuleTokenType.NVariable, Text = "n" },
            new { Type = PluralRuleTokenType.NotEqual, Text = "!=" },
            new { Type = PluralRuleTokenType.Equal, Text = "==" },
            new { Type = PluralRuleTokenType.LessThanOrEqual, Text = "<=" },
            new { Type = PluralRuleTokenType.GreaterThanOrEqual, Text = ">=" },
            new { Type = PluralRuleTokenType.And, Text = "&&" },
            new { Type = PluralRuleTokenType.Or, Text = "||" },
            new { Type = PluralRuleTokenType.OpenParenthesis, Text = "(" },
            new { Type = PluralRuleTokenType.CloseParenthesis, Text = ")" },
            new { Type = PluralRuleTokenType.QuestionMark, Text = "?" },
            new { Type = PluralRuleTokenType.Colon, Text = ":" },
            new { Type = PluralRuleTokenType.Plus, Text = "+" },
            new { Type = PluralRuleTokenType.Minus, Text = "-" },
            new { Type = PluralRuleTokenType.Multiplication, Text = "*" },
            new { Type = PluralRuleTokenType.Division, Text = "/" },
            new { Type = PluralRuleTokenType.Modulo, Text = "%" },
            new { Type = PluralRuleTokenType.Not, Text = "!" },
            new { Type = PluralRuleTokenType.LessThan, Text = "<" },
            new { Type = PluralRuleTokenType.GreaterThan, Text = ">" },
            new { Type = PluralRuleTokenType.Number, Text = "123" },
            new { Type = PluralRuleTokenType.End, Text = "end of expression" },
        ]);
    }
}