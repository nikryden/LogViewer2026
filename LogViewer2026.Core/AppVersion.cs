using System.Reflection;

namespace LogViewer2026.Core;

public static class AppVersion
{
    public static string Current { get; } =
        typeof(AppVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
        ?? "0.0.0";
}
