using System.Reflection;

namespace LogViewer2026.Core;

public static class AppVersion
{
    /// <summary>Full version including commit hash, e.g. "0.6.0-alpha1+abc1234".</summary>
    public static string Full { get; } =
        typeof(AppVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
        ?? "0.0.0";

    /// <summary>Short version without commit hash, e.g. "0.6.0-alpha1".</summary>
    public static string Current { get; } =
        Full.Contains('+') ? Full[..Full.IndexOf('+')] : Full;
}
