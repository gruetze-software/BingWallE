using System;
using System.IO;
using System.Text.Json;

namespace BingWallE.Models;

public class AppSettings
{
    public string WallpaperPath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "BingImages");
    
    public static string FilePath
    {
        get 
        {
            // Erstellt den Pfad: /home/user/.config/bingwalle/ (Linux) 
            // oder AppData/Roaming/bingwalle/ (Windows)
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string configDir = Path.Combine(appData, "bingwalle");
                    
            // Ganz wichtig: Sicherstellen, dass der Ordner existiert!
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }
                    
            return Path.Combine(configDir, "settings.json");
        }
    }

    public static AppSettings Load()
    {
        if (!File.Exists(FilePath)) return new AppSettings();
        try { return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings(); }
        catch { return new AppSettings(); }
    }

    public void Save()
    {
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
    }
}