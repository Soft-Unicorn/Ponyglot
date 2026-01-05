using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace Ponyglot.Sources.PortableObject.PluralRule;

/// <summary>
/// The plural rule parser responsible for parsing plural rules into expression trees.
/// </summary>
internal sealed class PluralRuleParser
{
    private static readonly ReadOnlyDictionary<PluralRuleTokenType, PrefixParselet> PrefixNodeBuilders = new(new Dictionary<PluralRuleTokenType, PrefixParselet>
    {
        [PluralRuleTokenType.Number] = new ValueParselet(t => PluralRuleExpression.Number(int.Parse(t, CultureInfo.InvariantCulture))),
        [PluralRuleTokenType.NVariable] = new ValueParselet(_ => PluralRuleExpression.NVariable()),
        [PluralRuleTokenType.Not] = new UnaryOperatorParselet(PluralRuleExpression.Not),
        [PluralRuleTokenType.Plus] = new UnaryOperatorParselet(PluralRuleExpression.Plus),
        [PluralRuleTokenType.Minus] = new UnaryOperatorParselet(PluralRuleExpression.Minus),
        [PluralRuleTokenType.OpenParenthesis] = new GroupOperatorParselet(),
    });

    private static readonly ReadOnlyDictionary<PluralRuleTokenType, InfixNodeBuilder> InfixNodeBuilders = new(new Dictionary<PluralRuleTokenType, InfixNodeBuilder>
    {
        [PluralRuleTokenType.Multiplication] = new BinaryNodeBuilder(70, PluralRuleExpression.Multiply),
        [PluralRuleTokenType.Division] = new BinaryNodeBuilder(70, PluralRuleExpression.Divide),
        [PluralRuleTokenType.Modulo] = new BinaryNodeBuilder(70, PluralRuleExpression.Modulo),

        [PluralRuleTokenType.Plus] = new BinaryNodeBuilder(60, PluralRuleExpression.Add),
        [PluralRuleTokenType.Minus] = new BinaryNodeBuilder(60, PluralRuleExpression.Subtract),

        [PluralRuleTokenType.LessThan] = new BinaryNodeBuilder(50, PluralRuleExpression.LessThan),
        [PluralRuleTokenType.LessThanOrEqual] = new BinaryNodeBuilder(50, PluralRuleExpression.LessThanOrEqual),
        [PluralRuleTokenType.GreaterThan] = new BinaryNodeBuilder(50, PluralRuleExpression.GreaterThan),
        [PluralRuleTokenType.GreaterThanOrEqual] = new BinaryNodeBuilder(50, PluralRuleExpression.GreaterThanOrEqual),

        [PluralRuleTokenType.Equal] = new BinaryNodeBuilder(40, PluralRuleExpression.Equal),
        [PluralRuleTokenType.NotEqual] = new BinaryNodeBuilder(40, PluralRuleExpression.NotEqual),

        [PluralRuleTokenType.And] = new BinaryNodeBuilder(30, PluralRuleExpression.And),

        [PluralRuleTokenType.Or] = new BinaryNodeBuilder(20, PluralRuleExpression.Or),

        [PluralRuleTokenType.QuestionMark] = new TernaryConditionalNodeBuilder(10, PluralRuleExpression.TernaryCondition),
    });

    private readonly IPluralRuleLexer _lexer;

    /// <summary>
    /// Initialize a new instance of the <see cref="PluralRuleParser"/> class.
    /// </summary>
    /// <param name="lexer">The lexer that parses the expression and returns the resulting <see cref="PluralRuleToken"/>.</param>
    public PluralRuleParser(IPluralRuleLexer lexer)
    {
        _lexer = lexer ?? throw new ArgumentNullException(nameof(lexer));
    }

    /// <summary>
    /// Parses the plural rule into an expression tree.
    /// </summary>
    /// <returns>The <see cref="PluralRuleExpression"/> that represents the root of the expression tree.</returns>
    public PluralRuleExpression Parse()
    {
        _lexer.Consume();
        var node = ParseExpression(0);
        _lexer.Expect(PluralRuleTokenType.End);
        return node;
    }

    /// <summary>
    /// Parse the next expression.
    /// </summary>
    /// <param name="minPrecedence">The minimum (inclusive) precedence of the infix operators to parse.</param>
    /// <returns>The parsed expression.</returns>
    /// <exception cref="FormatException">Thrown when the expression is malformed.</exception>
    private PluralRuleExpression ParseExpression(int minPrecedence)
    {
        // Parses the prefix
        if (!PrefixNodeBuilders.TryGetValue(_lexer.Current.Type, out var prefixNodeBuilder))
        {
            throw _lexer.CreateSyntaxError(PrefixNodeBuilders.Keys.ToList());
        }

        var left = prefixNodeBuilder.Parse(this, _lexer);

        // Parses the infix operators of higher or equal precedence
        while (InfixNodeBuilders.TryGetValue(_lexer.Current.Type, out var infixNodeBuilder) && infixNodeBuilder.Precedence >= minPrecedence)
        {
            left = infixNodeBuilder.Parse(this, left, _lexer);
        }

        return left;
    }

    private abstract class PrefixParselet
    {
        public abstract PluralRuleExpression Parse(PluralRuleParser parser, IPluralRuleLexer lexer);
    }

    private class ValueParselet(Func<string, PluralRuleExpression> factory) : PrefixParselet
    {
        public override PluralRuleExpression Parse(PluralRuleParser parser, IPluralRuleLexer lexer)
        {
            // Consume the token
            var text = lexer.Current.Text;
            lexer.Consume();

            // Build the node
            return factory(text);
        }
    }

    private class UnaryOperatorParselet(Func<PluralRuleExpression, PluralRuleExpression> factory) : PrefixParselet
    {
        public override PluralRuleExpression Parse(PluralRuleParser parser, IPluralRuleLexer lexer)
        {
            // Consume the token
            lexer.Consume();

            // Parse the operand
            var operand = parser.ParseExpression(int.MaxValue);

            // Build the node
            return factory(operand);
        }
    }

    private class GroupOperatorParselet : PrefixParselet
    {
        public override PluralRuleExpression Parse(PluralRuleParser parser, IPluralRuleLexer lexer)
        {
            // Consume the token (open parenthesis)
            lexer.Consume();

            // Parse the inner expression
            var inner = parser.ParseExpression(0);

            // Verify the closing parenthesis and consumes it
            lexer.Expect(PluralRuleTokenType.CloseParenthesis);
            lexer.Consume();

            // Returns the inner expression 
            return inner;
        }
    }

    private abstract class InfixNodeBuilder(int precedence)
    {
        public int Precedence { get; } = precedence;

        public abstract PluralRuleExpression Parse(PluralRuleParser parser, PluralRuleExpression left, IPluralRuleLexer lexer);
    }

    private class BinaryNodeBuilder(int precedence, Func<PluralRuleExpression, PluralRuleExpression, PluralRuleExpression> factory) : InfixNodeBuilder(precedence)
    {
        public override PluralRuleExpression Parse(PluralRuleParser parser, PluralRuleExpression left, IPluralRuleLexer lexer)
        {
            // Consumes the token
            lexer.Consume();

            // Parses the right operand
            var right = parser.ParseExpression(Precedence + 1);

            // Builds the node
            return factory(left, right);
        }
    }

    private class TernaryConditionalNodeBuilder(int precedence, Func<PluralRuleExpression, PluralRuleExpression, PluralRuleExpression, PluralRuleExpression> factory) : InfixNodeBuilder(precedence)
    {
        /// <inheritdoc/>
        public override PluralRuleExpression Parse(PluralRuleParser parser, PluralRuleExpression left, IPluralRuleLexer lexer)
        {
            // Consumes the token
            lexer.Consume();

            // Parses the "then" arm
            var thenArm = parser.ParseExpression(0);

            // Verifies the then-else separator and consumes
            lexer.Expect(PluralRuleTokenType.Colon);
            lexer.Consume();

            // Parses the "else" arm
            var elseArm = parser.ParseExpression(Precedence);

            // Builds the node
            return factory(left, thenArm, elseArm);
        }
    }
}