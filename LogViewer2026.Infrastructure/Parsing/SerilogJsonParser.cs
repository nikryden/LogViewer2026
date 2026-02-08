using System.Text.Json;
using LogViewer2026.Core.Interfaces;
using LogViewer2026.Core.Models;

namespace LogViewer2026.Infrastructure.Parsing;

public sealed class SerilogJsonParser : ILogParser
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public bool CanParse(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return false;

        var trimmed = line.TrimStart();
        return trimmed.StartsWith('{') && trimmed.Contains("Timestamp") && trimmed.Contains("Level");
    }

    public LogEntry? Parse(string line, long offset, int lineNumber)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;

            if (!root.TryGetProperty("Timestamp", out var timestampElement) ||
                !root.TryGetProperty("Level", out var levelElement))
            {
                return null;
            }

            var timestamp = timestampElement.GetDateTime();
            var level = ParseLogLevel(levelElement.GetString() ?? "Information");

            var message = root.TryGetProperty("RenderedMessage", out var renderedMsg)
                ? renderedMsg.GetString() ?? string.Empty
                : root.TryGetProperty("MessageTemplate", out var msgTemplate)
                    ? msgTemplate.GetString() ?? string.Empty
                    : string.Empty;

            var exception = root.TryGetProperty("Exception", out var exElement)
                ? exElement.GetString()
                : null;

            var sourceContext = root.TryGetProperty("SourceContext", out var srcElement)
                ? srcElement.GetString()
                : null;

            Dictionary<string, object>? properties = null;
            if (root.TryGetProperty("Properties", out var propsElement))
            {
                properties = [];
                foreach (var prop in propsElement.EnumerateObject())
                {
                    properties[prop.Name] = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                        JsonValueKind.Number => prop.Value.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => prop.Value.ToString()
                    };
                }
            }

            return new LogEntry
            {
                Timestamp = timestamp,
                Level = level,
                Message = message,
                Exception = exception,
                Properties = properties,
                FileOffset = offset,
                LineNumber = lineNumber,
                SourceContext = sourceContext
            };
        }
        catch
        {
            return null;
        }
    }

    private static LogLevel ParseLogLevel(string level) => level.ToUpperInvariant() switch
    {
        "VERBOSE" or "VRB" => LogLevel.Verbose,
        "DEBUG" or "DBG" => LogLevel.Debug,
        "INFORMATION" or "INFO" or "INF" => LogLevel.Information,
        "WARNING" or "WARN" or "WRN" => LogLevel.Warning,
        "ERROR" or "ERR" => LogLevel.Error,
        "FATAL" or "FTL" => LogLevel.Fatal,
        _ => LogLevel.Information
    };
}
