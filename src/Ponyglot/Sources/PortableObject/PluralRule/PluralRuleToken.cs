namespace Ponyglot.Sources.PortableObject.PluralRule;

/// <summary>
/// Represents a token in a plural rule expression.
/// </summary>
/// <param name="type">The <see cref="PluralRuleTokenType"/> that defines the type of the token.</param>
/// <param name="offset">The zero-based character offset of the token in the expression.</param>
/// <param name="text">The token text.</param>
internal class PluralRuleToken(PluralRuleTokenType type, int offset, string text)
{
    /// <summary>
    /// The <see cref="PluralRuleTokenType"/> that defines the type of the token.
    /// </summary>
    public PluralRuleTokenType Type { get; } = type;

    /// <summary>
    /// The token text.
    /// </summary>
    public string Text { get; } = text;

    /// <inheritdoc/>
    public override string ToString() => $"{Text} [{offset}, {Type}]";
}