using FluentAssertions;
using LogViewer2026.Core.Configuration;
using LogViewer2026.Core.Models;
using LogViewer2026.Core.Services;
using LogViewer2026.Infrastructure.Services;
using LogViewer2026.UI.ViewModels;
using Moq;

namespace LogViewer2026.UI.Tests.ViewModels;

public class MainViewModelTests
{
    private readonly Mock<ILogService> _mockLogService;
    private readonly Mock<IMultiFileLogService> _mockMultiFileLogService;
    private readonly Mock<IFilterConfigurationService> _mockFilterConfigService;
    private readonly Mock<ISettingsService> _mockSettingsService;

    public MainViewModelTests()
    {
        _mockLogService = new Mock<ILogService>();
        _mockMultiFileLogService = new Mock<IMultiFileLogService>();
        _mockFilterConfigService = new Mock<IFilterConfigurationService>();
        _mockSettingsService = new Mock<ISettingsService>();

        _mockSettingsService.Setup(s => s.LoadAsync()).ReturnsAsync(new AppSettings());
        _mockFilterConfigService.Setup(s => s.GetDefaultAsync())
            .ReturnsAsync(new FilterConfigurationCollection());
        _mockFilterConfigService.Setup(s => s.LoadAsync(It.IsAny<string>()))
            .ReturnsAsync(new FilterConfigurationCollection());
    }

    private MainViewModel CreateViewModel() =>
        new(_mockLogService.Object, _mockMultiFileLogService.Object,
            _mockFilterConfigService.Object, _mockSettingsService.Object);

    [Fact]
    public void Constructor_ShouldInitializeDefaultProperties()
    {
        var vm = CreateViewModel();

        vm.StatusText.Should().Be("Ready");
        vm.LogText.Should().BeEmpty();
        vm.OriginalLogText.Should().BeEmpty();
        vm.SearchText.Should().BeEmpty();
        vm.TotalSearchResults.Should().Be(0);
        vm.CurrentSearchResultIndex.Should().Be(-1);
        vm.IsLoading.Should().BeFalse();
        vm.SelectedLogLevelOption.Should().NotBeNull();
        vm.SelectedLogLevelOption!.DisplayName.Should().Be("All");
    }

    [Fact]
    public void Constructor_ShouldProvideAllLogLevelOptions()
    {
        var vm = CreateViewModel();

        vm.AvailableLogLevels.Should().HaveCount(7);
        vm.AvailableLogLevels.First().DisplayName.Should().Be("All");
        vm.AvailableLogLevels.First().Value.Should().BeNull();
    }

    [Fact]
    public void FindNext_ShouldAdvanceToNextResult()
    {
        var vm = CreateViewModel();
        vm.TotalSearchResults = 5;
        vm.CurrentSearchResultIndex = 0;

        vm.FindNextCommand.Execute(null);

        vm.CurrentSearchResultIndex.Should().Be(1);
    }

    [Fact]
    public void FindNext_ShouldWrapAroundToFirst()
    {
        var vm = CreateViewModel();
        vm.TotalSearchResults = 3;
        vm.CurrentSearchResultIndex = 2;

        vm.FindNextCommand.Execute(null);

        vm.CurrentSearchResultIndex.Should().Be(0);
    }

    [Fact]
    public void FindNext_WithNoResults_ShouldDoNothing()
    {
        var vm = CreateViewModel();
        vm.TotalSearchResults = 0;
        vm.CurrentSearchResultIndex = -1;

        vm.FindNextCommand.Execute(null);

        vm.CurrentSearchResultIndex.Should().Be(-1);
    }

    [Fact]
    public void FindPrevious_ShouldGoToPreviousResult()
    {
        var vm = CreateViewModel();
        vm.TotalSearchResults = 5;
        vm.CurrentSearchResultIndex = 3;

        vm.FindPreviousCommand.Execute(null);

        vm.CurrentSearchResultIndex.Should().Be(2);
    }

    [Fact]
    public void FindPrevious_ShouldWrapAroundToLast()
    {
        var vm = CreateViewModel();
        vm.TotalSearchResults = 5;
        vm.CurrentSearchResultIndex = 0;

        vm.FindPreviousCommand.Execute(null);

        vm.CurrentSearchResultIndex.Should().Be(4);
    }

    [Fact]
    public void FindPrevious_WithNoResults_ShouldDoNothing()
    {
        var vm = CreateViewModel();
        vm.TotalSearchResults = 0;
        vm.CurrentSearchResultIndex = -1;

        vm.FindPreviousCommand.Execute(null);

        vm.CurrentSearchResultIndex.Should().Be(-1);
    }

    [Fact]
    public void FindNext_ShouldFireSearchResultChangedEvent()
    {
        var vm = CreateViewModel();
        vm.TotalSearchResults = 3;
        vm.CurrentSearchResultIndex = 0;
        int? receivedIndex = null;
        vm.OnSearchResultChanged += index => receivedIndex = index;

        vm.FindNextCommand.Execute(null);

        receivedIndex.Should().Be(1);
    }

    [Fact]
    public void ApplySearchFilter_ShouldFilterLinesToMatchingText()
    {
        var vm = CreateViewModel();
        vm.OriginalLogText = "2024-01-01 [INF] Hello world\n2024-01-01 [ERR] Error occurred\n2024-01-01 [INF] Another info";
        vm.LogText = vm.OriginalLogText;
        vm.SearchText = "Error";
        vm.FilterSearchResults = true;

        vm.ApplySearchFilter();

        vm.LogText.Should().Contain("Error occurred");
        vm.LogText.Should().NotContain("Hello world");
        vm.LogText.Should().NotContain("Another info");
    }

    [Fact]
    public void ApplySearchFilter_WithEmptySearch_ShouldRestoreOriginalText()
    {
        var vm = CreateViewModel();
        vm.OriginalLogText = "line1\nline2\nline3";
        vm.LogText = "line2";
        vm.SearchText = string.Empty;
        vm.FilterSearchResults = true;

        vm.ApplySearchFilter();

        vm.LogText.Should().Be(vm.OriginalLogText);
    }

    [Fact]
    public void ApplySearchFilter_WithFilterDisabled_ShouldRestoreOriginalText()
    {
        var vm = CreateViewModel();
        vm.OriginalLogText = "line1\nline2\nline3";
        vm.LogText = "line2";
        vm.SearchText = "line2";
        vm.FilterSearchResults = false;

        vm.ApplySearchFilter();

        vm.LogText.Should().Be(vm.OriginalLogText);
    }

    [Fact]
    public void ApplySearchFilter_ShouldBeCaseInsensitive()
    {
        var vm = CreateViewModel();
        vm.OriginalLogText = "Hello World\nGoodbye World\nHELLO again";
        vm.LogText = vm.OriginalLogText;
        vm.SearchText = "hello";
        vm.FilterSearchResults = true;

        vm.ApplySearchFilter();

        vm.LogText.Should().Contain("Hello World");
        vm.LogText.Should().Contain("HELLO again");
        vm.LogText.Should().NotContain("Goodbye");
    }

    [Fact]
    public void ApplySearchFilter_WithLevelFilter_ShouldApplyBothFilters()
    {
        var vm = CreateViewModel();
        vm.OriginalLogText = "2024-01-01 [INF] Hello info\n2024-01-01 [ERR] Error hello\n2024-01-01 [ERR] Error goodbye";
        vm.LogText = vm.OriginalLogText;
        vm.SearchText = "hello";
        vm.FilterLevel = LogLevel.Error;
        vm.FilterSearchResults = true;

        vm.ApplySearchFilter();

        vm.LogText.Should().Contain("Error hello");
        vm.LogText.Should().NotContain("Hello info");
        vm.LogText.Should().NotContain("Error goodbye");
    }

    [Fact]
    public void CopyToSearch_ShouldCopySelectedTextToSearchText()
    {
        var vm = CreateViewModel();
        vm.SelectedText = "test search term";

        vm.CopyToSearchCommand.Execute(null);

        vm.SearchText.Should().Be("test search term");
    }

    [Fact]
    public void CopyToSearch_WithNoSelection_ShouldNotChangeSearchText()
    {
        var vm = CreateViewModel();
        vm.SearchText = "existing";
        vm.SelectedText = string.Empty;

        vm.CopyToSearchCommand.Execute(null);

        vm.SearchText.Should().Be("existing");
    }

    [Fact]
    public void ClearFilters_ShouldResetAllFilterProperties()
    {
        var vm = CreateViewModel();
        vm.OriginalLogText = "line1\nline2\nline3";
        vm.LogText = "line2";
        vm.SearchText = "search";
        vm.FilterLevel = LogLevel.Error;

        vm.ClearFiltersCommand.Execute(null);

        vm.SearchText.Should().BeEmpty();
        vm.FilterLevel.Should().BeNull();
        vm.FilterStartTime.Should().BeNull();
        vm.FilterEndTime.Should().BeNull();
        vm.LogText.Should().Be(vm.OriginalLogText);
    }

    [Fact]
    public void ClearFilters_WithNoData_ShouldSetStatusCleared()
    {
        var vm = CreateViewModel();

        vm.ClearFiltersCommand.Execute(null);

        vm.StatusText.Should().Be("Filters cleared");
    }

    [Fact]
    public void UpdateLookingGlass_WithEmptyOriginalText_ShouldClearLookingGlass()
    {
        var vm = CreateViewModel();
        vm.OriginalLogText = string.Empty;

        vm.UpdateLookingGlass(1, 0, 0, "some text");

        vm.SelectedLookingGlas.Text.Should().BeEmpty();
    }

    [Fact]
    public void UpdateLookingGlass_ShouldShowContextAroundSelectedLine()
    {
        var vm = CreateViewModel();
        var lines = new[]
        {
            "line 0", "line 1", "line 2", "line 3", "line 4",
            "line 5", "line 6", "line 7", "line 8", "line 9"
        };
        var text = string.Join("\n", lines);
        vm.OriginalLogText = text;

        // Select line 5 (1-based: lineNumber=6), default context = 5 lines
        var displayedText = text;
        vm.UpdateLookingGlass(6, 0, 0, displayedText);

        vm.SelectedLookingGlas.Text.Should().Contain("line 5");
        vm.SelectedLookingGlas.Text.Should().Contain("line 1");
        vm.SelectedLookingGlas.StartingLineNumber.Should().BeGreaterThan(0);
    }

    [Fact]
    public void UpdateLookingGlass_WithInvalidLineNumber_ShouldClearLookingGlass()
    {
        var vm = CreateViewModel();
        vm.OriginalLogText = "line1\nline2";

        vm.UpdateLookingGlass(0, 0, 0, "line1\nline2");

        vm.SelectedLookingGlas.Text.Should().BeEmpty();
    }

    [Fact]
    public void UpdateLookingGlass_WithLineNumberBeyondRange_ShouldClearLookingGlass()
    {
        var vm = CreateViewModel();
        vm.OriginalLogText = "line1\nline2";

        vm.UpdateLookingGlass(100, 0, 0, "line1\nline2");

        vm.SelectedLookingGlas.Text.Should().BeEmpty();
    }

    [Fact]
    public void FindNext_ShouldUpdateStatusText()
    {
        var vm = CreateViewModel();
        vm.TotalSearchResults = 10;
        vm.CurrentSearchResultIndex = 0;

        vm.FindNextCommand.Execute(null);

        vm.StatusText.Should().Contain("Result 2 of 10");
    }

    [Fact]
    public void FindPrevious_ShouldUpdateStatusText()
    {
        var vm = CreateViewModel();
        vm.TotalSearchResults = 10;
        vm.CurrentSearchResultIndex = 5;

        vm.FindPreviousCommand.Execute(null);

        vm.StatusText.Should().Contain("Result 5 of 10");
    }
}
