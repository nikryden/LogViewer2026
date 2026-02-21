using FluentAssertions;
using LogViewer2026.Core.Configuration;

namespace LogViewer2026.Core.Tests.Configuration;

public class AppSettingsTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var settings = new AppSettings();

        settings.LookingGlassContextLines.Should().Be(5);
        settings.AutoUpdateLookingGlass.Should().BeFalse();
        settings.FilterSearchResults.Should().BeFalse();
        settings.ShowLookingGlass.Should().BeTrue();
        settings.CacheSize.Should().Be(10000);
        settings.MaxFileSizeMB.Should().Be(2048);
        settings.EnableIndexing.Should().BeTrue();
        settings.Theme.Should().Be("Light");
        settings.MaxRecentFiles.Should().Be(10);
        settings.LoadMultipleFiles.Should().BeTrue();
    }

    [Fact]
    public void RecentFiles_ShouldDefaultToEmptyList()
    {
        var settings = new AppSettings();

        settings.RecentFiles.Should().NotBeNull();
        settings.RecentFiles.Should().BeEmpty();
    }

    [Fact]
    public void LastOpenedFolder_ShouldDefaultToEmptyString()
    {
        var settings = new AppSettings();

        settings.LastOpenedFolder.Should().BeEmpty();
    }

    [Fact]
    public void OutputTemplate_ShouldHaveDefaultSerilogFormat()
    {
        var settings = new AppSettings();

        settings.OutputTemplate.Should().Contain("Timestamp");
        settings.OutputTemplate.Should().Contain("Level");
        settings.OutputTemplate.Should().Contain("Message");
    }
}
