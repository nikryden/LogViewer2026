using FluentAssertions;
using LogViewer2026.Core.Models;
using LogViewer2026.Core.Services;
using LogViewer2026.Infrastructure.Parsing;

namespace LogViewer2026.Infrastructure.Tests.Integration;

public class EndToEndTests : IDisposable
{
    private readonly string _testFilePath;

    public EndToEndTests()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"integration_test_{Guid.NewGuid()}.log");
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
            File.Delete(_testFilePath);
    }

    [Fact]
    public async Task EndToEnd_JsonFormat_ShouldLoadAndParseSuccessfully()
    {
        var lines = new[]
        {
            """{"Timestamp":"2024-01-01T10:00:00.000Z","Level":"Information","MessageTemplate":"User logged in","RenderedMessage":"User logged in"}""",
            """{"Timestamp":"2024-01-01T10:00:01.000Z","Level":"Error","MessageTemplate":"Failed to connect","RenderedMessage":"Failed to connect","Exception":"System.Exception: timeout"}""",
            """{"Timestamp":"2024-01-01T10:00:02.000Z","Level":"Warning","MessageTemplate":"Cache miss","RenderedMessage":"Cache miss"}"""
        };
        File.WriteAllText(_testFilePath, string.Join("\n", lines));

        // Load file
        using var logService = new LogService();
        var text = await logService.LoadFileAsync(_testFilePath);

        text.Should().NotBeNullOrEmpty();
        logService.GetTotalLineCount(text).Should().Be(3);

        // Parse entries
        var parser = new SerilogJsonParser();
        var textLines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        parser.CanParse(textLines[0]).Should().BeTrue();

        var entry1 = parser.Parse(textLines[0], 0, 1);
        entry1.Should().NotBeNull();
        entry1!.Level.Should().Be(LogLevel.Information);
        entry1.Message.Should().Be("User logged in");

        var entry2 = parser.Parse(textLines[1], 0, 2);
        entry2.Should().NotBeNull();
        entry2!.Level.Should().Be(LogLevel.Error);
        entry2.Exception.Should().Contain("timeout");

        var entry3 = parser.Parse(textLines[2], 0, 3);
        entry3.Should().NotBeNull();
        entry3!.Level.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public async Task EndToEnd_TextFormat_ShouldLoadAndParseSuccessfully()
    {
        var lines = new[]
        {
            "2024-01-01 10:00:00.000 [INF] MyApp.Services.UserService User logged in",
            "2024-01-01 10:00:01.000 [ERR] MyApp.Services.UserService Failed to process request",
            "2024-01-01 10:00:02.000 [WRN] MyApp.Services.CacheService Cache miss for key: user_123"
        };
        File.WriteAllText(_testFilePath, string.Join("\n", lines));

        // Load file
        using var logService = new LogService();
        var text = await logService.LoadFileAsync(_testFilePath);

        text.Should().NotBeNullOrEmpty();
        logService.GetTotalLineCount(text).Should().Be(3);

        // Parse entries
        var parser = new SerilogTextParser();
        var textLines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        parser.CanParse(textLines[0]).Should().BeTrue();

        var entry1 = parser.Parse(textLines[0], 0, 1);
        entry1.Should().NotBeNull();
        entry1!.Level.Should().Be(LogLevel.Information);
        entry1.SourceContext.Should().Be("MyApp.Services.UserService");

        var entry2 = parser.Parse(textLines[1], 0, 2);
        entry2.Should().NotBeNull();
        entry2!.Level.Should().Be(LogLevel.Error);

        var entry3 = parser.Parse(textLines[2], 0, 3);
        entry3.Should().NotBeNull();
        entry3!.Level.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public async Task LargeFile_ShouldLoadAndCountEfficiently()
    {
        // Generate a large log file
        var logLines = Enumerable.Range(1, 10000)
            .Select(i => $"2024-01-01 10:{i / 3600:D2}:{i % 3600 / 60:D2}.{i % 1000:D3} [INF] MyApp.Service Log entry number {i}");
        File.WriteAllText(_testFilePath, string.Join("\n", logLines));

        using var logService = new LogService();

        var start = DateTime.UtcNow;
        var text = await logService.LoadFileAsync(_testFilePath);
        var elapsed = DateTime.UtcNow - start;

        text.Should().NotBeNullOrEmpty();
        logService.GetTotalLineCount(text).Should().Be(10000);
        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));

        // Verify first and last entries parse correctly
        var parser = new SerilogTextParser();
        var textLines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var firstEntry = parser.Parse(textLines[0], 0, 1);
        firstEntry.Should().NotBeNull();
        firstEntry!.Message.Should().Contain("Log entry number 1");

        var lastEntry = parser.Parse(textLines[^1], 0, 10000);
        lastEntry.Should().NotBeNull();
        lastEntry!.Message.Should().Contain("Log entry number 10000");
    }

    [Fact]
    public async Task MixedContent_ParserDetection_ShouldIdentifyFormat()
    {
        // Test that parsers correctly identify their format
        var jsonLine = """{"Timestamp":"2024-01-01T10:00:00Z","Level":"Information","MessageTemplate":"Test"}""";
        var textLine = "2024-01-01 10:00:00.000 [INF] Test message";

        var jsonParser = new SerilogJsonParser();
        var textParser = new SerilogTextParser();

        jsonParser.CanParse(jsonLine).Should().BeTrue();
        jsonParser.CanParse(textLine).Should().BeFalse();

        textParser.CanParse(textLine).Should().BeTrue();
        textParser.CanParse(jsonLine).Should().BeFalse();

        await Task.CompletedTask;
    }
}
