using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BingWallE.Models;

public static class WallpaperFileName
{
    public static string Create(string? date, string? title) => CreateClean(date, title, false);

    // Spotlight already has a stable unique ID in its prefix; no second hash is needed.
    internal static string CreateWithIdentity(string prefix, string title) => CreateClean(prefix, title, true);

    private static string CreateClean(string? prefix, string? title, bool hasUniqueIdentity)
    {
        var original = $"{prefix}_{title}";
        var decomposed = original.Replace("Ä", "Ae").Replace("Ö", "Oe").Replace("Ü", "Ue")
            .Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ß", "ss")
            .Replace("Æ", "AE").Replace("æ", "ae").Replace("Œ", "OE").Replace("œ", "oe")
            .Replace("Ø", "O").Replace("ø", "o").Replace("Ł", "L").Replace("ł", "l")
            .Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9') builder.Append(c);
            else if (c is '\'' or '\u2019' or '\u02BB' or '\u02BC') continue;
            else if (builder.Length > 0 && builder[^1] != '_') builder.Append('_');
        }
        var name = builder.ToString().Trim('_');
        var changed = name != original.Replace(' ', '_');
        var shortened = name.Length > 140;
        if (shortened) name = name[..140].TrimEnd('_');
        if (name.Length == 0) { name = "Bild"; changed = true; }
        if ((changed || shortened) && !hasUniqueIdentity)
            name += "_" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(original)))[..12];
        return name + ".jpg";
    }

    // Used only to recognize files saved by earlier versions; never used for new downloads.
    internal static string CreateLegacy(string? date, string? title)
    {
        var original = $"{date}_{title}";
        var name = new string(original.Select(c => char.IsControl(c) || "<>:\"/\\|?*".Contains(c)
            ? '_' : c == ' ' ? '_' : c).ToArray());
        // Keep existing ordinary names; distinguish titles that need sanitizing or shortening.
        var needsSuffix = name != original.Replace(' ', '_') || name.Length > 140 || Encoding.UTF8.GetByteCount(name) > 180;
        if (name.Length > 140)
        {
            name = name[..140];
            if (char.IsHighSurrogate(name[^1])) name = name[..^1];
        }
        while (Encoding.UTF8.GetByteCount(name) > 180)
        {
            name = name[..^1];
            if (char.IsHighSurrogate(name[^1])) name = name[..^1];
        }
        if (needsSuffix)
            name += "_" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(original)))[..12];
        return name + ".jpg";
    }
}
