using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Avalonia;

namespace BingWallE.Localization;

public sealed class UiText
{
    public sealed record Entry(string Key, string De, string En);
    public static IReadOnlyList<Entry> Entries { get; } = Load();
    private static readonly Dictionary<string, Entry> German = Entries.ToDictionary(x => x.De);
    private readonly Dictionary<string, (string Template, object[] Arguments)> _messages = new();
    private readonly Dictionary<string, string[]> _composites = new();
    public string Language { get; private set; } = "de";
    public CultureInfo Culture => CultureInfo.GetCultureInfo(Language == "en" ? "en-GB" : "de-DE");
    public void SetLanguage(string? language) => Language = language == "en" ? "en" : "de";
    public string Get(string text, params object[] arguments)
    {
        var template = German.TryGetValue(text, out var entry) && Language == "en" ? entry.En : text;
        var result = arguments.Length == 0 ? template : string.Format(Culture, template, arguments);
        if (arguments.Length > 0) _messages[result] = (text, arguments);
        return result;
    }
    public string Join(params string[] parts)
    {
        var result = string.Concat(parts);
        _composites[result] = parts;
        return result;
    }
    public string Translate(string text)
    {
        if (_composites.TryGetValue(text, out var parts)) return Join(parts.Select(Translate).ToArray());
        if (_messages.TryGetValue(text, out var message)) return Get(message.Template, message.Arguments.Select(x => x is string value ? Translate(value) : x).ToArray());
        if (German.ContainsKey(text)) return Get(text);
        var entry = Entries.FirstOrDefault(x => x.En == text);
        return entry == null ? text : Get(entry.De);
    }
    public string Progress(string text)
    {
        var match = Regex.Match(text, @"^Bing: Tag (\d+) von 8 – (Bilddaten|Vorschau) laden …$");
        return match.Success ? Get(match.Groups[2].Value == "Bilddaten"
            ? "Bing: Tag {0} von 8 – Bilddaten laden …" : "Bing: Tag {0} von 8 – Vorschau laden …", match.Groups[1].Value) : Translate(text);
    }
    public void ApplyResources()
    {
        if (Application.Current is not { } app) return;
        foreach (var entry in Entries) app.Resources[entry.Key] = Language == "en" ? entry.En : entry.De;
    }
    private static Entry[] Load()
    {
        using var stream = typeof(UiText).Assembly.GetManifestResourceStream("bingwalle.Localization.strings.json")
            ?? typeof(UiText).Assembly.GetManifestResourceStream("BingWallE.Localization.strings.json")
            ?? throw new InvalidOperationException("Missing translation catalog.");
        return JsonSerializer.Deserialize<Entry[]>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
}
