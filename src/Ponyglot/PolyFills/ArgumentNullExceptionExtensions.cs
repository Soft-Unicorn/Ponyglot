#if NETSTANDARD

// ReSharper disable CheckNamespace - Polyfill

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System;

/// <summary>
/// Adds missing method on the <see cref="ArgumentNullException"/> interface in .NET Standard.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ArgumentNullExceptionExtensions
{
    extension(ArgumentNullException)
    {
        /// <summary>
        /// Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null.
        /// </summary>
        /// <param name="argument">The reference type argument to validate as non-null.</param>
        /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
        public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument is null)
            {
                throw new ArgumentNullException(paramName);
            }
        }
    }
}

#endif