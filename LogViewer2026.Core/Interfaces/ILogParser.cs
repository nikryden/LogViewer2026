using LogViewer2026.Core.Models;

namespace LogViewer2026.Core.Interfaces;

public interface ILogParser
{
    LogEntry? Parse(string line, long offset, int lineNumber);
    bool CanParse(string line);
}
