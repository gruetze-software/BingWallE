using BingWallE.Models;
using BingWallE.Services;
using BingWallE.ViewModels;
using Xunit;

namespace BingWallE.Tests;

public class WallpaperKeywordTests
{
    private static Wallpaper Octopus => new("https://bing.com/photo.jpg", "Meister der Tarnung", "20261008",
        Copyright: "Krake in Abwehrhaltung, Mayotte, Indischer Ozean (© Gabriel Barathieu/Minden Pictures)");

    [Fact]
    public void CaptionSuppliesSubjectsAndPlacesWithoutInventingUnderwaterOrCreditingPhotographerAsTag()
    {
        var tags = WallpaperKeywords.Build(Octopus, []);
        Assert.Equal(new[] { "Krake", "Tier", "Meer", "Mayotte", "Indischer Ozean" }, tags.Select(x => x.Label));
        Assert.All(tags, x => Assert.False(x.FromModel));
        var item = new ImageItemViewModel(Octopus);
        Assert.Contains("Meer", item.TextKeywords);
        item.SetLanguage("en");
        Assert.Contains("Meer", item.TextKeywords); // Bing language is independent of UI language.
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
    public void DisablingRecognitionClearsCachedSuggestionsForFutureDownloads()
    {
        using var client = new System.Net.Http.HttpClient();
        var vm = new MainWindowViewModel(new BingWallpaperClient(client), new WallpaperStore(client), new TestSettings(), new TestDialogs());
        var item = new ImageItemViewModel(Octopus);
        vm.Wallpapers.Add(item);
        vm.AutoTags = true;
        item.ApplyPredictions([new("coral reef", .85f)]);
        vm.AutoTags = false;
        Assert.False(item.HasRecognition);
        Assert.False(item.HasModelKeywords);
        Assert.Contains("Meer", item.TextKeywords);
    }

    [Fact]
    public void DoesNotMatchWordFragmentsOrPhotographerNames()
    {
        Assert.Empty(WallpaperKeywords.Build(Octopus with { Copyright = "A charming scene (© Forest Bird)", Title = "Mountains" }, []));
    }
}

