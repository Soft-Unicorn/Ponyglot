using System.Linq;
using AwesomeAssertions;
using Xunit;

namespace Ponyglot.Tests;

public class PolyfillsTest
{
#if NETFRAMEWORK
    [Fact]
    public void Assembly_BuiltForNetStandard_ContainsNonPublicPolyfills()
    {
        // Arrange

        // Act
        var types = typeof(ITranslator).Assembly.GetTypes()
            .Where(t => (t.Namespace ?? "").StartsWith("System"))
            .ToList();

        // Assert
        types.Should().NotBeEmpty();
        types.Should().AllSatisfy(t => t.IsPublic.Should().BeFalse());
    }
#endif
}