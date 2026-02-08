using FluentAssertions;
using LogViewer2026.Infrastructure.FileReading;

namespace LogViewer2026.Infrastructure.Tests.FileReading;

public class MemoryMappedFileReaderTests : IDisposable
{
    private readonly string _testFilePath;

    public MemoryMappedFileReaderTests()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.log");
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
            File.Delete(_testFilePath);
    }

    [Fact]
    public void Open_WithValidFile_ShouldBuildLineIndex()
    {
        File.WriteAllText(_testFilePath, "line1\nline2\nline3\n");
        using var reader = new MemoryMappedFileReader();

        reader.Open(_testFilePath);

        reader.GetTotalLines().Should().Be(3);
    }

    [Fact]
    public void ReadLine_ShouldReturnCorrectLine()
    {
        File.WriteAllText(_testFilePath, "first line\nsecond line\nthird line\n");
        using var reader = new MemoryMappedFileReader();
        reader.Open(_testFilePath);

        var offset = reader.GetLineOffset(1);
        var line = reader.ReadLine(offset);

        line.Should().Be("second line");
    }

    [Fact]
    public void ReadLines_ShouldReturnMultipleLines()
    {
        File.WriteAllText(_testFilePath, "line1\nline2\nline3\nline4\n");
        using var reader = new MemoryMappedFileReader();
        reader.Open(_testFilePath);

        var lines = reader.ReadLines(reader.GetLineOffset(1), 2).ToList();

        lines.Should().HaveCount(2);
        lines[0].line.Should().Be("line2");
        lines[1].line.Should().Be("line3");
    }

    [Fact]
    public void Open_WithLargeFile_ShouldIndexEfficiently()
    {
        var lines = Enumerable.Range(1, 10000).Select(i => $"Log entry {i}");
        File.WriteAllLines(_testFilePath, lines);
        using var reader = new MemoryMappedFileReader();

        var start = DateTime.UtcNow;
        reader.Open(_testFilePath);
        var elapsed = DateTime.UtcNow - start;

        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
        reader.GetTotalLines().Should().Be(10000);
    }

    [Fact]
    public void ReadLine_WithWindowsLineEndings_ShouldHandleCorrectly()
    {
        File.WriteAllText(_testFilePath, "line1\r\nline2\r\nline3\r\n");
        using var reader = new MemoryMappedFileReader();
        reader.Open(_testFilePath);

        var line = reader.ReadLine(reader.GetLineOffset(1));

        line.Should().Be("line2");
    }

    [Fact]
    public void GetLineOffset_WithInvalidLineNumber_ShouldThrow()
    {
        File.WriteAllText(_testFilePath, "line1\nline2\n");
        using var reader = new MemoryMappedFileReader();
        reader.Open(_testFilePath);

        var act = () => reader.GetLineOffset(999);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
