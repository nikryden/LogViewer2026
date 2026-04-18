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

public class FilterLogTextTests
{
    private const string SerilogLine1 = "[2026-02-12 12:59:59.038 +01:00][][INF][Aptus.MultiserverEx.ApiCommunicator.DeviceApiCommunicator] Method: GET";
    private const string SerilogLine2 = "[2026-02-12 13:30:59.038 +01:00][][INF][Aptus.MultiserverEx.ApiCommunicator.DeviceApiCommunicator] URI: \"device/Styra4000/BuSsBBNDR\"";
    private const string SerilogLine3 = "[2026-02-12 13:40:59.038 +01:00][][INF][Aptus.MultiserverEx.ApiCommunicator.DeviceApiCommunicator] Version: 1.1";
    private const string SerilogLine4 = "[2026-02-12 15:59:59.038 +01:00][][INF][Aptus.MultiserverEx.ApiCommunicator.DeviceApiCommunicator] Headers:";

    private static string AllLines => string.Join("\n", SerilogLine1, SerilogLine2, SerilogLine3, SerilogLine4);

    [Fact]
    public void FilterLogText_WithNoFilters_ReturnsAllLines()
    {
        var result = MainViewModel.FilterLogText(AllLines, null, null, null);

        result.Should().Contain("Method: GET");
        result.Should().Contain("URI:");
        result.Should().Contain("Version:");
        result.Should().Contain("Headers:");
    }

    [Fact]
    public void FilterLogText_SerilogFormat_FiltersByStartDate()
    {
        var start = new DateTime(2026, 2, 12, 13, 0, 0);

        var result = MainViewModel.FilterLogText(AllLines, null, start, null);

        result.Should().NotContain("Method: GET");   // 12:59 is before 13:00
        result.Should().Contain("URI:");             // 13:30 passes
        result.Should().Contain("Version:");         // 13:40 passes
        result.Should().Contain("Headers:");         // 15:59 passes
    }

    [Fact]
    public void FilterLogText_SerilogFormat_FiltersByEndDate()
    {
        var end = new DateTime(2026, 2, 12, 13, 59, 0);

        var result = MainViewModel.FilterLogText(AllLines, null, null, end);

        result.Should().Contain("Method: GET");      // 12:59 passes
        result.Should().Contain("URI:");             // 13:30 passes
        result.Should().Contain("Version:");         // 13:40 passes
        result.Should().NotContain("Headers:");      // 15:59 is after 13:59
    }

    [Fact]
    public void FilterLogText_SerilogFormat_FiltersByDateRange()
    {
        var start = new DateTime(2026, 2, 12, 13, 0, 0);
        var end = new DateTime(2026, 2, 12, 13, 59, 0);

        var result = MainViewModel.FilterLogText(AllLines, null, start, end);

        result.Should().NotContain("Method: GET");   // 12:59 is before start
        result.Should().Contain("URI:");             // 13:30 is in range
        result.Should().Contain("Version:");         // 13:40 is in range
        result.Should().NotContain("Headers:");      // 15:59 is after end
    }

    [Fact]
    public void FilterLogText_SerilogFormat_ExactDateFilter_ReturnsOnlyThatDay()
    {
        var start = new DateTime(2026, 2, 12, 0, 0, 0);
        var end = new DateTime(2026, 2, 12, 23, 59, 59);

        var result = MainViewModel.FilterLogText(AllLines, null, start, end);

        result.Should().Contain("Method: GET");
        result.Should().Contain("URI:");
        result.Should().Contain("Version:");
        result.Should().Contain("Headers:");
    }

    [Fact]
    public void FilterLogText_SerilogFormat_DifferentDateFilter_ReturnsNothing()
    {
        // Filter for a completely different date — all lines should be excluded
        var start = new DateTime(2025, 1, 1, 0, 0, 0);
        var end = new DateTime(2025, 1, 1, 23, 59, 59);

        var result = MainViewModel.FilterLogText(AllLines, null, start, end);

        result.Should().BeEmpty();
    }

    [Fact]
    public void FilterLogText_ContinuationLines_AreAlwaysIncluded()
    {
        // Lines without a timestamp (stack traces, multi-line messages) should pass through
        var text = SerilogLine1 + "\n   at Namespace.Class.Method() in File.cs:line 42\n" + SerilogLine4;
        var start = new DateTime(2026, 2, 12, 13, 0, 0);

        var result = MainViewModel.FilterLogText(text, null, start, null);

        // Stack trace line has no timestamp, so it passes through
        result.Should().Contain("at Namespace.Class.Method()");
        result.Should().Contain("Headers:");
        result.Should().NotContain("Method: GET");
    }

    [Fact]
    public void FilterLogText_CombinesLevelAndDateFilters()
    {
        var lines = string.Join("\n",
            "[2026-02-12 13:30:00.000 +01:00][][INF][Source] Info message",
            "[2026-02-12 13:30:00.000 +01:00][][ERR][Source] Error message",
            "[2026-02-12 15:00:00.000 +01:00][][ERR][Source] Late error");

        var start = new DateTime(2026, 2, 12, 13, 0, 0);
        var end = new DateTime(2026, 2, 12, 14, 0, 0);

        var result = MainViewModel.FilterLogText(lines, LogLevel.Error, start, end);

        result.Should().NotContain("Info message");
        result.Should().Contain("Error message");
        result.Should().NotContain("Late error");
    }

    [Fact]
    public void FilterLogText_ThenSearch_SearchOperatesOnDateFilteredSubset()
    {
        // Arrange: three lines — only the middle one is in the date range
        var line1 = "[2026-02-12 10:00:00.000 +01:00][][INF][Source] keyword early";
        var line2 = "[2026-02-12 13:30:00.000 +01:00][][INF][Source] keyword in-range";
        var line3 = "[2026-02-12 20:00:00.000 +01:00][][INF][Source] keyword late";
        var allText = string.Join("\n", line1, line2, line3);

        var start = new DateTime(2026, 2, 12, 13, 0, 0);
        var end = new DateTime(2026, 2, 12, 14, 0, 0);

        // Act: date-filter first (simulates what ApplyFilterAsync stores in _dateFilteredText)
        var dateFiltered = MainViewModel.FilterLogText(allText, null, start, end);

        // Assert: only the in-range line survives the date filter
        dateFiltered.Should().Contain("in-range");
        dateFiltered.Should().NotContain("early");
        dateFiltered.Should().NotContain("late");

        // Act: search runs on the date-filtered result (simulates ApplySearchFilter using _dateFilteredText)
        var searchFiltered = dateFiltered.Split('\n')
            .Where(l => l.Contains("keyword", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Assert: search finds only the in-range line, not the out-of-range "keyword early/late" lines
        searchFiltered.Should().HaveCount(1);
        searchFiltered[0].Should().Contain("in-range");
    }
}
