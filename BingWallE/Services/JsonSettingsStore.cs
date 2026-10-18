using System;
using System.IO;
using System.Text.Json;
using BingWallE.Models;

namespace BingWallE.Services;

public sealed class JsonSettingsStore(string filePath) : ISettingsStore
{
    public static string DefaultFilePath => ApplicationPaths.SettingsFile;

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(filePath)) return new AppSettings();
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(filePath));
            return settings == null || string.IsNullOrWhiteSpace(settings.WallpaperPath)
                ? new AppSettings() : settings;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var fullPath = Path.GetFullPath(filePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temporaryPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings));
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            try { File.Delete(temporaryPath); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }
        }
    }
}
