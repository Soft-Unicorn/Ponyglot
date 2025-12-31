using System.Diagnostics.CodeAnalysis;

#if NETSTANDARD

// ReSharper disable CheckNamespace - Polyfill
// ReSharper disable UnusedAutoPropertyAccessor.Global - Polyfill

namespace System.Runtime.CompilerServices;

/// <summary>
/// Indicates that a parameter captures the expression passed for another parameter as a string.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
[ExcludeFromCodeCoverage]
internal sealed class CallerArgumentExpressionAttribute : Attribute
{
    /// <summary>
    /// Initialize a new instance of the <see cref="CallerArgumentExpressionAttribute"/> class.
    /// </summary>
    /// <param name="parameterName">The name of the parameter whose expression should be captured as a string.</param>
    public CallerArgumentExpressionAttribute(string parameterName)
    {
        ParameterName = parameterName;
    }

    /// <summary>
    /// Gets the name of the parameter whose expression should be captured as a string.
    /// </summary>
    public string ParameterName { get; }
}
#endif