using FluentAssertions;
using LogViewer2026.Core.Models;
using LogViewer2026.Core.Services;

namespace LogViewer2026.Core.Tests.Services;

public class InvertedIndexTests
{
    private static LogEntry CreateEntry(string message, int lineNumber) => new()
    {
        Timestamp = DateTime.UtcNow,
        Level = LogLevel.Information,
        Message = message,
        LineNumber = lineNumber
    };

    [Fact]
    public void AddEntry_AndSearch_ShouldFindMatchingLine()
    {
        var index = new InvertedIndex();
        index.AddEntry(CreateEntry("User logged in successfully", 1));
        index.AddEntry(CreateEntry("Database connection established", 2));

        var results = index.Search("logged").ToList();

        results.Should().Contain(1);
        results.Should().NotContain(2);
    }

    [Fact]
    public void Search_WithMultipleMatches_ShouldReturnAllLines()
    {
        var index = new InvertedIndex();
        index.AddEntry(CreateEntry("Error in service A", 1));
        index.AddEntry(CreateEntry("Success in service B", 2));
        index.AddEntry(CreateEntry("Error in service C", 3));

        var results = index.Search("error").ToList();

        results.Should().HaveCount(2);
        results.Should().Contain(1);
        results.Should().Contain(3);
    }

    [Fact]
    public void Search_WithNoMatch_ShouldReturnEmpty()
    {
        var index = new InvertedIndex();
        index.AddEntry(CreateEntry("Hello world", 1));

        var results = index.Search("nonexistent").ToList();

        results.Should().BeEmpty();
    }

    [Fact]
    public void Search_WithMultipleWords_ShouldIntersectResults()
    {
        var index = new InvertedIndex();
        index.AddEntry(CreateEntry("User logged in from API", 1));
        index.AddEntry(CreateEntry("User logged out from web", 2));
        index.AddEntry(CreateEntry("Admin access from API", 3));

        var results = index.Search("user logged").ToList();

        results.Should().Contain(1);
        results.Should().Contain(2);
        results.Should().NotContain(3);
    }

    [Fact]
    public void Search_ShouldBeCaseInsensitive()
    {
        var index = new InvertedIndex();
        index.AddEntry(CreateEntry("ERROR occurred in handler", 1));

        var results = index.Search("error").ToList();

        results.Should().Contain(1);
    }

    [Fact]
    public void Search_WithEmptyTerm_ShouldReturnEmpty()
    {
        var index = new InvertedIndex();
        index.AddEntry(CreateEntry("Some message", 1));

        var results = index.Search(string.Empty).ToList();

        results.Should().BeEmpty();
    }

    [Fact]
    public void Search_WithWhitespaceTerm_ShouldReturnEmpty()
    {
        var index = new InvertedIndex();
        index.AddEntry(CreateEntry("Some message", 1));

        var results = index.Search("   ").ToList();

        results.Should().BeEmpty();
    }

    [Fact]
    public void Clear_ShouldRemoveAllEntries()
    {
        var index = new InvertedIndex();
        index.AddEntry(CreateEntry("Test message", 1));
        index.AddEntry(CreateEntry("Another message", 2));

        index.Clear();

        index.Search("test").ToList().Should().BeEmpty();
        index.Search("another").ToList().Should().BeEmpty();
    }

    [Fact]
    public void AddEntry_WithNullMessage_ShouldNotThrow()
    {
        var index = new InvertedIndex();
        var entry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = LogLevel.Information,
            Message = null!,
            LineNumber = 1
        };

        var act = () => index.AddEntry(entry);

        act.Should().NotThrow();
    }

    [Fact]
    public void Search_ShortWords_ShouldBeFilteredOut()
    {
        var index = new InvertedIndex();
        index.AddEntry(CreateEntry("An OK test of IT", 1));

        // Words with 2 or fewer characters are filtered out by tokenizer
        var results = index.Search("an").ToList();

        results.Should().BeEmpty();
    }
}
