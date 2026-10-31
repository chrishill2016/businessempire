using System.Reflection;

namespace BlackMarketEmpire.Services;

public static class AppInfo
{
    public const string Name = "Black Market Empire";

    /// <summary>The project's &lt;Version&gt;, stamped with the build number by the pipeline.</summary>
    public static string Version { get; } =
        (typeof(AppInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? "0.0.0").Split('+')[0];
}
