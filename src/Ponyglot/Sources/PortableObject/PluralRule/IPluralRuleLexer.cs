using System;
using System.Collections.Generic;

namespace Ponyglot.Sources.PortableObject.PluralRule;

/// <summary>
/// A class that tokenizes a plural rule expression.
/// </summary>
internal interface IPluralRuleLexer
{
    /// <summary>
    /// Returns the current token
    /// </summary>
    /// <exception cref="InvalidOperationException">There is no current token (<see cref="Consume"/> was not called)</exception>
    PluralRuleToken Current { get; }

    /// <summary>
    /// Consumes the next token and optionally validates the new current token type.
    /// </summary>
    void Consume();

    /// <summary>
    /// Expects the current token to be of the specified type.
    /// </summary>
    /// <param name="expectedType">The type of the token after the current token has been consumed; if null, no validation is to be performed.</param>
    /// <exception cref="FormatException">The current token does not match the expected one.</exception>
    void Expect(PluralRuleTokenType expectedType);

    /// <summary>
    /// Creates a syntax error exception with a list of expected token types.
    /// </summary>
    /// <param name="expectedTypes">The list of expected tokens.</param>
    /// <returns>The exception to throw.</returns>
    FormatException CreateSyntaxError(IReadOnlyCollection<PluralRuleTokenType> expectedTypes);
}