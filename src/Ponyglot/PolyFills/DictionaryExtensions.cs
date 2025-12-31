using System.Diagnostics.CodeAnalysis;

#if NETSTANDARD

// ReSharper disable CheckNamespace - Polyfill

namespace System.Collections.Generic;

/// <summary>
/// Adds missing method on the <see cref="IDictionary{TKey,TValue}"/> interface in .NET Standard.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class DictionaryExtensions
{
    /// <summary>
    /// Attempts to add the specified key and value to the dictionary.
    /// </summary>
    /// <param name="dictionary">The <see cref="IDictionary{TKey,TValue}"/> to extend.</param>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add. It can be null.</param>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <returns><c>true</c> if the key/value pair was added to the dictionary successfully; otherwise, <c>false</c>.</returns>
    public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        if (!dictionary.ContainsKey(key))
        {
            dictionary.Add(key, value);
            return true;
        }

        return false;
    }
}

#endif