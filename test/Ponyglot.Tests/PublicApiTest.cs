using System;
using System.Runtime.Versioning;
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

        var options = new ApiGeneratorOptions
        {
            ExcludeAttributes = [typeof(TargetFrameworkAttribute).FullName!],
        };

        // Act
        var publicApi = assembly.GeneratePublicApi(options).Replace(Environment.NewLine, "\n");

        // Assert
        await Verifier
            .Verify(publicApi, extension: "cs")
            .UseFileName("PublicApi");
    }
}