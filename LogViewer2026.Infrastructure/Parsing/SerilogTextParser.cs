using System.Globalization;
using System.Text.RegularExpressions;
using LogViewer2026.Core.Interfaces;
using LogViewer2026.Core.Models;

namespace LogViewer2026.Infrastructure.Parsing;

public sealed partial class SerilogTextParser : ILogParser
{
    [GeneratedRegex(@"^(\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}(?:\.\d{3})?)\s+\[([A-Z]{3})\]\s+(.*)$", RegexOptions.Compiled)]
    private static partial Regex LogLineRegex();

    public bool CanParse(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            System.Diagnostics.Debug.WriteLine("SerilogTextParser.CanParse: Line is null or whitespace");
            return false;
        }

        var isMatch = LogLineRegex().IsMatch(line);
        System.Diagnostics.Debug.WriteLine($"SerilogTextParser.CanParse: Regex match = {isMatch} for line: '{line.Substring(0, Math.Min(50, line.Length))}'");
        return isMatch;
    }

    public LogEntry? Parse(string line, long offset, int lineNumber)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            System.Diagnostics.Debug.WriteLine($"Parse line {lineNumber}: Line is null or whitespace");
            return null;
        }

        try
        {
            var match = LogLineRegex().Match(line);
            if (!match.Success)
            {
                System.Diagnostics.Debug.WriteLine($"SerilogTextParser.Parse: Regex didn't match line {lineNumber}");
                System.Diagnostics.Debug.WriteLine($"  Line content: '{line}'");
                System.Diagnostics.Debug.WriteLine($"  Line length: {line.Length}");
                return null;
            }

            var timestampStr = match.Groups[1].Value;
            var levelCode = match.Groups[2].Value;
            var remainder = match.Groups[3].Value;

            System.Diagnostics.Debug.WriteLine($"Parse line {lineNumber}: SUCCESS - timestamp='{timestampStr}', level='{levelCode}'");

            if (!DateTime.TryParseExact(
                timestampStr,
                new[] { "yyyy-MM-dd HH:mm:ss.fff", "yyyy-MM-dd HH:mm:ss" },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var timestamp))
            {
                System.Diagnostics.Debug.WriteLine($"  WARNING: Timestamp parse failed for '{timestampStr}', using DateTime.Now");
                timestamp = DateTime.Now;
            }

            var level = ParseLogLevel(levelCode);

            string? sourceContext = null;
            var message = remainder.Trim();

            if (!string.IsNullOrEmpty(remainder))
            {
                var parts = remainder.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 1 && parts[0].Contains('.'))
                {
                    sourceContext = parts[0];
                    message = parts.Length > 1 ? parts[1] : string.Empty;
                }
            }

            var entry = new LogEntry
            {
                Timestamp = timestamp,
                Level = level,
                Message = message,
                FileOffset = offset,
                LineNumber = lineNumber,
                SourceContext = sourceContext
            };

            return entry;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SerilogTextParser.Parse EXCEPTION at line {lineNumber}: {ex.GetType().Name} - {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"  Stack: {ex.StackTrace}");
            return null;
        }
    }

    private static LogLevel ParseLogLevel(string levelCode) => levelCode.ToUpperInvariant() switch
    {
        "VRB" => LogLevel.Verbose,
        "DBG" => LogLevel.Debug,
        "INF" => LogLevel.Information,
        "WRN" => LogLevel.Warning,
        "ERR" => LogLevel.Error,
        "FTL" => LogLevel.Fatal,
        _ => LogLevel.Information
    };
}
