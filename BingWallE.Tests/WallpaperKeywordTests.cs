using BingWallE.Models;
using BingWallE.Services;
using BingWallE.ViewModels;
using Xunit;

namespace BingWallE.Tests;

public class WallpaperKeywordTests
{
    [Fact]
    public void SubjectsAndLocationsAreAddedWhileLegacySourceKeywordsAreRemovedOnExplicitSave()
    {
        var image = new ImageItemViewModel(Octopus);
        image.LoadKeywords(new(["Bing", "Wallpaper", "Mayotte", "Indischer Ozean", "My favourite"], [], [], ["My favourite"]));
        Assert.DoesNotContain(image.KeywordChips, x => x.Label is "Bing" or "Wallpaper");
        Assert.Contains(image.KeywordChips, x => x.Label == "Mayotte");
        Assert.Contains(image.ExportKeywords().Keywords, x => x == "My favourite");
        Assert.Contains("Krake", image.ExportKeywords().Keywords);
        Assert.DoesNotContain(WallpaperKeywords.Stored(Octopus, []), x => x is "Bing" or "Wallpaper");
    }
    private static Wallpaper Octopus => new("https://bing.com/photo.jpg", "Meister der Tarnung", "20261008",
        Copyright: "Krake in Abwehrhaltung, Mayotte, Indischer Ozean (© Gabriel Barathieu/Minden Pictures)");

    [Fact]
    public void CaptionSuppliesSubjectsAndPlacesWithoutCreditingPhotographerAsTag()
    {
        var tags = WallpaperKeywords.Build(Octopus, []);
        Assert.Equal(new[] { "Krake", "Tier", "Meer", "Mayotte", "Indischer Ozean" }, tags.Select(x => x.Label));
        Assert.All(tags, x => Assert.False(x.FromModel));
        var item = new ImageItemViewModel(Octopus);
        Assert.Contains("Mayotte", item.TextKeywords);
        item.SetLanguage("en");
        Assert.Contains("Mayotte", item.TextKeywords); // Bing language is independent of UI language.
        Assert.DoesNotContain("©", item.Description);
    }

    [Fact]
    public void EnglishRegionAndModelProvenanceAreIndependentAndDeduplicated()
    {
        var photo = Octopus with { Market = "en-US", Copyright = "Octopus, Mayotte, Indian Ocean (© Someone)" };
        var tags = WallpaperKeywords.Build(photo, [new("coral reef", .85f), new("octopus", .9f), new("forest", .1f)]);
        Assert.Contains(tags, x => x.Label == "Octopus" && !x.FromModel);
        Assert.Contains(tags, x => x.Label == "Underwater" && x.FromModel);
        Assert.Single(tags, x => x.Label == "Sea");
        Assert.DoesNotContain(tags, x => x.Label == "Forest");
        var item = new ImageItemViewModel(photo);
        item.ApplyPredictions([new("coral reef", .85f)]);
        Assert.True(item.HasModelKeywords);
        Assert.Contains("Underwater", item.ModelKeywords);
    }

    [Fact]
    public void DoesNotMatchWordFragmentsOrPhotographerNames()
    {
        Assert.Empty(WallpaperKeywords.Build(Octopus with { Copyright = "A charming scene (© Forest Bird)", Title = "A charming scene" }, []));
    }
}

