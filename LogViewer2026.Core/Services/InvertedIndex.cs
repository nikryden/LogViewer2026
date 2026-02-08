using LogViewer2026.Core.Models;
using System.Collections.Concurrent;

namespace LogViewer2026.Core.Services;

public sealed class InvertedIndex
{
    private readonly ConcurrentDictionary<string, HashSet<int>> _index = new();
    private readonly object _lock = new();

    public void AddEntry(LogEntry entry)
    {
        if (entry.Message == null) return;

        var words = TokenizeMessage(entry.Message);
        lock (_lock)
        {
            foreach (var word in words)
            {
                if (!_index.TryGetValue(word, out var lineNumbers))
                {
                    lineNumbers = [];
                    _index[word] = lineNumbers;
                }
                lineNumbers.Add(entry.LineNumber);
            }
        }
    }

    public IEnumerable<int> Search(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Enumerable.Empty<int>();

        var searchWords = TokenizeMessage(searchTerm);
        HashSet<int>? results = null;

        foreach (var word in searchWords)
        {
            if (_index.TryGetValue(word, out var lineNumbers))
            {
                if (results == null)
                {
                    results = new HashSet<int>(lineNumbers);
                }
                else
                {
                    results.IntersectWith(lineNumbers);
                }
            }
            else
            {
                return Enumerable.Empty<int>();
            }
        }

        return results ?? Enumerable.Empty<int>();
    }

    public void Clear()
    {
        lock (_lock)
        {
            _index.Clear();
        }
    }

    private static IEnumerable<string> TokenizeMessage(string message)
    {
        return message
            .Split([' ', '\t', '\n', '\r', '.', ',', ':', ';', '!', '?', '(', ')', '[', ']', '{', '}'], 
                   StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.ToLowerInvariant())
            .Where(w => w.Length > 2);
    }
}
