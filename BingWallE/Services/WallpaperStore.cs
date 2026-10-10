using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BingWallE.Models;

namespace BingWallE.Services;

public sealed class WallpaperStore(HttpClient httpClient) : IWallpaperStore
{
    public bool Exists(string folder, Wallpaper image) => File.Exists(GetPath(folder, image));

    private static string GetPath(string folder, Wallpaper image)
    {
        if (string.IsNullOrWhiteSpace(folder))
            throw new IOException("Bitte einen gültigen Zielordner auswählen.");
        return Path.Combine(folder, WallpaperVariants.FileName(image));
    }

    public async Task DownloadAsync(string folder, Wallpaper image, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = GetPath(folder, image);
        Directory.CreateDirectory(folder);
        var temporaryPath = Path.Combine(folder, $".bingwalle-{Guid.NewGuid():N}.tmp");
        try
        {
            var data = await httpClient.GetByteArrayAsync(image.Url, cancellationToken);
            await File.WriteAllBytesAsync(temporaryPath, data, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            try { File.Delete(temporaryPath); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Trace.WriteLine($"Temporäre Datei konnte nicht entfernt werden: {ex.Message}");
            }
        }
    }
}
