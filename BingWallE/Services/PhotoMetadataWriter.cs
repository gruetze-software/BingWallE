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
        var credits = WallpaperCredits.Split(image.Copyright);
        var description = string.IsNullOrWhiteSpace(credits.Description) ? image.Title : credits.Description;
        var comments = description;
        file.Properties.Set(ExifTag.WindowsTitle, image.Title, Encoding.Unicode);
        file.Properties.Set(ExifTag.WindowsSubject, description, Encoding.Unicode);
        file.Properties.Set(ExifTag.WindowsComment, comments, Encoding.Unicode);
        file.Properties.Set(ExifTag.WindowsKeywords, string.Join(";", WallpaperKeywords.Stored(image, tags)), Encoding.Unicode);
        file.Properties.Set(new Utf8ExifString(ExifTag.ImageDescription, image.Title));
        file.Properties.Set(ExifTag.UserComment, comments, Encoding.Unicode);
        file.Properties.Set(new Utf8ExifString(ExifTag.Software, AppInfo.DisplayName));
        if (!string.IsNullOrWhiteSpace(credits.Copyright)) file.Properties.Set(new Utf8ExifString(ExifTag.Copyright, credits.Copyright));
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
