using FluentAssertions;
using LogViewer2026.Core.Services;

namespace LogViewer2026.Core.Tests.Services;

public class LogServiceTests : IDisposable
{
    private readonly string _testFilePath;

    public LogServiceTests()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"logservice_test_{Guid.NewGuid()}.log");
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
            File.Delete(_testFilePath);
    }

    [Fact]
    public async Task LoadFileAsync_ShouldReturnFileContent()
    {
        var content = "line1\nline2\nline3";
        File.WriteAllText(_testFilePath, content);
        using var service = new LogService();

        var result = await service.LoadFileAsync(_testFilePath);

        result.Should().Be(content);
    }

    [Fact]
    public async Task LoadFileAsync_ShouldReportProgress()
    {
        File.WriteAllText(_testFilePath, "line1\nline2\nline3\n");
        using var service = new LogService();
        int? reportedCount = null;
        var progress = new Progress<int>(count => reportedCount = count);

        await service.LoadFileAsync(_testFilePath, progress);

        // Allow progress callback to complete
        await Task.Delay(100);
        reportedCount.Should().NotBeNull();
        reportedCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task LoadFileAsync_WithEmptyFile_ShouldReturnEmptyString()
    {
        File.WriteAllText(_testFilePath, string.Empty);
        using var service = new LogService();

        var result = await service.LoadFileAsync(_testFilePath);

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetTotalLineCount_ShouldReturnCorrectCount()
    {
        using var service = new LogService();

        var count = service.GetTotalLineCount("line1\nline2\nline3");

        count.Should().Be(3);
    }

    [Fact]
    public void GetTotalLineCount_WithSingleLine_ShouldReturnOne()
    {
        using var service = new LogService();

        var count = service.GetTotalLineCount("single line");

        count.Should().Be(1);
    }

    [Fact]
    public void GetTotalLineCount_WithEmptyString_ShouldReturnZero()
    {
        using var service = new LogService();

        var count = service.GetTotalLineCount(string.Empty);

        count.Should().Be(0);
    }

    [Fact]
    public void GetTotalLineCount_WithNull_ShouldReturnZero()
    {
        using var service = new LogService();

        var count = service.GetTotalLineCount(null!);

        count.Should().Be(0);
    }

    [Fact]
    public async Task LoadFileAsync_WithLargeFile_ShouldLoadSuccessfully()
    {
        var lines = Enumerable.Range(1, 5000).Select(i => $"2024-01-01 10:00:{i:D2}.000 [INF] Log entry {i}");
        File.WriteAllText(_testFilePath, string.Join("\n", lines));
        using var service = new LogService();

        var result = await service.LoadFileAsync(_testFilePath);

        result.Should().NotBeNullOrEmpty();
        service.GetTotalLineCount(result).Should().Be(5000);
    }
}
