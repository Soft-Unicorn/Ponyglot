using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using Ponyglot.Sources.PortableObject.PluralRule;
using Ponyglot.Tests._TestUtils;
using Xunit;

namespace Ponyglot.Tests.Sources.PortableObject.PluralRule;

public class PluralRuleParserTest
{
    private readonly FakeLexer _lexer;
    private readonly PluralRuleParser _sut;

    public PluralRuleParserTest()
    {
        _lexer = new FakeLexer();
        _sut = new PluralRuleParser(_lexer);
    }

    [Fact]
    public void Parse_AtomN_ReturnsCorrectAST()
    {
        // Arrange
        _lexer.Setup("n");

        // Act
        var result = _sut.Parse();

        // Assert
        var actual = AstTree.From(result);
        var expected = AstTree.From(PluralRuleExpression.NVariable());

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Parse_AtomNumber_ReturnsCorrectAST()
    {
        // Arrange
        _lexer.Setup("123");

        // Act
        var result = _sut.Parse();

        // Assert
        var actual = AstTree.From(result);
        var expected = AstTree.From(
            PluralRuleExpression.Number(123));

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Parse_UnaryNot_ReturnsCorrectAST()
    {
        // Arrange
        _lexer.Setup("!", "n");

        // Act
        var result = _sut.Parse();

        // Assert
        var actual = AstTree.From(result);
        var expected = AstTree.From(
            PluralRuleExpression.Not(
                PluralRuleExpression.NVariable()));

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Parse_UnaryMinus_ReturnsCorrectAST()
    {
        // Arrange
        _lexer.Setup("-", "n");

        // Act
        var result = _sut.Parse();

        // Assert
        var actual = AstTree.From(result);
        var expected = AstTree.From(
            PluralRuleExpression.Minus(
                PluralRuleExpression.NVariable()));

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Parse_UnaryPlus_ReturnsCorrectAST()
    {
        // Arrange
        _lexer.Setup("+", "n");

        // Act
        var result = _sut.Parse();

        // Assert
        var actual = AstTree.From(result);
        var expected = AstTree.From(
            PluralRuleExpression.Plus(
                PluralRuleExpression.NVariable()));

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Parse_UnaryWithGrouping_ReturnsCorrectAST()
    {
        // Arrange
        _lexer.Setup("!", "(", "n", "==", "1", ")");

        // Act
        var result = _sut.Parse();

        // Assert
        var actual = AstTree.From(result);
        var expected = AstTree.From(
            PluralRuleExpression.Not(
                PluralRuleExpression.Equal(
                    PluralRuleExpression.NVariable(),
                    PluralRuleExpression.Number(1))));

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Parse_ArithmeticMulBeforeAdd_ReturnsCorrectAST()
    {
        // Arrange
        _lexer.Setup("1", "+", "2", "*", "3");

        // Act
        var result = _sut.Parse();

        // Assert
        var actual = AstTree.From(result);
        var expected = AstTree.From(
            PluralRuleExpression.Add(
                PluralRuleExpression.Number(1),
                PluralRuleExpression.Multiply(
                    PluralRuleExpression.Number(2),
                    PluralRuleExpression.Number(3))));

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Parse_ArithmeticGroupingChangesPrecedence_ReturnsCorrectAST()
    {
        // Arrange
        _lexer.Setup("(", "1", "+", "2", ")", "*", "3");

        // Act
        var result = _sut.Parse();

        // Assert
        var actual = AstTree.From(result);
        var expected = AstTree.From(
            PluralRuleExpression.Multiply(
                PluralRuleExpression.Add(
                    PluralRuleExpression.Number(1),
                    PluralRuleExpression.Number(2)),
                PluralRuleExpression.Number(3)));

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Parse_ArithmeticModulo_ReturnsCorrectAST()
    {
        // Arrange
        _lexer.Setup("n", "%", "10");

        // Act
        var result = _sut.Parse();

        // Assert
        var actual = AstTree.From(result);
        var expected = AstTree.From(
            PluralRuleExpression.Modulo(
                PluralRuleExpression.NVariable(),
                PluralRuleExpression.Number(10)));

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Parse_ArithmeticDivide_ReturnsCorrectAST()
    {
        // Arrange
        _lexer.Setup("n", "/", "2");

        // Act
        var result = _sut.Parse();

        // Assert
        var actual = AstTree.From(result);
        var expected = AstTree.From(
            PluralRuleExpression.Divide(
                PluralRuleExpression.NVariable(),
                PluralRuleExpression.Number(2)));

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Parse_ComparisonLessThan_ReturnsCorrectAST()
    {
        // Arrange
        _lexer.Setup("n", "<", "2");

        // Act
        var result = _sut.Parse();

        // Assert
        var actual = AstTree.From(result);
        var expected = AstTree.From(
            PluralRuleExpression.LessThan(
                PluralRuleExpression.NVariable(),
                PluralRuleExpression.Number(2)));

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Parse_ComparisonLessThanOrEqual_ReturnsCorrectAST()
    {
        // Arrange
        _lexer.Setup("n", "<=", "2");

        // Act
        var result = _sut.Parse();

        // Assert
        var actual = AstTree.From(result);
        var expected = AstTree.From(
            PluralRuleExpression.LessThanOrEqual(
                PluralRuleExpression.NVariable(),
                PluralRuleExpression.Number(2)));

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Parse_ComparisonGreaterThan_ReturnsCorrectAST()
    {
        // Arrange
        _lexer.Setup("n", ">", "2");

        // Act
        var result = _sut.Parse();

        // Assert
        var actual = AstTree.From(result);
        var expected = AstTree.From(
            PluralRuleExpression.GreaterThan(
                PluralRuleExpression.NVariable(),
                PluralRuleExpression.Number(2)));

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Parse_ComparisonGreaterThanOrEqual_ReturnsCorrectAST()
    {
        // Arrange
        _lexer.Setup("n", ">=", "2");

        // Act
        var result = _sut.Parse();

        // Assert
        var actual = AstTree.From(result);
        var expected = AstTree.From(
            PluralRuleExpression.GreaterThanOrEqual(
                PluralRuleExpression.NVariable(),
                PluralRuleExpression.Number(2)));

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Parse_ComparisonEqualEqual_ReturnsCorrectAST()
    {
        // Arrange
        _lexer.Setup("n", "==", "2");

        // Act
        var result = _sut.Parse();

        // Assert
        var actual = AstTree.From(result);
        var expected = AstTree.From(
            PluralRuleExpression.Equal(
                PluralRuleExpression.NVariable(),
                PluralRuleExpression.Number(2)));

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Parse_ComparisonNotEqual_ReturnsCorrectAST()
    {
        // Arrange
        _lexer.Setup("n", "!=", "2");

        // Act
        var result = _sut.Parse();

        // Assert
        var actual = AstTree.From(result);
        var expected = AstTree.From(
            PluralRuleExpression.NotEqual(
                PluralRuleExpression.NVariable(),
                PluralRuleExpression.Number(2)));

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Parse_BooleanBindsTighter_ReturnsCorrectAST()
    {
        // Arrange
        _lexer.Setup("n", "==", "1", "||", "n", "==", "2", "&&", "n", "==", "3");

        // Act
        var result = _sut.Parse();

        // Assert
        var actual = AstTree.From(result);
        var expected = AstTree.From(
            PluralRuleExpression.Or(
                PluralRuleExpression.Equal(
                    PluralRuleExpression.NVariable(),
                    PluralRuleExpression.Number(1)),
                PluralRuleExpression.And(
                    PluralRuleExpression.Equal(
                        PluralRuleExpression.NVariable(),
                        PluralRuleExpression.Number(2)),
                    PluralRuleExpression.Equal(
                        PluralRuleExpression.NVariable(),
                        PluralRuleExpression.Number(3)))));

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Parse_BooleanGrouping_ReturnsCorrectAST()
    {
        // Arrange
        _lexer.Setup("(", "n", "==", "1", "||", "n", "==", "2", ")", "&&", "n", "!=", "3");

        // Act
        var result = _sut.Parse();

        // Assert
        var actual = AstTree.From(result);
        var expected = AstTree.From(
            PluralRuleExpression.And(
                PluralRuleExpression.Or(
                    PluralRuleExpression.Equal(
                        PluralRuleExpression.NVariable(),
                        PluralRuleExpression.Number(1)),
                    PluralRuleExpression.Equal(
                        PluralRuleExpression.NVariable(),
                        PluralRuleExpression.Number(2))),
                PluralRuleExpression.NotEqual(
                    PluralRuleExpression.NVariable(),
                    PluralRuleExpression.Number(3))));

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Parse_TernarySimple_ReturnsCorrectAST()
    {
        // Arrange
        _lexer.Setup("n", "==", "1", "?", "0", ":", "1");

        // Act
        var result = _sut.Parse();

        // Assert
        var actual = AstTree.From(result);
        var expected = AstTree.From(
            PluralRuleExpression.TernaryCondition(
                PluralRuleExpression.Equal(
                    PluralRuleExpression.NVariable(),
                    PluralRuleExpression.Number(1)),
                PluralRuleExpression.Number(0),
                PluralRuleExpression.Number(1)));

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Parse_TernaryRightAssociative_ReturnsCorrectAST()
    {
        // Arrange
        _lexer.Setup("n", "==", "0", "?", "0", ":", "n", "==", "1", "?", "1", ":", "2");

        // Act
        var result = _sut.Parse();

        // Assert
        var actual = AstTree.From(result);
        var expected = AstTree.From(
            PluralRuleExpression.TernaryCondition(
                PluralRuleExpression.Equal(
                    PluralRuleExpression.NVariable(),
                    PluralRuleExpression.Number(0)),
                PluralRuleExpression.Number(0),
                PluralRuleExpression.TernaryCondition(
                    PluralRuleExpression.Equal(
                        PluralRuleExpression.NVariable(),
                        PluralRuleExpression.Number(1)),
                    PluralRuleExpression.Number(1),
                    PluralRuleExpression.Number(2))));

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Parse_GettextStyleFormula_ReturnsCorrectAST()
    {
        // Arrange
        _lexer.Setup("n", "%", "10", "==", "1", "&&", "n", "%", "100", "!=", "11", "?", "0", ":", "1");

        // Act
        var result = _sut.Parse();

        // Assert
        var actual = AstTree.From(result);
        var expected = AstTree.From(
            PluralRuleExpression.TernaryCondition(
                PluralRuleExpression.And(
                    PluralRuleExpression.Equal(
                        PluralRuleExpression.Modulo(
                            PluralRuleExpression.NVariable(),
                            PluralRuleExpression.Number(10)),
                        PluralRuleExpression.Number(1)),
                    PluralRuleExpression.NotEqual(
                        PluralRuleExpression.Modulo(
                            PluralRuleExpression.NVariable(),
                            PluralRuleExpression.Number(100)),
                        PluralRuleExpression.Number(11))),
                PluralRuleExpression.Number(0),
                PluralRuleExpression.Number(1)));

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Parse_InvalidPrefix_Throws()
    {
        // Arrange
        _lexer.Setup("+", "+");

        // Act
        var action = () => _sut.Parse();

        // Assert
        action.Should().ThrowExactly<SyntaxErrorException>()
            .Which.ExpectedTypes.Should().BeEquivalentTo([
                PluralRuleTokenType.Number,
                PluralRuleTokenType.NVariable,
                PluralRuleTokenType.Not,
                PluralRuleTokenType.Plus,
                PluralRuleTokenType.Minus,
                PluralRuleTokenType.OpenParenthesis,
            ]);
    }

    [Fact]
    public void Parse_MissingClosingParenthesis_Throws()
    {
        // Arrange
        _lexer.Setup("(", "n");

        // Act
        var action = () => _sut.Parse();

        // Assert
        action.Should().ThrowExactly<SyntaxErrorException>()
            .Which.ExpectedTypes.Should().BeEquivalentTo([PluralRuleTokenType.CloseParenthesis]);
    }

    [Fact]
    public void Parse_MissingRightOperand_Throws()
    {
        // Arrange
        _lexer.Setup("n", "+");

        // Act
        var action = () => _sut.Parse();

        // Assert
        action.Should().ThrowExactly<SyntaxErrorException>()
            .Which.ExpectedTypes.Should().BeEquivalentTo([
                PluralRuleTokenType.Number,
                PluralRuleTokenType.NVariable,
                PluralRuleTokenType.Not,
                PluralRuleTokenType.Plus,
                PluralRuleTokenType.Minus,
                PluralRuleTokenType.OpenParenthesis,
            ]);
    }

    [Fact]
    public void Parse_TernaryMissingThenArm_Throws()
    {
        // Arrange
        _lexer.Setup("n", "?");

        // Act
        var action = () => _sut.Parse();

        // Assert
        action.Should().ThrowExactly<SyntaxErrorException>()
            .Which.ExpectedTypes.Should().BeEquivalentTo([
                PluralRuleTokenType.Number,
                PluralRuleTokenType.NVariable,
                PluralRuleTokenType.Not,
                PluralRuleTokenType.Plus,
                PluralRuleTokenType.Minus,
                PluralRuleTokenType.OpenParenthesis,
            ]);
    }

    [Fact]
    public void Parse_TernaryMissingColon_Throws()
    {
        // Arrange
        _lexer.Setup("n", "?", "1");

        // Act
        var action = () => _sut.Parse();

        // Assert
        action.Should().ThrowExactly<SyntaxErrorException>()
            .Which.ExpectedTypes.Should().BeEquivalentTo([PluralRuleTokenType.Colon]);
    }

    [Fact]
    public void Parse_TernaryMissingElseArm_Throws()
    {
        // Arrange
        _lexer.Setup("n", "?", "1", ":");

        // Act
        var action = () => _sut.Parse();

        // Assert
        action.Should().ThrowExactly<SyntaxErrorException>()
            .Which.ExpectedTypes.Should().BeEquivalentTo([
                PluralRuleTokenType.Number,
                PluralRuleTokenType.NVariable,
                PluralRuleTokenType.Not,
                PluralRuleTokenType.Plus,
                PluralRuleTokenType.Minus,
                PluralRuleTokenType.OpenParenthesis,
            ]);
    }

    #region Helpers

    // ReSharper disable all

    private class AstTree : AstNode
    {
        private AstTree(PluralRuleExpression expression)
            : base(expression)
        {
        }

        public static AstTree From(PluralRuleExpression expression) => new(expression);
    }

    private class AstNode
    {
        protected AstNode(PluralRuleExpression expression)
        {
            Symbol = expression.GetSymbol();
            Children = expression.GetChildren().Select(c => new AstNode(c)).ToArray();
        }

        public string Symbol { get; set; }
        public IReadOnlyCollection<AstNode> Children { get; set; }
    }

    private sealed class SyntaxErrorException : Exception
    {
        public SyntaxErrorException(IReadOnlyCollection<PluralRuleTokenType> expectedTypes)
            : base($"Expected token type(s): {string.Join(", ", expectedTypes)}")
        {
            ExpectedTypes = expectedTypes;
        }

        public IReadOnlyCollection<PluralRuleTokenType> ExpectedTypes { get; }
    }

    private sealed class FakeLexer : IPluralRuleLexer
    {
        private readonly List<PluralRuleToken> _tokens = [];
        private int _index = -1;

        public void Setup(params string[] tokens)
        {
            foreach (var text in tokens)
            {
                var type = text switch
                {
                    "n" => PluralRuleTokenType.NVariable,
                    "(" => PluralRuleTokenType.OpenParenthesis,
                    ")" => PluralRuleTokenType.CloseParenthesis,
                    "?" => PluralRuleTokenType.QuestionMark,
                    ":" => PluralRuleTokenType.Colon,
                    "+" => PluralRuleTokenType.Plus,
                    "-" => PluralRuleTokenType.Minus,
                    "*" => PluralRuleTokenType.Multiplication,
                    "/" => PluralRuleTokenType.Division,
                    "%" => PluralRuleTokenType.Modulo,
                    "!" => PluralRuleTokenType.Not,
                    "&&" => PluralRuleTokenType.And,
                    "||" => PluralRuleTokenType.Or,
                    "==" => PluralRuleTokenType.Equal,
                    "!=" => PluralRuleTokenType.NotEqual,
                    "<" => PluralRuleTokenType.LessThan,
                    "<=" => PluralRuleTokenType.LessThanOrEqual,
                    ">" => PluralRuleTokenType.GreaterThan,
                    ">=" => PluralRuleTokenType.GreaterThanOrEqual,
                    _ when int.TryParse(text, out _) => PluralRuleTokenType.Number,
                    _ => throw new ArgumentException($"Unknown token text: {text}"),
                };

                _tokens.Add(new PluralRuleToken(type, _tokens.Count, text));
            }

            _tokens.Add(new PluralRuleToken(PluralRuleTokenType.End, _tokens.Count, "end of expression"));
        }

        public PluralRuleToken Current => _index switch
        {
            < 0 => throw new InvalidOperationException($"There is no current token. Was {nameof(Consume)} called?"),
            _ when _index >= _tokens.Count => throw new InvalidOperationException("All tokens have been consumed."),
            _ => _tokens[_index],
        };

        public void Consume()
        {
            if (_index >= _tokens.Count - 1)
            {
                throw new InvalidOperationException("Cannot consume past the last token.");
            }

            _index++;
        }

        public void Expect(PluralRuleTokenType expectedType)
        {
            if (Current.Type != expectedType)
            {
                throw new SyntaxErrorException([expectedType]);
            }
        }

        public FormatException CreateSyntaxError(IReadOnlyCollection<PluralRuleTokenType> expectedTypes)
            => throw new SyntaxErrorException(expectedTypes);
    }

    #endregion
}