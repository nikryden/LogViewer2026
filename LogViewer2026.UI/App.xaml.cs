using LogViewer2026.Core.Interfaces;
using LogViewer2026.Core.Models;
using LogViewer2026.Core.Services;
using LogViewer2026.Infrastructure.FileReading;
using LogViewer2026.Infrastructure.Parsing;
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
}


