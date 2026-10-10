using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using BingWallE.Models;
using ExifLibrary;

namespace BingWallE.Services;

public static class PhotoMetadataWriter
{
    public static byte[] Write(byte[] jpeg, Wallpaper image, IReadOnlyList<ImageTag> tags)
    {
        var file = ImageFile.FromBuffer(jpeg);
        if (file.Format != ImageFileFormat.JPEG) throw new InvalidDataException("Bing did not return a JPEG image.");
        var date = DateTime.TryParseExact(image.Date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
            ? day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : image.Date;
        var description = string.IsNullOrWhiteSpace(image.Copyright) ? image.Title : image.Copyright;
        var comments = $"{description}\nBing publication date: {date}\nSource: {image.Url}";
        if (!string.IsNullOrWhiteSpace(image.CopyrightUrl)) comments += $"\nBing information: {image.CopyrightUrl}";
        if (tags.Count > 0) comments += "\nAI suggestions (local MobileNetV2; may be incorrect): " +
            string.Join(", ", tags.Select(x => $"{x.Label} ({x.Score.ToString("P0", CultureInfo.InvariantCulture)})"));
        file.Properties.Set(ExifTag.WindowsTitle, image.Title, Encoding.Unicode);
        file.Properties.Set(ExifTag.WindowsSubject, description, Encoding.Unicode);
        file.Properties.Set(ExifTag.WindowsComment, comments, Encoding.Unicode);
        file.Properties.Set(ExifTag.WindowsKeywords, string.Join(";", new[] { "Bing", "Wallpaper" }.Concat(tags.Select(x => x.Label))), Encoding.Unicode);
        file.Properties.Set(new Utf8ExifString(ExifTag.ImageDescription, image.Title));
        file.Properties.Set(ExifTag.UserComment, comments, Encoding.Unicode);
        file.Properties.Set(new Utf8ExifString(ExifTag.Software, AppInfo.DisplayName));
        if (!string.IsNullOrWhiteSpace(image.Copyright)) file.Properties.Set(new Utf8ExifString(ExifTag.Copyright, image.Copyright));
        // Bing's date is publication, not capture time. Preserve existing artist and camera fields.
        using var output = new MemoryStream();
        file.Save(output);
        return JpegXmpMetadata.Write(output.ToArray(), image, comments, tags);
    }
    // ExifLibNet counts characters for ASCII values; UTF-8 needs the actual byte count.
    private sealed class Utf8ExifString(ExifTag tag, string value) : ExifAscii(tag, value, Encoding.UTF8)
    {
        public override ExifInterOperability Interoperability
        {
            get
            {
                var bytes = Encoding.UTF8.GetBytes(Value + "\0");
                return new ExifInterOperability(ExifTagFactory.GetTagID(Tag), InterOpType.ASCII, (uint)bytes.Length, bytes);
            }
        }
    }

}

public record ImageTag(string Label, float Score);
public record WallpaperDownloadResult(int Width, int Height, IReadOnlyList<ImageTag> Tags);
