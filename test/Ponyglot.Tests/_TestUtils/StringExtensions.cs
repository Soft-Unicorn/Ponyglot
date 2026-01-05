using System.Collections.Generic;
using System.IO;

namespace Ponyglot.Tests._TestUtils;

/// <summary>
/// Provides extension methods to the <see cref="string"/> type.
/// </summary>
public static class StringExtensions
{
    public static List<string> SplitLines(this string str)
    {
        var lines = new List<string>();

        var reader = new StringReader(str);
        while (reader.ReadLine() is { } line)
        {
            lines.Add(line);
        }

        return lines;
    }
}