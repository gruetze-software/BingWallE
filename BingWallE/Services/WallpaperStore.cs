using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BingWallE.Models;
using SkiaSharp;
using System.Collections.Generic;

namespace BingWallE.Services;

public sealed class WallpaperStore(HttpClient httpClient, IImageTagger? tagger = null) : IWallpaperStore
{
    public bool Exists(string folder, Wallpaper image) => File.Exists(GetPath(folder, image));

    public void Delete(string folder, Wallpaper image) => File.Delete(GetPath(folder, image));

    public string GetPath(string folder, Wallpaper image)
    {
        if (string.IsNullOrWhiteSpace(folder))
            throw new IOException("Bitte einen gültigen Zielordner auswählen.");
        var path = Path.Combine(folder, WallpaperVariants.FileName(image));
        if (File.Exists(path)) return path;
        var legacyPath = Path.Combine(folder, WallpaperVariants.LegacyFileName(image));
        return File.Exists(legacyPath) ? legacyPath : path;
    }

    public async Task<WallpaperDownloadResult> DownloadAsync(string folder, Wallpaper image, CancellationToken cancellationToken, bool generateTags = false, Action<string>? progress = null, KeywordMetadataState? keywords = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = GetPath(folder, image);
        Directory.CreateDirectory(folder);
        if (keywords == null && File.Exists(path))
        {
            var existing = KeywordMetadata.Read(await File.ReadAllBytesAsync(path, cancellationToken));
            if (existing.UserKeywords.Length > 0 || existing.ExcludedKeywords.Length > 0) keywords = existing;
        }
        var temporaryPath = Path.Combine(folder, $".bingwalle-{Guid.NewGuid():N}.tmp");
        try
        {
            var data = await httpClient.GetByteArrayAsync(image.Url, cancellationToken);
            var downloadedAt = DateTimeOffset.Now;
            using var codec = SKCodec.Create(new MemoryStream(data)) ?? throw new InvalidDataException("Bing did not return a valid image.");
            IReadOnlyList<ImageTag> tags = generateTags
                ? await (tagger ?? throw new InvalidOperationException("Image recognition is unavailable.")).RecognizeAsync(data, cancellationToken, progress)
                : Array.Empty<ImageTag>();
            progress?.Invoke("Bildinformationen werden gespeichert …");
            var tagged = await Task.Run(() => PhotoMetadataWriter.Write(data, image, tags, downloadedAt, keywords), cancellationToken);
            var savedKeywords = KeywordMetadata.Read(tagged);
            await File.WriteAllBytesAsync(temporaryPath, tagged, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, path, overwrite: true);
            return new WallpaperDownloadResult(codec.Info.Width, codec.Info.Height, tags, savedKeywords);
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
