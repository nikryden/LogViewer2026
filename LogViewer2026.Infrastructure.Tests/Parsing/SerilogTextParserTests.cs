using FluentAssertions;
using LogViewer2026.Core.Models;
using LogViewer2026.Infrastructure.Parsing;

namespace LogViewer2026.Infrastructure.Tests.Parsing;

public class SerilogTextParserTests
{
    [Fact]
    public void CanParse_WithSerilogTextFormat_ShouldReturnTrue()
    {
        var parser = new SerilogTextParser();
        var text = "2024-01-01 10:00:00.123 [INF] Test message";

        var result = parser.CanParse(text);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanParse_WithJsonFormat_ShouldReturnFalse()
    {
        var parser = new SerilogTextParser();
        var json = """{"Timestamp":"2024-01-01T10:00:00"}""";

        var result = parser.CanParse(json);

        result.Should().BeFalse();
    }

    [Fact]
    public void Parse_WithStandardFormat_ShouldReturnLogEntry()
    {
        var parser = new SerilogTextParser();
        var text = "2024-01-01 10:00:00.123 [INF] User logged in";

        var entry = parser.Parse(text, 0, 1);

        entry.Should().NotBeNull();
        entry!.Level.Should().Be(LogLevel.Information);
        entry.Message.Should().Be("User logged in");
    }

    [Theory]
    [InlineData("[VRB]", LogLevel.Verbose)]
    [InlineData("[DBG]", LogLevel.Debug)]
    [InlineData("[INF]", LogLevel.Information)]
    [InlineData("[WRN]", LogLevel.Warning)]
    [InlineData("[ERR]", LogLevel.Error)]
    [InlineData("[FTL]", LogLevel.Fatal)]
    public void Parse_WithDifferentLogLevels_ShouldParseCorrectly(string levelCode, LogLevel expectedLevel)
    {
        var parser = new SerilogTextParser();
        var text = $"2024-01-01 10:00:00.123 {levelCode} Test message";

        var entry = parser.Parse(text, 0, 1);

        entry.Should().NotBeNull();
        entry!.Level.Should().Be(expectedLevel);
    }

    [Fact]
    public void Parse_WithSourceContext_ShouldIncludeSourceContext()
    {
        var parser = new SerilogTextParser();
        var text = "2024-01-01 10:00:00.123 [INF] MyNamespace.MyClass Test message";

        var entry = parser.Parse(text, 0, 1);

        entry.Should().NotBeNull();
        entry!.SourceContext.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Parse_WithMalformedText_ShouldReturnNull()
    {
        var parser = new SerilogTextParser();
        var text = "This is not a log entry";

        var entry = parser.Parse(text, 0, 1);

        entry.Should().BeNull();
    }

    [Fact]
    public void Parse_ShouldIncludeOffsetAndLineNumber()
    {
        var parser = new SerilogTextParser();
        var text = "2024-01-01 10:00:00.123 [INF] Test message";

        var entry = parser.Parse(text, 12345, 42);

        entry.Should().NotBeNull();
        entry!.FileOffset.Should().Be(12345);
        entry.LineNumber.Should().Be(42);
    }
}
