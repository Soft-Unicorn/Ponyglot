using System.Globalization;

namespace Ponyglot;

/// <summary>
/// The default culture source that reads <see cref="CultureInfo.CurrentUICulture"/>.
/// </summary>
public sealed class DefaultCultureSource : ICultureSource
{
    /// <inheritdoc/>
    public CultureInfo Culture
    {
        get => CultureInfo.CurrentUICulture;
        set => CultureInfo.CurrentUICulture = value;
    }
}