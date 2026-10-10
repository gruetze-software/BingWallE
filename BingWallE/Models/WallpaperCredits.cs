using System;

namespace BingWallE.Models;

public static class WallpaperCredits
{
    public static (string Description, string Copyright) Split(string text)
    {
        text = text.Trim();
        var start = text.LastIndexOf("(©", StringComparison.Ordinal);
        if (start >= 0 && text.EndsWith(')'))
            return (text[..start].TrimEnd(), text[(start + 1)..^1].Trim());
        // A standalone copyright notice is still valid; ordinary captions are not copyright claims.
        if (text.StartsWith('©')) return ("", text);
        return (text, "");
    }
}
