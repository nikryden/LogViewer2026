using FluentAssertions;
using LogViewer2026.Core.Configuration;
using LogViewer2026.Core.Models;
using LogViewer2026.Core.Services;

namespace LogViewer2026.Core.Tests.Services;

public class FilterConfigurationServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _testFilePath;

    public FilterConfigurationServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"filter_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        _testFilePath = Path.Combine(_testDir, "filters.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }

    [Fact]
    public async Task GetDefaultAsync_ShouldReturnDefaultFilters()
    {
        var service = new FilterConfigurationService();

        var result = await service.GetDefaultAsync(TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Filters.Should().NotBeEmpty();
        result.Filters.Should().Contain(f => f.Name == "Errors Only");
        result.Filters.Should().Contain(f => f.Name == "Warnings and Above");
        result.Filters.Should().Contain(f => f.Name == "Info and Above");
    }

    [Fact]
    public async Task GetDefaultAsync_ErrorsOnlyFilter_ShouldHaveErrorLevel()
    {
        var service = new FilterConfigurationService();

        var result = await service.GetDefaultAsync(TestContext.Current.CancellationToken);
        var errorsOnly = result.Filters.First(f => f.Name == "Errors Only");

        errorsOnly.LogLevel.Should().Be(LogLevel.Error);
    }

    [Fact]
    public async Task SaveAndLoad_ShouldRoundtripFilters()
    {
        var service = new FilterConfigurationService();
        var collection = new FilterConfigurationCollection
        {
            Filters =
            [
                new FilterConfiguration
                {
                    Name = "Test Filter",
                    LogLevel = LogLevel.Warning,
                    SearchText = "test search"
                }
            ]
        };

        await service.SaveAsync(collection, _testFilePath, TestContext.Current.CancellationToken);
        var loaded = await service.LoadAsync(_testFilePath, TestContext.Current.CancellationToken);

        loaded.Filters.Should().HaveCount(1);
        loaded.Filters[0].Name.Should().Be("Test Filter");
        loaded.Filters[0].LogLevel.Should().Be(LogLevel.Warning);
        loaded.Filters[0].SearchText.Should().Be("test search");
    }

    [Fact]
    public async Task LoadAsync_WhenFileDoesNotExist_ShouldReturnEmptyCollection()
    {
        var service = new FilterConfigurationService();
        var nonExistentPath = Path.Combine(_testDir, "nonexistent.json");

        var result = await service.LoadAsync(nonExistentPath, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Filters.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_WithCorruptedFile_ShouldReturnEmptyCollection()
    {
        await File.WriteAllTextAsync(_testFilePath, "not valid json{{{", TestContext.Current.CancellationToken);
        var service = new FilterConfigurationService();

        var result = await service.LoadAsync(_testFilePath, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Filters.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAsync_ShouldCreateDirectoryIfNeeded()
    {
        var nestedPath = Path.Combine(_testDir, "nested", "filters.json");
        var service = new FilterConfigurationService();
        var collection = new FilterConfigurationCollection
        {
            Filters = [new FilterConfiguration { Name = "Test" }]
        };

        await service.SaveAsync(collection, nestedPath, TestContext.Current.CancellationToken);

        File.Exists(nestedPath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveAndLoad_WithMultipleFilters_ShouldPreserveAll()
    {
        var service = new FilterConfigurationService();
        var collection = new FilterConfigurationCollection
        {
            Filters =
            [
                new FilterConfiguration { Name = "Filter 1", LogLevel = LogLevel.Error },
                new FilterConfiguration { Name = "Filter 2", LogLevel = LogLevel.Warning },
                new FilterConfiguration { Name = "Filter 3", SearchText = "exception" }
            ],
            LastUsedFilter = "Filter 2"
        };

        await service.SaveAsync(collection, _testFilePath, TestContext.Current.CancellationToken);
        var loaded = await service.LoadAsync(_testFilePath, TestContext.Current.CancellationToken);

        loaded.Filters.Should().HaveCount(3);
        loaded.LastUsedFilter.Should().Be("Filter 2");
    }
}
