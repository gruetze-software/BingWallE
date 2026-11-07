using System;
using BingWallE.Localization;
using BingWallE.Models;

namespace BingWallE.Services;

public static class KeywordPromptBuilder
{
    public static string Build(Wallpaper image, string description, UiText text)
    {
        var language = string.IsNullOrWhiteSpace(image.Market) ? text.Language : image.Market;
        var keywordLanguage = text.Get(language.StartsWith("de", StringComparison.OrdinalIgnoreCase) ? "Deutsch" : "Englisch");
        var hasUrl = Uri.TryCreate(image.Url, UriKind.Absolute, out var url)
            && url.Scheme is "https" or "http" && !url.IsLoopback;
        var imageInstruction = hasUrl
            ? text.Get("Analysiere das Bild unter dieser URL: {0}", url!.AbsoluteUri)
            : text.Get("Bitte hänge das Bild an diesen Chat an und analysiere das angehängte Bild.");
        return imageInstruction + "\n\n"
            + text.Get("Nutze Titel und Beschreibung nur als ergänzenden Kontext. Nenne nur sicher erkennbare Motive; rate keine Orte, Arten oder Objekte. Wenn du die URL nicht öffnen kannst, bitte um das Bild als Anhang, statt anhand des Textes zu raten.") + "\n\n"
            + text.Get("Erstelle höchstens zehn kurze Keywords auf {0}, durch Semikolon getrennt. Antworte ausschließlich mit der Keyword-Liste, ohne Aufzählung oder Erklärung.", keywordLanguage) + "\n\n"
            + text.Get("Titel: {0}", image.Title) + "\n" + text.Get("Beschreibung: {0}", description);
    }
}
