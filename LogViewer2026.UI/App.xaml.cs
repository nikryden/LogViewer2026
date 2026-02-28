using LogViewer2026.Core.Services;
using LogViewer2026.Infrastructure.Services;
using LogViewer2026.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Windows;
using WpfApplication = System.Windows.Application;

namespace LogViewer2026.UI;

public partial class App : WpfApplication
{
    private readonly IHost _host;
    public IServiceProvider? Services { get; private set; }

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Simple! Just the service
                services.AddSingleton<ILogService, LogService>();
                services.AddSingleton<IMultiFileLogService, MultiFileLogService>();
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IFilterConfigurationService, FilterConfigurationService>();

                services.AddTransient<MainViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<MainWindow>();
                services.AddTransient<SettingsWindow>();
            })
            .UseSerilog((context, configuration) =>
            {
                configuration
                    .WriteTo.File("logs/logviewer-.log", rollingInterval: RollingInterval.Day)
                    .MinimumLevel.Information();
            })
            .Build();

        Services = _host.Services;
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        await _host.StartAsync();

        // Load and apply theme
        var settingsService = _host.Services.GetRequiredService<ISettingsService>();
        var settings = await settingsService.LoadAsync();
        ApplyTheme(settings.Theme);

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        using (_host)
        {
            await _host.StopAsync();
        }
        base.OnExit(e);
    }

    public void ApplyTheme(string themeName)
    {
        var themeUri = themeName?.ToLower() == "dark"
            ? new Uri("pack://application:,,,/LogViewer2026.UI;component/Themes/DarkTheme.xaml")
            : new Uri("pack://application:,,,/LogViewer2026.UI;component/Themes/LightTheme.xaml");

        // Remove existing theme dictionary if present
        var existingTheme = Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString?.Contains("Theme.xaml") == true);

        if (existingTheme != null)
        {
            Resources.MergedDictionaries.Remove(existingTheme);
        }

        // Load and add new theme at the beginning (highest priority)
        var theme = new ResourceDictionary { Source = themeUri };
        Resources.MergedDictionaries.Insert(0, theme);
    }
}


