using System;
using System.IO;

namespace BingWallE.Services;

public static class ApplicationPaths
{
    private static bool IsSnap => OperatingSystem.IsLinux() && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SNAP"));
    private static string? SnapCommon => IsSnap ? Environment.GetEnvironmentVariable("SNAP_USER_COMMON") : null;

    public static string SettingsFile => InSnapCommon(SnapCommon, "config/bingwalle/settings.json",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "bingwalle", "settings.json"));
    public static string ModelsDirectory => InSnapCommon(SnapCommon, "models",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "bingwalle", "models"));

    public static string PicturesDirectory
    {
        get
        {
            var ordinary = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            return IsSnap ? SnapPictures(ordinary, Environment.GetEnvironmentVariable("SNAP_USER_DATA"),
                Environment.GetEnvironmentVariable("SNAP_REAL_HOME")) : ordinary;
        }
    }

    public static string InSnapCommon(string? common, string relative, string ordinary) =>
        !string.IsNullOrWhiteSpace(common) && Path.IsPathRooted(common) ? Path.Combine(common, relative) : ordinary;

    public static string SnapPictures(string ordinary, string? privateHome, string? realHome)
    {
        // Keep a host picture directory if the desktop integration already resolved one.
        if (!string.IsNullOrWhiteSpace(ordinary) && !string.IsNullOrWhiteSpace(privateHome)
            && !Path.GetFullPath(ordinary).StartsWith(Path.GetFullPath(privateHome).TrimEnd(Path.DirectorySeparatorChar)
                + Path.DirectorySeparatorChar, StringComparison.Ordinal)) return ordinary;
        return !string.IsNullOrWhiteSpace(realHome) && Path.IsPathRooted(realHome)
            ? Path.Combine(realHome, "Pictures") : ordinary;
    }
}
