using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NSubstitute;
using Ponyglot.Sources;
using Ponyglot.Tests._TestUtils;
using Xunit;

namespace Ponyglot.Tests.Sources;

public class AssemblyCatalogSourceTest
{
    private readonly ICatalogReader _catalogReader = Substitute.For<ICatalogReader>();

    [Theory]
    [InlineData(true, "assembly")]
    [InlineData(true, "catalogReader")]
    [InlineData(true, "options")]
    [InlineData(false, "assembly")]
    [InlineData(false, "catalogReader")]
    public void Constructor_ArgumentIsNull_Throws(bool overloadWithOptions, string parameterName)
    {
        // Arrange

        // Act
        Action action = overloadWithOptions switch
        {
            true => () => _ = new AssemblyCatalogSourceDouble(
                parameterName == "catalogReader" ? null! : _catalogReader,
                parameterName == "assembly" ? null! : Assembly.GetExecutingAssembly(),
                parameterName == "options" ? null! : new AssemblyCatalogSourceOptions()),
            false => () => _ = new AssemblyCatalogSourceDouble(
                parameterName == "catalogReader" ? null! : _catalogReader,
                parameterName == "assembly" ? null! : Assembly.GetExecutingAssembly()),
        };

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName(parameterName);
    }

    [Fact]
    public void Options_CreatedWithOptions_ReturnsTheConstructorValue()
    {
        // Arrange
        var expectedOptions = new AssemblyCatalogSourceOptions();
        var sut = new AssemblyCatalogSourceDouble(_catalogReader, Assembly.GetExecutingAssembly(), expectedOptions);

        // Act
        var options = sut.Options;

        // Assert
        options.Should().BeSameAs(options);
    }

    [Fact]
    public void Options_CreatedWithoutOptions_ReturnsTheDefaultOptions()
    {
        // Arrange
        var sut = new AssemblyCatalogSourceDouble(_catalogReader, Assembly.GetExecutingAssembly());

        // Act
        var options = sut.Options;

        // Assert
        options.Should().BeEquivalentTo(new AssemblyCatalogSourceOptions());
    }

    [Fact]
    public void Assembly_Created_DefaultsTheConstructorValue()
    {
        // Arrange
        var sut = new AssemblyCatalogSourceDouble(_catalogReader, Assembly.GetExecutingAssembly());

        // Act
        var rootDirectory = sut.Assembly;

        // Assert
        rootDirectory.Should().BeSameAs(Assembly.GetExecutingAssembly());
    }

    [Fact]
    public async Task EnumerateResourcesAsync_EmbeddedResourcesFound_ReturnsResourcesWithCorrectAttributes()
    {
        // Arrange
        var assembly = new DynamicAssemblyBuilder("my-assembly")
            .AddResource("my.namespace.my-resource-a.test", "my-content"u8)
            .AddResource("my.namespace.my-resource-b", "my-content"u8)
            .Build();

        var sut = new AssemblyCatalogSourceDouble(_catalogReader, assembly);

        // Act
        var result = await sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync();

        // Assert
        result.OrderBy(r => r.Name, StringComparer.Ordinal).Should().SatisfyRespectively(
            first =>
            {
                first.Uid.Should().Be($"EmbeddedResource:Assembly={assembly.FullName};Resource=my.namespace.my-resource-a.test");
                first.Name.Should().Be("my.namespace.my-resource-a.test");
                first.CatalogName.Should().Be("my-assembly");
            },
            second =>
            {
                second.Uid.Should().Be($"EmbeddedResource:Assembly={assembly.FullName};Resource=my.namespace.my-resource-b");
                second.Name.Should().Be("my.namespace.my-resource-b");
                second.CatalogName.Should().Be("my-assembly");
            });
    }

    [Fact]
    public async Task EnumerateResourcesAsync_EmbeddedResourceFoundBut_ReturnsAResourceThatOpensTheCorrectEmbeddedResource()
    {
        // Arrange
        var assembly = new DynamicAssemblyBuilder("my-assembly")
            .AddResource("my.namespace.my-resource", "my-content"u8)
            .Build();

        var sut = new AssemblyCatalogSourceDouble(_catalogReader, assembly);

        // Act
        var result = await sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync();

        // Assert
        await using var stream = await result.Single().OpenAsync(TestContext.Current.CancellationToken);
        stream.AsString().Should().Be("my-content");
    }

    [Fact]
    public async Task EnumerateResourcesAsync_AssemblyNameIsNull_ReturnsResourcesWithEmptyCatalogName()
    {
        // Arrange
        var assembly = Substitute.For<Assembly>();
        assembly.GetName().Returns(new AssemblyName());
        assembly.GetManifestResourceNames().Returns(["my.namespace.my-resource-b"]);

        var sut = new AssemblyCatalogSourceDouble(_catalogReader, assembly);

        // Act
        var result = await sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync();

        // Assert
        result.Single().CatalogName.Should().BeEmpty();
    }

    [Fact]
    public async Task EnumerateResourcesAsync_FilterSet_CallsFilterForEachEmbeddedResource()
    {
        // Arrange
        var assembly = new DynamicAssemblyBuilder("my-assembly")
            .AddResource("my.namespace.my-resource-a", "my-content"u8)
            .AddResource("my.namespace.my-resource-b", "my-content"u8)
            .AddResource("my.namespace.my-resource-c", "my-content"u8)
            .AddResource("my.namespace.my-resource-d", "my-content"u8)
            .Build();

        var filteredResources = new List<(Assembly Assembly, string ResourceName)>();
        var options = new AssemblyCatalogSourceOptions
        {
            Filter = (asm, name) =>
            {
                filteredResources.Add((asm, name));
                return true;
            },
        };

        var sut = new AssemblyCatalogSourceDouble(_catalogReader, assembly, options);

        // Act
        await sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync();

        // Assert
        filteredResources.Should().BeEquivalentTo(
            [
                (assembly, "my.namespace.my-resource-a"),
                (assembly, "my.namespace.my-resource-b"),
                (assembly, "my.namespace.my-resource-c"),
                (assembly, "my.namespace.my-resource-d"),
            ],
            opts => opts.ComparingByValue<Assembly>());
    }

    [Fact]
    public async Task EnumerateResourcesAsync_FilterSet_ReturnsOnlyResourcesThatSatisfyTheFilter()
    {
        // Arrange
        var assembly = new DynamicAssemblyBuilder("my-assembly")
            .AddResource("my.namespace.my-resource-a", "my-content"u8)
            .AddResource("my.namespace.my-resource-b", "my-content"u8)
            .AddResource("my.namespace.my-resource-c", "my-content"u8)
            .AddResource("my.namespace.my-resource-d", "my-content"u8)
            .Build();

        var options = new AssemblyCatalogSourceOptions
        {
            Filter = (_, name) => name.EndsWith('a') || name.EndsWith('c'),
        };

        var sut = new AssemblyCatalogSourceDouble(_catalogReader, assembly, options);

        // Act
        var result = await sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync();

        // Assert
        result.Select(r => r.Name).Should().BeEquivalentTo(
            "my.namespace.my-resource-a",
            "my.namespace.my-resource-c");
    }

    [Fact]
    public async Task EnumerateResourcesAsync_CatalogNameResolverSet_CallsResolverForEachEmbeddedResource()
    {
        // Arrange
        var assembly = new DynamicAssemblyBuilder("my-assembly")
            .AddResource("my.namespace.my-resource-a", "my-content"u8)
            .AddResource("my.namespace.my-resource-b", "my-content"u8)
            .AddResource("my.namespace.my-resource-c", "my-content"u8)
            .AddResource("my.namespace.my-resource-d", "my-content"u8)
            .Build();

        var resolvedResources = new List<(Assembly Assembly, string ResourceName)>();
        var options = new AssemblyCatalogSourceOptions
        {
            CatalogNameResolver = (asm, name) =>
            {
                resolvedResources.Add((asm, name));
                return "";
            },
        };

        var sut = new AssemblyCatalogSourceDouble(_catalogReader, assembly, options);

        // Act
        await sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync();

        // Assert
        resolvedResources.Should().BeEquivalentTo(
            [
                (assembly, "my.namespace.my-resource-a"),
                (assembly, "my.namespace.my-resource-b"),
                (assembly, "my.namespace.my-resource-c"),
                (assembly, "my.namespace.my-resource-d"),
            ],
            opts => opts.ComparingByValue<Assembly>());
    }

    [Fact]
    public async Task EnumerateResourcesAsync_CatalogNameResolverSet_ReturnsResourcesWithTheResolvedCatalogName()
    {
        // Arrange
        var assembly = new DynamicAssemblyBuilder("my-assembly")
            .AddResource("my.namespace.my-resource-a", "my-content"u8)
            .AddResource("my.namespace.my-resource-b", "my-content"u8)
            .AddResource("my.namespace.my-resource-c", "my-content"u8)
            .AddResource("my.namespace.my-resource-d", "my-content"u8)
            .Build();

        var options = new AssemblyCatalogSourceOptions
        {
            CatalogNameResolver = (_, name) => $"my-catalog-{name[^1..]}",
        };

        var sut = new AssemblyCatalogSourceDouble(_catalogReader, assembly, options);

        // Act
        var result = await sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync();

        // Assert
        result.Select(r => r.CatalogName).Should().BeEquivalentTo(
            "my-catalog-a",
            "my-catalog-b",
            "my-catalog-c",
            "my-catalog-d");
    }

    [Fact]
    public async Task ReturnedResourceOpenAsync_EmbeddedResourceExists_OpensTheCorrectEmbeddedResource()
    {
        // Arrange
        var assembly = new DynamicAssemblyBuilder("my-assembly")
            .AddResource("my.namespace.my-resource", "my-content"u8)
            .Build();

        var sut = new AssemblyCatalogSourceDouble(_catalogReader, assembly);
        var resource = (await sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync()).Single();

        // Act
        await using var stream = await resource.OpenAsync(TestContext.Current.CancellationToken);

        // Assert
        stream.AsString().Should().Be("my-content");
    }

    [Fact]
    public async Task ReturnedResourceOpenAsync_EmbeddedResourceDoesNotExists_Throws()
    {
        // Arrange
        var assembly = Substitute.For<Assembly>();
        assembly.FullName.Returns("my-assembly-full-name");
        assembly.GetName().Returns(new AssemblyName("my-assembly"));
        assembly.GetManifestResourceNames().Returns(["my.namespace.my-resource"]);

        var sut = new AssemblyCatalogSourceDouble(_catalogReader, assembly);
        var resource = (await sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync()).Single();

        // Act
        var action = async () => await (await resource.OpenAsync(TestContext.Current.CancellationToken)).DisposeAsync();

        // Assert
        (await action.Should().ThrowExactlyAsync<FileNotFoundException>())
            .WithMessage("Resource 'my.namespace.my-resource' not found in assembly 'my-assembly-full-name'.")
            .Which.FileName.Should().Be("my.namespace.my-resource");
    }

    [Fact]
    public async Task ReturnedResourceOpenAsync_CancellationOccurs_Throws()
    {
        // Arrange
        var assembly = new DynamicAssemblyBuilder("my-assembly")
            .AddResource("my.namespace.my-resource", "my-content"u8)
            .Build();

        var sut = new AssemblyCatalogSourceDouble(_catalogReader, assembly);
        var resource = (await sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync()).Single();

        // Act
        var action = async () => await (await resource.OpenAsync(new CancellationToken(canceled: true))).DisposeAsync();

        // Assert
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    #region Helpers

    private class AssemblyCatalogSourceDouble : AssemblyCatalogSource
    {
        public AssemblyCatalogSourceDouble(ICatalogReader catalogReader, Assembly assembly)
            : base(catalogReader, assembly)
        {
        }

        public AssemblyCatalogSourceDouble(ICatalogReader catalogReader, Assembly assembly, AssemblyCatalogSourceOptions options)
            : base(catalogReader, assembly, options)
        {
        }

        public new Assembly Assembly => base.Assembly;

        public new AssemblyCatalogSourceOptions Options => base.Options;

        public new IAsyncEnumerable<StreamResource> EnumerateResourcesAsync(CancellationToken cancellationToken = default) => base.EnumerateResourcesAsync(cancellationToken);
    }

    /// <summary>
    /// Helper class for creating real assemblies with embedded resources for testing.
    /// </summary>
    public class DynamicAssemblyBuilder
    {
        private readonly string? _assemblyName;
        private readonly Dictionary<string, byte[]> _resources = new();

        public DynamicAssemblyBuilder(string? assemblyName)
        {
            _assemblyName = assemblyName;
        }

        public DynamicAssemblyBuilder AddResource(string name, ReadOnlySpan<byte> content)
        {
            _resources[name] = content.ToArray();
            return this;
        }

        public Assembly Build()
        {
            // Simple dummy code to compile

            var syntaxTree = CSharpSyntaxTree.ParseText($$"""
            namespace DynamicAssembly_{{Guid.NewGuid():N}} 
            { 
                public class DummyClass { } 
            }
            """);

            var assemblyName = _assemblyName == null ? new AssemblyName() : new AssemblyName(_assemblyName);
            var references = new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };
            var compilation = CSharpCompilation.Create(assemblyName.FullName, [syntaxTree], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var manifestResources = _resources.Select(kvp => new ResourceDescription(kvp.Key, () => new MemoryStream(kvp.Value), isPublic: true));
            using var dllStream = new MemoryStream();
            var emitResult = compilation.Emit(dllStream, manifestResources: manifestResources);

            if (!emitResult.Success)
            {
                var errors = string.Join(Environment.NewLine, emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
                throw new InvalidOperationException($"Failed to compile assembly: {errors}");
            }

            return Assembly.Load(dllStream.ToArray());
        }
    }

    #endregion
}