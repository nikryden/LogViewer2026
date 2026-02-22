using System.Text.Json;
using LogViewer2026.Core.Configuration;

namespace LogViewer2026.Core.Services;

public interface IFilterConfigurationService
{
    Task<FilterConfigurationCollection> LoadAsync(string filePath, CancellationToken cancellationToken = default);
    Task SaveAsync(FilterConfigurationCollection configuration, string filePath, CancellationToken cancellationToken = default);
    Task<FilterConfigurationCollection> GetDefaultAsync(CancellationToken cancellationToken = default);
}

public sealed class FilterConfigurationService : IFilterConfigurationService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public async Task<FilterConfigurationCollection> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            return new FilterConfigurationCollection();

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            return JsonSerializer.Deserialize<FilterConfigurationCollection>(json, _jsonOptions)
                   ?? new FilterConfigurationCollection();
        }
        catch
        {
            return new FilterConfigurationCollection();
        }
    }

    public async Task SaveAsync(FilterConfigurationCollection configuration, string filePath, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(configuration, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    public Task<FilterConfigurationCollection> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new FilterConfigurationCollection
        {
            Filters =
            [
                new FilterConfiguration { Name = "Errors Only", LogLevel = Models.LogLevel.Error },
                new FilterConfiguration { Name = "Warnings and Above", LogLevel = Models.LogLevel.Warning },
                new FilterConfiguration { Name = "Info and Above", LogLevel = Models.LogLevel.Information }
            ]
        });
    }
}
