using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using BingWallE.Models;
using ExifLibrary;
using SkiaSharp;

namespace BingWallE.Services;

public sealed record LocalWallpaper(string Path, Wallpaper Image, byte[] Bytes, int Width, int Height, KeywordMetadataState Keywords);

public sealed class LocalCollectionReader
{
    public async IAsyncEnumerable<LocalWallpaper> ReadAsync(string folder, Action skipped,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(folder)) throw new IOException("Bitte einen gültigen Zielordner auswählen.");
        if (!Directory.Exists(folder)) yield break;
        var paths = await Task.Run(() => Directory.EnumerateFiles(folder)
            .Where(p => Path.GetExtension(p).ToLowerInvariant() is ".jpg" or ".jpeg" or ".png" or ".webp")
            .OrderByDescending(File.GetLastWriteTimeUtc).ThenBy(p => p, StringComparer.Ordinal).ToArray(), cancellationToken);
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LocalWallpaper? result = null;
            try { result = await Task.Run(() => Read(path), cancellationToken); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { Trace.WriteLine($"Skipping local image {path}: {ex}"); skipped(); }
            if (result != null) yield return result;
        }
    }

    private static LocalWallpaper Read(string path)
    {
        var bytes = File.ReadAllBytes(path);
        using var codec = SKCodec.Create(new MemoryStream(bytes)) ?? throw new InvalidDataException("Invalid local image.");
        string? title = null, description = null, copyright = null, comments = null;
        try
        {
            var exif = ImageFile.FromBuffer(bytes);
            string? Get(ExifTag tag) => exif.Properties.Get(tag)?.Value?.ToString()?.TrimEnd('\0');
            title = Get(ExifTag.WindowsTitle) ?? Get(ExifTag.ImageDescription);
            description = Get(ExifTag.WindowsSubject);
            comments = Get(ExifTag.WindowsComment) ?? Get(ExifTag.UserComment);
            copyright = Get(ExifTag.Copyright);
        }
        catch (Exception ex) { Trace.WriteLine($"EXIF unavailable for {path}: {ex.Message}"); }
        XDocument? xmp = null;
        try { xmp = KeywordMetadata.ReadXmp(bytes); }
        catch (Exception ex) { Trace.WriteLine($"XMP unavailable for {path}: {ex.Message}"); }
        XNamespace dc = "http://purl.org/dc/elements/1.1/";
        XNamespace bw = "https://github.com/gruetze-software/BingWallE/metadata/1.0/";
        string? Value(XName name) => xmp?.Descendants(name).FirstOrDefault()?.Value;
        title = Value(dc + "title") ?? title;
        description = Value(dc + "description") ?? description ?? comments;
        var sourceText = Value(bw + "Source") ?? comments ?? "";
        var source = sourceText.Contains("Windows Spotlight", StringComparison.OrdinalIgnoreCase)
            ? WallpaperSource.WindowsSpotlight : sourceText.Equals("Bing", StringComparison.OrdinalIgnoreCase)
              || sourceText.Contains("Source: Bing", StringComparison.OrdinalIgnoreCase) ? WallpaperSource.Bing : WallpaperSource.Local;
        var sourceLine = description?.LastIndexOf("\nSource: ", StringComparison.Ordinal) ?? -1;
        if (sourceLine >= 0) description = description![..sourceLine];
        var image = new Wallpaper(Value(bw + "SourceUrl") ?? new Uri(Path.GetFullPath(path)).AbsoluteUri,
            string.IsNullOrWhiteSpace(title) ? Path.GetFileNameWithoutExtension(path) : title,
            Value(bw + "PublicationDate") ?? "", Copyright: Value(dc + "rights") ?? copyright ?? "",
            Market: Value(bw + "Market") ?? "", Source: source, SourceId: Value(bw + "SourceId"), Description: description);
        return new(Path.GetFullPath(path), image, bytes, codec.Info.Width, codec.Info.Height, KeywordMetadata.Read(bytes));
    }

}
