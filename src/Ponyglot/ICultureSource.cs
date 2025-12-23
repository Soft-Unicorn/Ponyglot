using System.Globalization;

namespace Ponyglot;

/// <summary>
/// Provides the current UI culture used for translation lookups.
/// </summary>
public interface ICultureSource
{
    /// <summary>
    /// The culture to use to provide the translations.
    /// </summary>
    CultureInfo Culture { get; set; }
}