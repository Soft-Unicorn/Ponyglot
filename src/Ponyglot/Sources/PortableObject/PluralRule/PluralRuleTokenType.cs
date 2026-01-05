namespace Ponyglot.Sources.PortableObject.PluralRule;

/// <summary>
/// Defines the types of tokens in a plural rule expression.
/// </summary>
internal enum PluralRuleTokenType
{
    /// <summary>
    /// Represents a number literal.
    /// </summary>
    Number,

    /// <summary>
    /// Represents the 'n' variable.
    /// </summary>
    NVariable,

    /// <summary>
    /// Represents an opening parenthesis <c>(</c>.
    /// </summary>
    OpenParenthesis,

    /// <summary>
    /// Represents a closing parenthesis <c>)</c>.
    /// </summary>
    CloseParenthesis,

    /// <summary>
    /// Represents the ternary 'if' question mark <c>?</c>.
    /// </summary>
    QuestionMark,

    /// <summary>
    /// Represents the ternary 'else' colon <c>:</c>.
    /// </summary>
    Colon,

    /// <summary>
    /// Represents the plus operator <c>+</c>.
    /// </summary>
    Plus,

    /// <summary>
    /// Represents the minus operator <c>-</c>.
    /// </summary>
    Minus,

    /// <summary>
    /// Represents the multiplication operator <c>*</c>.
    /// </summary>
    Multiplication,

    /// <summary>
    /// Represents the division operator <c>/</c>.
    /// </summary>
    Division,

    /// <summary>
    /// Represents the module operator <c>%</c>.
    /// </summary>
    Modulo,

    /// <summary>
    /// Represents the logical not <c>!</c>.
    /// </summary>
    Not,

    /// <summary>
    /// Represents the logical AND operator <c><![CDATA[&&]]></c>.
    /// </summary>
    And,

    /// <summary>
    /// Represents the logical OR operator <c>||</c>.
    /// </summary>
    Or,

    /// <summary>
    /// Represents the equal operator <c>==</c>.
    /// </summary>
    Equal,

    /// <summary>
    /// Represents the not equal operator <c>!=</c>.
    /// </summary>
    NotEqual,

    /// <summary>
    /// Represents the less than operator <c><![CDATA[>]]></c>.
    /// </summary>
    LessThan,

    /// <summary>
    /// Represents the less than or equal operator <c><![CDATA[<=]]></c>.
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    /// Represents the greater than operator <c><![CDATA[>]]></c>.
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Represents the greater than or equal operator <c><![CDATA[>=]]></c>.
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// Represents the end of the expression.
    /// </summary>
    End,
}