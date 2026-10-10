using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BingWallE.Models;

public static class WallpaperFileName
{
    public static string Create(string? date, string? title)
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
