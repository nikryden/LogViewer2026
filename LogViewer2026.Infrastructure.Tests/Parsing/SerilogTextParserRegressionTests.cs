using FluentAssertions;
using LogViewer2026.Infrastructure.Parsing;
using Xunit;

namespace LogViewer2026.Infrastructure.Tests.Parsing;

public sealed class SerilogTextParserRegressionTests
{
    [Fact]
    public void Parse_WithTinyLogFormat_ShouldSucceed()
    {
        // Arrange - Exact format from tiny.log
        var parser = new SerilogTextParser();
        var line = "2026-01-31 11:37:52.660 [ERR] MyApp.Services.CacheService API request received from IP: 192.168.1.824";
        
        // Act
        var result = parser.Parse(line, 0, 0);
        
        // Assert
        result.Should().NotBeNull();
        result!.Level.Should().Be(Core.Models.LogLevel.Error);
        result.SourceContext.Should().Be("MyApp.Services.CacheService");
        result.Message.Should().Be("API request received from IP: 192.168.1.824");
    }

    [Fact]
    public void CanParse_WithTinyLogFormat_ShouldReturnTrue()
    {
        // Arrange
        var parser = new SerilogTextParser();
        var line = "2026-01-31 11:37:52.660 [ERR] MyApp.Services.CacheService API request received from IP: 192.168.1.824";
        
        // Act
        var result = parser.CanParse(line);
        
        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("2026-01-31 11:37:52.660 [INF] MyApp.Services.Initializer Exception caught: NullReferenceException")]
    [InlineData("2026-01-31 11:38:02.660 [INF] MyApp.Services.EmailService Timeout occurred while waiting for response")]
    [InlineData("2024-01-01 10:00:00.000 [INF] Test.ManualTest This is test entry 1")]
    [InlineData("2024-01-01 10:00:01.000 [ERR] Test.ManualTest This is test entry 2 with error")]
    [InlineData("2024-01-01 10:00:02.000 [WRN] Test.ManualTest This is test entry 3 with warning")]
    public void Parse_WithVariousFormats_ShouldSucceed(string line)
    {
        // Arrange
        var parser = new SerilogTextParser();
        
        // Act
        var result = parser.Parse(line, 0, 0);
        
        // Assert
        result.Should().NotBeNull($"line should parse: {line}");
        result!.Message.Should().NotBeNullOrEmpty();
    }
}
