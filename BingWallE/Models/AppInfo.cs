namespace BingWallE.Models;

public static class AppInfo
{
    public const string Name = "BingWallE";
    public static string Version => typeof(AppInfo).Assembly.GetName().Version?.ToString(2) ?? "1.0";
    public static string DisplayName => $"{Name} {Version}";
    public const string RepositoryUrl = "https://github.com/gruetze-software/BingWallE";
}
