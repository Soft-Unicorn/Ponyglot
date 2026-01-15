using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Ponyglot.Tests._TestUtils;

public static class TestExtensionMethods
{
    extension<T>(IAsyncEnumerable<T> enumerable)
    {
        public async Task ConsumeAsync()
        {
            await foreach (var _ in enumerable)
            {
            }
        }

        public async Task<List<T>> RealizeAsync()
        {
            var list = new List<T>();
            await foreach (var item in enumerable)
            {
                list.Add(item);
            }

            return list;
        }
    }

    extension(Stream stream)
    {
        public string AsString()
        {
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}