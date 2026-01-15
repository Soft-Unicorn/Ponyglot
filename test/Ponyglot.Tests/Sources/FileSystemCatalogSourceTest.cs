using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using NSubstitute;
using Ponyglot.Sources;
using Ponyglot.Tests._TestUtils;
using Xunit;

namespace Ponyglot.Tests.Sources;

public class FileSystemCatalogSourceTest : IDisposable
{
    private readonly DirectoryInfo _rootDirectory;
    private readonly FileSystemCatalogSourceOptions _options;
    private FileSystemCatalogSourceDouble _sut;

    public FileSystemCatalogSourceTest()
    {
        _rootDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("D")));
        _options = new FileSystemCatalogSourceOptions();
        _sut = new FileSystemCatalogSourceDouble(Substitute.For<ICatalogReader>(), _rootDirectory, _options);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _rootDirectory.Refresh();
        if (_rootDirectory.Exists)
        {
            _rootDirectory.Delete(recursive: true);
        }
    }

    [Theory]
    [InlineData(true, "catalogReader")]
    [InlineData(true, "rootDirectory")]
    [InlineData(true, "options")]
    [InlineData(false, "catalogReader")]
    [InlineData(false, "rootDirectory")]
    public void Constructor_ArgumentIsNull_Throws(bool overloadWithOptions, string parameterName)
    {
        // Arrange

        // Act
        Action action = overloadWithOptions switch
        {
            true => () => _ = new FileSystemCatalogSource(
                parameterName == "catalogReader" ? null! : Substitute.For<ICatalogReader>(),
                parameterName == "rootDirectory" ? null! : _rootDirectory,
                parameterName == "options" ? null! : _options),
            false => () => _ = new FileSystemCatalogSource(
                parameterName == "catalogReader" ? null! : Substitute.For<ICatalogReader>(),
                parameterName == "rootDirectory" ? null! : _rootDirectory),
        };

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName(parameterName);
    }

    [Fact]
    public void RootDirectory_Created_ReturnsTheConstructorValue()
    {
        // Arrange

        // Act
        var rootDirectory = _sut.RootDirectory;

        // Assert
        rootDirectory.Should().BeSameAs(_rootDirectory);
    }

    [Fact]
    public void Options_CreatedWithOptions_ReturnsTheConstructorValue()
    {
        // Arrange

        // Act
        var options = _sut.Options;

        // Assert
        options.Should().BeSameAs(_options);
    }

    [Fact]
    public void Options_CreatedWithoutOptions_ReturnsTheDefaultOptions()
    {
        // Arrange
        _sut = new FileSystemCatalogSourceDouble(Substitute.For<ICatalogReader>(), _rootDirectory);

        // Act
        var options = _sut.Options;

        // Assert
        options.Should().BeEquivalentTo(new FileSystemCatalogSourceOptions());
    }

    [Fact]
    public async Task EnumerateResourcesAsync_FileFound_ReturnsResourcesWithCorrectAttributes()
    {
        // Arrange
        var files = new[]
        {
            CreateFile(path: [".my-file-a.x.y.z"]),
            CreateFile(path: [".my-file-b"]),
            CreateFile(path: ["my-file-c.x.y.z"]),
            CreateFile(path: ["my-file-d"]),
        };

        _options.FileSearchOptions.AttributesToSkip = FileAttributes.None; // Allow hidden files because of the leading dot

        // Act
        var result = await _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync();

        // Assert
        result.OrderBy(r => r.Name, StringComparer.Ordinal).Should().SatisfyRespectively(
            first =>
            {
                first.Uid.Should().Be($"FileSystem:File={files[0].FullName}");
                first.Name.Should().Be(files[0].FullName);
                first.CatalogName.Should().Be("my-file-a");
            },
            second =>
            {
                second.Uid.Should().Be($"FileSystem:File={files[1].FullName}");
                second.Name.Should().Be(files[1].FullName);
                second.CatalogName.Should().Be("my-file-b");
            },
            third =>
            {
                third.Uid.Should().Be($"FileSystem:File={files[2].FullName}");
                third.Name.Should().Be(files[2].FullName);
                third.CatalogName.Should().Be("my-file-c");
            },
            fourth =>
            {
                fourth.Uid.Should().Be($"FileSystem:File={files[3].FullName}");
                fourth.Name.Should().Be(files[3].FullName);
                fourth.CatalogName.Should().Be("my-file-d");
            });
    }

    [Fact]
    public async Task EnumerateResourcesAsync_RecursiveTrue_ReturnsFilesInTheRootAndChildChildFolders()
    {
        // Arrange
        var files = new[]
        {
            CreateFile(path: ["my-file.a"]),
            CreateFile(path: ["my-file.b"]),
            CreateFile(path: ["my-sub-dir", "my-file.c"]),
            CreateFile(path: ["my-sub-dir", "my-file.d"]),
        };

        _options.FileSearchOptions.RecurseSubdirectories = true;

        // Act
        var result = await _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync();

        // Assert
        result.Select(r => r.Name).Should().BeEquivalentTo(
            files[0].FullName,
            files[1].FullName,
            files[2].FullName,
            files[3].FullName);
    }

    [Fact]
    public async Task EnumerateResourcesAsync_RecursiveFalse_ReturnsFilesInTheRootFolderOnly()
    {
        // Arrange
        var files = new[]
        {
            CreateFile(path: ["my-file.a"]),
            CreateFile(path: ["my-file.b"]),
            CreateFile(path: ["my-sub-dir", "my-file.c"]),
            CreateFile(path: ["my-sub-dir", "my-file.d"]),
        };

        _options.FileSearchOptions.RecurseSubdirectories = false;

        // Act
        var result = await _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync();

        // Assert
        result.Select(r => r.Name).Should().BeEquivalentTo(
            files[0].FullName,
            files[1].FullName);
    }

    [Fact]
    public async Task EnumerateResourcesAsync_EmptyFile_SkipsTheFile()
    {
        // Arrange
        CreateFile(path: ["empty.test"], content: null);

        // Act
        var result = await _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(FileAttributes.Hidden, false)]
    [InlineData(FileAttributes.None, true)]
    public async Task EnumerateResourcesAsync_HiddenFile_SkipsTheFileIfOptionsSaysSo(FileAttributes attributesToSkip, bool expectedFound)
    {
        // Arrange
        CreateFile(path: [".hidden.test"], attributes: FileAttributes.Hidden);

        _options.FileSearchOptions.AttributesToSkip = attributesToSkip;
        // Act
        var result = await _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync();

        // Assert
        result.Should().HaveCount(expectedFound ? 1 : 0);
    }

    [Theory]
    [CombinatorialData]
    public async Task EnumerateResourcesAsync_FilterSet_CallsFilterForEachFile(bool recursive)
    {
        // Arrange
        var files = new[]
        {
            CreateFile(path: ["my-file.a"]),
            CreateFile(path: ["my-file.b"]),
            CreateFile(path: recursive ? ["my-sub-dir", "my-file.c"] : ["my-file.c"]),
            CreateFile(path: recursive ? ["my-sub-dir", "my-file.d"] : ["my-file.d"]),
        };

        _options.FileSearchOptions.RecurseSubdirectories = recursive;

        var filteredFiles = new List<string>();
        _options.Filter = f =>
        {
            filteredFiles.Add(f.FullName);
            return true;
        };

        // Act
        await _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync();

        // Assert
        filteredFiles.Should().BeEquivalentTo(files[0].FullName, files[1].FullName, files[2].FullName, files[3].FullName);
    }

    [Theory]
    [CombinatorialData]
    public async Task EnumerateResourcesAsync_FilterSet_ReturnsOnlyFilesThatSatisfyTheFilter(bool recursive)
    {
        // Arrange
        var files = new[]
        {
            CreateFile(path: ["my-file.a"]),
            CreateFile(path: ["my-file.b"]),
            CreateFile(path: recursive ? ["my-sub-dir", "my-file.c"] : ["my-file.c"]),
            CreateFile(path: recursive ? ["my-sub-dir", "my-file.d"] : ["my-file.d"]),
        };

        _options.FileSearchOptions.RecurseSubdirectories = recursive;
        _options.Filter = f => f.Name.EndsWith('a') || f.Name.EndsWith('c');

        // Act
        var result = await _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync();

        // Assert
        result.Select(r => r.Name).Should().BeEquivalentTo(
            files[0].FullName,
            files[2].FullName);
    }

    [Theory]
    [CombinatorialData]
    public async Task EnumerateResourcesAsync_CatalogNameResolverSet_CallsResolverForEachFile(bool recursive)
    {
        // Arrange
        var files = new[]
        {
            CreateFile(path: ["my-file.a"]),
            CreateFile(path: ["my-file.b"]),
            CreateFile(path: recursive ? ["my-sub-dir", "my-file.c"] : ["my-file.c"]),
            CreateFile(path: recursive ? ["my-sub-dir", "my-file.d"] : ["my-file.d"]),
        };

        _options.FileSearchOptions.RecurseSubdirectories = recursive;

        var resolvedFiles = new List<string>();
        _options.CatalogNameResolver = f =>
        {
            resolvedFiles.Add(f.FullName);
            return "";
        };

        // Act
        await _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync();

        // Assert
        resolvedFiles.Should().BeEquivalentTo(files[0].FullName, files[1].FullName, files[2].FullName, files[3].FullName);
    }

    [Theory]
    [CombinatorialData]
    public async Task EnumerateResourcesAsync_CatalogNameResolverSet_ReturnsResourcesWithTheResolvedCatalogName(bool recursive)
    {
        // Arrange
        CreateFile(path: ["my-file.a"]);
        CreateFile(path: ["my-file.b"]);
        CreateFile(path: recursive ? ["my-sub-dir", "my-file.c"] : ["my-file.c"]);
        CreateFile(path: recursive ? ["my-sub-dir", "my-file.d"] : ["my-file.d"]);

        _options.FileSearchOptions.RecurseSubdirectories = recursive;
        _options.CatalogNameResolver = f => $"my-catalog-{Path.GetExtension(f.Name)[1..]}";

        // Act
        var result = await _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync();

        // Assert
        result.Select(r => r.CatalogName).Should().BeEquivalentTo(
            "my-catalog-a",
            "my-catalog-b",
            "my-catalog-c",
            "my-catalog-d");
    }

    [Fact]
    public async Task ReturnedResourceOpenAsync_FileExists_OpensTheCorrectFile()
    {
        // Arrange
        var file = CreateFile(path: ["my-file.a.b.c"], content: "my-content");

        var resource = (await _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync()).Single();

        // Act
        await using var stream = await resource.OpenAsync(TestContext.Current.CancellationToken);

        // Assert
        stream.Should().BeOfType<FileStream>().Which.Should().Satisfy<FileStream>(s =>
        {
            s.Name.Should().Be(file.FullName);
            s.CanRead.Should().BeTrue();
            s.CanWrite.Should().BeFalse();
            s.IsAsync.Should().BeTrue();
        });

        stream.AsString().Should().Be("my-content");
    }

    [Fact]
    public async Task ReturnedResourceOpenAsync_FileIsAlreadyOpenForRead_DoesNotThrow()
    {
        // Arrange
        var file = CreateFile(path: ["my-file.a.b.c"], content: "my-content");

        var resource = (await _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync()).Single();

        await using var _ = file.OpenRead();

        // Act
        var action = async () => await (await resource.OpenAsync(TestContext.Current.CancellationToken)).DisposeAsync();

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ReturnedResourceOpenAsync_FileIsAlreadyOpenForWrite_Throws()
    {
        // Arrange
        var file = CreateFile(path: ["my-file.a.b.c"], content: "my-content");

        var resource = (await _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync()).Single();

        await using var _ = file.OpenWrite();

        // Act
        var action = async () => await (await resource.OpenAsync(TestContext.Current.CancellationToken)).DisposeAsync();

        // Assert
        await action.Should().ThrowAsync<IOException>().WithMessage($"*{file.FullName}*");
    }

    [Fact]
    public async Task ReturnedResourceOpenAsync_FileDoesNotExists_OpensTheCorrectFile()
    {
        // Arrange
        var file = CreateFile(path: ["my-file.a.b.c"], content: "my-content");

        var resource = (await _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync()).Single();

        file.Delete();

        // Act
        var action = async () => await (await resource.OpenAsync(TestContext.Current.CancellationToken)).DisposeAsync();

        // Assert
        (await action.Should().ThrowExactlyAsync<FileNotFoundException>().WithMessage($"*{file.FullName}*"))
            .Which.FileName.Should().Be(file.FullName);
    }

    [Fact]
    public async Task ReturnedResourceOpenAsync_CancellationOccurs_Throws()
    {
        // Arrange
        CreateFile(path: ["my-file.a.b.c"], content: "my-content");

        var resource = (await _sut.EnumerateResourcesAsync(TestContext.Current.CancellationToken).RealizeAsync()).Single();

        // Act
        var action = async () => await (await resource.OpenAsync(new CancellationToken(canceled: true))).DisposeAsync();

        // Assert
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    #region Helpers

    private FileInfo CreateFile(string[] path, string? content = "my-content", FileAttributes? attributes = null)
    {
        var file = new FileInfo(Path.Combine([_rootDirectory.FullName, ..path]));

        file.Directory?.Create();

        if (!string.IsNullOrEmpty(content))
        {
            using var stream = file.OpenWrite();
            stream.Write(Encoding.UTF8.GetBytes(content));
        }

        if (attributes != null)
        {
            file.Attributes = attributes.Value;
        }

        return file;
    }

    private class FileSystemCatalogSourceDouble : FileSystemCatalogSource
    {
        public FileSystemCatalogSourceDouble(ICatalogReader catalogReader, DirectoryInfo rootDirectory)
            : base(catalogReader, rootDirectory)
        {
        }

        public FileSystemCatalogSourceDouble(ICatalogReader catalogReader, DirectoryInfo rootDirectory, FileSystemCatalogSourceOptions options)
            : base(catalogReader, rootDirectory, options)
        {
        }

        public new DirectoryInfo RootDirectory => base.RootDirectory;
        public new FileSystemCatalogSourceOptions Options => base.Options;

        public new IAsyncEnumerable<StreamResource> EnumerateResourcesAsync(CancellationToken cancellationToken = default) => base.EnumerateResourcesAsync(cancellationToken);
    }

    #endregion
}