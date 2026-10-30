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
    public static byte[] Write(byte[] jpeg, Wallpaper image, IReadOnlyList<ImageTag> tags, DateTimeOffset? downloadedAt = null, KeywordMetadataState? keywords = null)
    {
        var file = ImageFile.FromBuffer(jpeg);
        if (file.Format != ImageFileFormat.JPEG) throw new InvalidDataException("Bing did not return a JPEG image.");
        var credits = WallpaperCredits.Split(image.Copyright);
        var description = !string.IsNullOrWhiteSpace(image.Description) ? image.Description
            : string.IsNullOrWhiteSpace(credits.Description) ? image.Title : credits.Description;
        var comments = description + "\nSource: " + image.SourceName;
        file.Properties.Set(ExifTag.WindowsTitle, image.Title, Encoding.Unicode);
        file.Properties.Set(ExifTag.WindowsSubject, description, Encoding.Unicode);
        file.Properties.Set(ExifTag.WindowsComment, comments, Encoding.Unicode);
        file.Properties.Set(ExifTag.WindowsKeywords, string.Join(";", WallpaperKeywords.Stored(image, tags)), Encoding.Unicode);
        file.Properties.Set(new Utf8ExifString(ExifTag.ImageDescription, image.Title));
        file.Properties.Set(ExifTag.UserComment, comments, Encoding.Unicode);
        file.Properties.Set(new Utf8ExifString(ExifTag.Software, AppInfo.DisplayName));
        if (!string.IsNullOrWhiteSpace(credits.Copyright)) file.Properties.Set(new Utf8ExifString(ExifTag.Copyright, credits.Copyright));
        // Missing photo dates use the download time; existing dates and photographer/camera fields are retained.
        var timestamp = downloadedAt ?? DateTimeOffset.Now;
        var createdAt = ReadDate(file, ExifTag.DateTimeDigitized);
        var originalAt = ReadDate(file, ExifTag.DateTimeOriginal);
        if (createdAt == null) file.Properties.Set(ExifTag.DateTimeDigitized, timestamp.DateTime);
        if (originalAt == null) file.Properties.Set(ExifTag.DateTimeOriginal, timestamp.DateTime);
        using var output = new MemoryStream();
        file.Save(output);
        var result = JpegXmpMetadata.Write(output.ToArray(), image, comments, tags, timestamp,
            createdAt, createdAt == null, originalAt == null);
        return keywords == null ? result : KeywordMetadata.Update(result, keywords);
    }
    private static DateTime? ReadDate(ImageFile file, ExifTag tag) =>
        file.Properties.Get(tag)?.Value is DateTime date && date > DateTime.MinValue ? date : null;

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
public record WallpaperDownloadResult(int Width, int Height, IReadOnlyList<ImageTag> Tags, KeywordMetadataState? Keywords = null);
