using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BingWallE.Models;

namespace BingWallE.Services;

public record WallpaperKeyword(string Label, bool FromModel);

public static class WallpaperKeywords
{
    // Match complete words, never photographer credits or fragments of unrelated words.
    private static readonly (string De, string En, string Words, string? Parent)[] Concepts =
    [
        ("Tier", "Animal", "tier|tiere|tieren|animal|animals|wildlife", null),
        ("Wolf", "Wolf", "wolf|wolves|wölfe|wölfen", "Tier|Animal"),
        ("Kojote", "Coyote", "coyote|coyotes|kojote|kojoten", "Tier|Animal"),
        ("Fuchs", "Fox", "fuchs|füchse|fox|foxes", "Tier|Animal"),
        ("Krake", "Octopus", "krake|kraken|octopus", "Tier|Animal"),
        ("Vogel", "Bird", "vogel|vögel|bird|birds|ohrenscharben|cormorant|cormorants", "Tier|Animal"),
        ("Fisch", "Fish", "fisch|fische|fish|anemone fish|puffer", "Tier|Animal"),
        ("Säugetier", "Mammal", "elefant|elefanten|elephant|elephants|lion|lions|löwe|löwen|tiger|tigers|whale|whales|wal|wale|dolphin|dolphins|delfin|delfine|wolf|wolves|wölfe|coyote|coyotes|kojote|kojoten|fox|foxes|fuchs|füchse|bear|bears|bär|bären", "Tier|Animal"),
        ("Meer", "Sea", "meer|meeres|ozean|ocean|sea|bay|bucht|seashore", null),
        ("Unterwasser", "Underwater", "unterwasser|underwater|scuba diver|coral reef|korallenriff", "Meer|Sea"),
        ("Küste", "Coast", "küste|küsten|coast|coastal|seashore", "Meer|Sea"),
        ("Strand", "Beach", "strand|beach|beaches", null),
        ("Insel", "Island", "insel|inseln|island|islands", null),
        ("Wald", "Forest", "wald|wälder|forest|woodland|rainforest|regenwald", "Natur|Nature"),
        ("Berge", "Mountains", "berg|berge|mountain|mountains|mountain peak|alpen|alps", "Natur|Nature"),
        ("See", "Lake", "see|lake|lakes|lakeside", "Natur|Nature"),
        ("Fluss", "River", "fluss|river|rivers", "Natur|Nature"),
        ("Wasserfall", "Waterfall", "wasserfall|waterfall|waterfalls", "Natur|Nature"),
        ("Blumen", "Flowers", "blume|blumen|flower|flowers|daisy", "Natur|Nature"),
        ("Architektur", "Architecture", "architektur|architecture|gebäude|building|castle|schloss|burg|palace|palast|bridge|brücke", null),
        ("Winter", "Winter", "winter|winterlich|wintry", null),
        ("Schnee", "Snow", "schnee|snow|snowy", "Winter|Winter"),
        ("Wüste", "Desert", "wüste|desert|sand dune", "Natur|Nature")
    ];

    public static IReadOnlyList<WallpaperKeyword> Build(Wallpaper image, IReadOnlyList<ImageTag> predictions)
    {
        var german = image.Market.StartsWith("de", StringComparison.OrdinalIgnoreCase);
        var result = new List<WallpaperKeyword>();
        void Add(string value, bool model)
        {
            if (!string.IsNullOrWhiteSpace(value) && !result.Any(x => x.Label.Equals(value, StringComparison.OrdinalIgnoreCase)))
                result.Add(new(value, model));
        }
        bool Match(string text, string aliases) => Regex.IsMatch(text, @"(?<![\p{L}\p{N}])(?:" + aliases + @")(?![\p{L}\p{N}])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        void Classify(string text, bool model)
        {
            foreach (var concept in Concepts)
                if (Match(text, concept.Words))
                {
                    Add(german ? concept.De : concept.En, model);
                    if (concept.Parent is {} parent) Add(parent.Split('|')[german ? 0 : 1], model);
                }
        }
        var caption = WallpaperCredits.Split(image.Copyright).Description;
        Classify(string.Join("\n", new[] { image.Title, image.Description, caption }.Where(t => !string.IsNullOrWhiteSpace(t))), false);
        // Bing captions conventionally list locations after the subject, separated by commas.
        foreach (var location in caption.Split(',').Skip(1).Select(x => x.Trim()))
            if (location.Length is > 1 and <= 60 && location.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 6)
                Add(location, false);
        foreach (var prediction in predictions.Where(x => float.IsFinite(x.Score) && x.Score >= .5f))
        {
            Classify(prediction.Label, true);
            if (!Concepts.Any(x => Match(prediction.Label, x.Words))) Add(prediction.Label, true);
        }
        return result.Take(24).ToArray();
    }

    public static IEnumerable<string> Stored(Wallpaper image, IReadOnlyList<ImageTag> predictions) =>
        Build(image, predictions).Select(x => x.Label).Concat(new[] { image.SourceName, "Wallpaper" }).Distinct(StringComparer.OrdinalIgnoreCase);
}
