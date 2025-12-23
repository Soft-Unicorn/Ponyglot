using System.Threading.Tasks;
using PublicApiGenerator;
using VerifyXunit;
using Xunit;

namespace Ponyglot.Tests;

public class PublicApiTest
{
    [Fact]
    public async Task PublicApi_SinceLastAccept_HasNotChanged()
    {
        // Arrange
        var assembly = typeof(ITranslator).Assembly;

        // Act
        var publicApi = assembly.GeneratePublicApi().ReplaceLineEndings("\n");

        // Assert
        await Verifier
            .Verify(publicApi, extension: "cs")
            .UseFileName("PublicApi");
    }
}