using FluentAssertions;
using LogViewer2026.Core.Models;
using LogViewer2026.Infrastructure.Parsing;

namespace LogViewer2026.Infrastructure.Tests.Parsing;

public class SerilogJsonParserTests
{
    [Fact]
    public void CanParse_WithValidJson_ShouldReturnTrue()
    {
        var parser = new SerilogJsonParser();
        var json = """{"Timestamp":"2024-01-01T10:00:00","Level":"Information","MessageTemplate":"Test"}""";

        var result = parser.CanParse(json);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanParse_WithInvalidJson_ShouldReturnFalse()
    {
        var parser = new SerilogJsonParser();
        var text = "2024-01-01 10:00:00 [INF] Test message";

        var result = parser.CanParse(text);

        result.Should().BeFalse();
    }

    [Fact]
    public void Parse_WithValidJson_ShouldReturnLogEntry()
    {
        var parser = new SerilogJsonParser();
        var json = """{"Timestamp":"2024-01-01T10:00:00.123Z","Level":"Information","MessageTemplate":"User logged in","RenderedMessage":"User logged in"}""";

        var entry = parser.Parse(json, 0, 1);

        entry.Should().NotBeNull();
        entry!.Timestamp.Should().Be(new DateTime(2024, 1, 1, 10, 0, 0, 123, DateTimeKind.Utc));
        entry.Level.Should().Be(LogLevel.Information);
        entry.Message.Should().Be("User logged in");
    }

    [Fact]
    public void Parse_WithException_ShouldIncludeExceptionDetails()
    {
        var parser = new SerilogJsonParser();
        var json = """{"Timestamp":"2024-01-01T10:00:00Z","Level":"Error","MessageTemplate":"Error occurred","Exception":"System.Exception: Test error"}""";

        var entry = parser.Parse(json, 0, 1);

        entry.Should().NotBeNull();
        entry!.Exception.Should().Be("System.Exception: Test error");
    }

    [Fact]
    public void Parse_WithProperties_ShouldIncludeProperties()
    {
        var parser = new SerilogJsonParser();
        var json = """{"Timestamp":"2024-01-01T10:00:00Z","Level":"Information","MessageTemplate":"Test","Properties":{"UserId":123,"Action":"Login"}}""";

        var entry = parser.Parse(json, 0, 1);

        entry.Should().NotBeNull();
        entry!.Properties.Should().NotBeNull();
        entry.Properties.Should().ContainKey("UserId");
    }

    [Theory]
    [InlineData("Verbose", LogLevel.Verbose)]
    [InlineData("Debug", LogLevel.Debug)]
    [InlineData("Information", LogLevel.Information)]
    [InlineData("Warning", LogLevel.Warning)]
    [InlineData("Error", LogLevel.Error)]
    [InlineData("Fatal", LogLevel.Fatal)]
    public void Parse_WithDifferentLogLevels_ShouldParseCorrectly(string levelString, LogLevel expectedLevel)
    {
        var parser = new SerilogJsonParser();
        var json = $$"""{"Timestamp":"2024-01-01T10:00:00Z","Level":"{{levelString}}","MessageTemplate":"Test"}""";

        var entry = parser.Parse(json, 0, 1);

        entry.Should().NotBeNull();
        entry!.Level.Should().Be(expectedLevel);
    }

    [Fact]
    public void Parse_WithMalformedJson_ShouldReturnNull()
    {
        var parser = new SerilogJsonParser();
        var json = """{"Timestamp":"2024-01-01T10:00:00","Level":"Info""";

        var entry = parser.Parse(json, 0, 1);

        entry.Should().BeNull();
    }
}
