using System;

namespace Ponyglot;

/// <summary>
/// Selects the plural form index for a given count.
/// </summary>
public interface IPluralRule
{
    /// <summary>
    /// The number of plural forms supported by this rule.
    /// </summary>
    int PluralCount { get; }

    /// <summary>
    /// Selects the zero-based plural form index for the specified count.
    /// </summary>
    /// <param name="count">The positive count used for plural selection.</param>
    /// <returns>A plural index in between <c>0</c> (inclusive) and <see cref="PluralCount"/> (exclusive).</returns>
    /// <exception cref="ArgumentException"><paramref name="count"/> is negative.</exception>
    /// <remarks><paramref name="count"/> can be any positive <see cref="long"/> value between <c>0</c> (inclusive) and <see cref="long.MaxValue"/> (inclusive).</remarks>
    int GetPluralForm(long count);
}