using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ponyglot.Sources.PortableObject.PluralRule;

/// <summary>
/// A class that tokenizes a plural rule expression.
/// </summary>
internal class PluralRuleLexer : IPluralRuleLexer
{
    private static readonly TokenBuilder[] TokenBuilders =
    [
        new NumberTokenBuilder(),

        new SymbolsTokenBuilder(PluralRuleTokenType.NotEqual, '!', '='),
        new SymbolsTokenBuilder(PluralRuleTokenType.Equal, '=', '='),
        new SymbolsTokenBuilder(PluralRuleTokenType.LessThanOrEqual, '<', '='),
        new SymbolsTokenBuilder(PluralRuleTokenType.GreaterThanOrEqual, '>', '='),
        new SymbolsTokenBuilder(PluralRuleTokenType.And, '&', '&'),
        new SymbolsTokenBuilder(PluralRuleTokenType.Or, '|', '|'),

        new SymbolsTokenBuilder(PluralRuleTokenType.NVariable, 'n'),
        new SymbolsTokenBuilder(PluralRuleTokenType.OpenParenthesis, '('),
        new SymbolsTokenBuilder(PluralRuleTokenType.CloseParenthesis, ')'),
        new SymbolsTokenBuilder(PluralRuleTokenType.QuestionMark, '?'),
        new SymbolsTokenBuilder(PluralRuleTokenType.Colon, ':'),
        new SymbolsTokenBuilder(PluralRuleTokenType.Plus, '+'),
        new SymbolsTokenBuilder(PluralRuleTokenType.Minus, '-'),
        new SymbolsTokenBuilder(PluralRuleTokenType.Multiplication, '*'),
        new SymbolsTokenBuilder(PluralRuleTokenType.Division, '/'),
        new SymbolsTokenBuilder(PluralRuleTokenType.Modulo, '%'),
        new SymbolsTokenBuilder(PluralRuleTokenType.Not, '!'),
        new SymbolsTokenBuilder(PluralRuleTokenType.LessThan, '<'),
        new SymbolsTokenBuilder(PluralRuleTokenType.GreaterThan, '>'),

        new EndTokenBuilder(),
    ];

    private readonly ExpressionVisitor _expression;
    private PluralRuleToken? _current;

    /// <summary>
    /// Initialize a new instance of the <see cref="PluralRuleLexer"/> class.
    /// </summary>
    /// <param name="expression">The expression to parse.</param>
    public PluralRuleLexer(string expression)
    {
        _expression = new ExpressionVisitor(expression);
    }

    /// <inheritdoc/>
    public PluralRuleToken Current => _current ?? throw new InvalidOperationException($"There is no current token. Was {nameof(Consume)} called?");

    /// <inheritdoc/>
    public void Consume()
    {
        // Skip whitespaces
        _expression.SkipWhitespaces();

        // Searches for the token
        PluralRuleToken? foundToken = null;
        foreach (var builder in TokenBuilders)
        {
            foundToken = builder.TryBuild(_expression);
            if (foundToken != null)
            {
                break;
            }
        }

        if (foundToken == null)
        {
            // Tries to find an incomplete symbol
            var incompleteSymbol = TokenBuilders
                .Where(b => b.SymbolName.Length >= 2 && b.SymbolName[0] == _expression.Current)
                .Select(b => b.SymbolName)
                .FirstOrDefault();

            var message = incompleteSymbol == null ? $"Unknown character '{_expression.Current}'." : $"Probable incomplete '{incompleteSymbol}'?";
            throw CreateSyntaxError(message);
        }

        _current = foundToken;
    }

    /// <inheritdoc/>
    public void Expect(PluralRuleTokenType expectedType)
    {
        if (Current.Type != expectedType)
        {
            throw CreateSyntaxError([expectedType]);
        }
    }

    /// <inheritdoc/>
    public FormatException CreateSyntaxError(IReadOnlyCollection<PluralRuleTokenType> expectedTypes)
    {
        var expectedList = expectedTypes
            .Select(t => $"'{TokenBuilders.FirstOrDefault(b => b.Type == t)?.SymbolName ?? t.ToString()}'")
            .ToList();

        var expectedStr = expectedList.Count == 1 ? expectedList[0] : $"{string.Join(", ", expectedList.Take(expectedList.Count - 1))} or {expectedList.Last()}";

        return CreateSyntaxError($"Expected {expectedStr} but found '{Current.Text}'.");
    }

    /// <summary>
    /// Creates a syntax error exception.
    /// </summary>
    /// <param name="message">The additional message that describes the problem.</param>
    /// <returns>The exception.</returns>
    private FormatException CreateSyntaxError(string message)
    {
        return new FormatException($"Syntax error in '{_expression}' at position {_expression.Index + 1} ('{_expression.Current}'): {message}");
    }

    /// <summary>
    /// A visitor for the expression to parse.
    /// </summary>
    private class ExpressionVisitor
    {
        private readonly string _expression;

        public ExpressionVisitor(string expression)
        {
            _expression = expression;
            Index = 0;
        }

        /// <summary>
        /// Returns the current character or null if there is no next character.
        /// </summary>
        /// <exception cref="InvalidOperationException">All characters have been consumed.</exception>
        public char? Current => Index < _expression.Length ? _expression[Index] : null;

        /// <summary>
        /// Returns the next character or null if there is no next character.
        /// </summary>
        public char? Next => Index + 1 < _expression.Length ? _expression[Index + 1] : null;

        /// <summary>
        /// Returns the current character index.
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// Skip all whitespaces.
        /// </summary>
        public void SkipWhitespaces()
        {
            while (Index < _expression.Length && char.IsWhiteSpace(_expression[Index]))
            {
                Index++;
            }
        }

        /// <summary>
        /// Consumes the current character.
        /// </summary>
        public void Consume()
        {
            if (Index < _expression.Length)
            {
                Index++;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => _expression;
    }

    /// <summary>
    /// A builder that can parse a token.
    /// </summary>
    /// <param name="type">The type of token this builder can process.</param>
    private abstract class TokenBuilder(PluralRuleTokenType type)
    {
        /// <summary>
        /// The token type that this builder can process.
        /// </summary>
        public PluralRuleTokenType Type { get; } = type;

        /// <summary>
        /// The symbol that represents the token.
        /// </summary>
        public abstract string SymbolName { get; }

        /// <summary>
        /// Tries to build the token
        /// </summary>
        /// <param name="expression">The <see cref="ExpressionVisitor"/> that provides convenient access to the parsed expression.</param>
        /// <returns>The resulting token if successful, otherwise <c>null</c>.</returns>
        public abstract PluralRuleToken? TryBuild(ExpressionVisitor expression);
    }

    /// <summary>
    /// A builder that processes a token composed of two characters where another token exists with the first character only.
    /// </summary>
    /// <param name="type">The type of token this builder can process.</param>
    /// <param name="symbol1">The expected first token character.</param>
    /// <param name="symbol2">The optional expected second token character.</param>
    private class SymbolsTokenBuilder(PluralRuleTokenType type, char symbol1, char? symbol2 = null) : TokenBuilder(type)
    {
        /// <inheritdoc/>
        public override string SymbolName { get; } = symbol2.HasValue ? $"{symbol1}{symbol2}" : symbol1.ToString();

        /// <inheritdoc/>
        public override PluralRuleToken? TryBuild(ExpressionVisitor expression)
        {
            if (expression.Current == symbol1)
            {
                var offset = expression.Index;
                if (symbol2 == null)
                {
                    expression.Consume();
                    return new PluralRuleToken(Type, offset, SymbolName);
                }

                if (expression.Next == symbol2)
                {
                    expression.Consume();
                    expression.Consume();
                    return new PluralRuleToken(Type, offset, SymbolName);
                }
            }

            return null;
        }
    }

    /// <summary>
    /// A builder that processes a number token.
    /// </summary>
    private class NumberTokenBuilder() : TokenBuilder(PluralRuleTokenType.Number)
    {
        /// <inheritdoc/>
        public override string SymbolName => "number";

        /// <inheritdoc/>
        public override PluralRuleToken? TryBuild(ExpressionVisitor expression)
        {
            var offset = expression.Index;
            StringBuilder? numberBuilder = null;
            while (expression.Current is >= '0' and <= '9')
            {
                (numberBuilder ??= new StringBuilder()).Append(expression.Current);
                expression.Consume();
            }

            return numberBuilder != null ? new PluralRuleToken(Type, offset, numberBuilder.ToString()) : null;
        }
    }

    /// <summary>
    /// A builder that processes the end of the expression token.
    /// </summary>
    private class EndTokenBuilder() : TokenBuilder(PluralRuleTokenType.End)
    {
        /// <inheritdoc/>
        public override string SymbolName => "end of expression";

        /// <inheritdoc/>
        public override PluralRuleToken? TryBuild(ExpressionVisitor expression)
        {
            return expression.Current == null ? new PluralRuleToken(Type, expression.Index, SymbolName) : null;
        }
    }
}