using FluentAssertions;
using LogViewer2026.Core.Configuration;
using LogViewer2026.Core.Services;

namespace LogViewer2026.Core.Tests.Services;

public class SettingsServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _testSettingsPath;

    public SettingsServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"settings_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
        _testSettingsPath = Path.Combine(_testDir, "settings.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }

    [Fact]
    public async Task LoadAsync_WhenFileDoesNotExist_ShouldReturnDefaults()
    {
        var service = new SettingsService(_testSettingsPath);

        var settings = await service.LoadAsync();

        settings.Should().NotBeNull();
        settings.LookingGlassContextLines.Should().Be(5);
        settings.AutoUpdateLookingGlass.Should().BeFalse();
        settings.ShowLookingGlass.Should().BeTrue();
        settings.FilterSearchResults.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAndLoad_ShouldRoundtripSettings()
    {
        var service = new SettingsService(_testSettingsPath);
        var settings = new AppSettings
        {
            LookingGlassContextLines = 10,
            AutoUpdateLookingGlass = true,
            ShowLookingGlass = false,
            FilterSearchResults = true,
            Theme = "Dark",
            CacheSize = 20000
        };

        await service.SaveAsync(settings);
        var loaded = await service.LoadAsync();

        loaded.LookingGlassContextLines.Should().Be(10);
        loaded.AutoUpdateLookingGlass.Should().BeTrue();
        loaded.ShowLookingGlass.Should().BeFalse();
        loaded.FilterSearchResults.Should().BeTrue();
        loaded.Theme.Should().Be("Dark");
        loaded.CacheSize.Should().Be(20000);
    }

    [Fact]
    public async Task LoadAsync_WithCorruptedFile_ShouldReturnDefaults()
    {
        await File.WriteAllTextAsync(_testSettingsPath, "this is not json{{{");
        var service = new SettingsService(_testSettingsPath);

        var settings = await service.LoadAsync();

        settings.Should().NotBeNull();
        settings.LookingGlassContextLines.Should().Be(5);
    }

    [Fact]
    public void GetSettingsPath_ShouldReturnConfiguredPath()
    {
        var service = new SettingsService(_testSettingsPath);

        var path = service.GetSettingsPath();

        path.Should().Be(_testSettingsPath);
    }

    [Fact]
    public async Task SaveAsync_ShouldCreateDirectoryIfNeeded()
    {
        var nestedPath = Path.Combine(_testDir, "nested", "deep", "settings.json");
        var service = new SettingsService(nestedPath);

        await service.SaveAsync(new AppSettings());

        File.Exists(nestedPath).Should().BeTrue();
    }
}
