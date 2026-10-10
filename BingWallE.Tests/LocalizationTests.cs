using System.Net;
using BingWallE.Localization;
using BingWallE.Models;
using BingWallE.Services;
using BingWallE.ViewModels;
using Xunit;

namespace BingWallE.Tests;

public class LocalizationTests
{
    [Fact]
    public void SwitchingLanguagePreservesSelectionAndPersistsPreference()
    {
        using var client = new HttpClient(new Handler((_, _) => throw new Exception("Unexpected HTTP")));
        var settings = new TestSettings { Value = new AppSettings { Market = "de-DE" } };
        var vm = new MainWindowViewModel(new BingWallpaperClient(client), new WallpaperStore(client), settings, new TestDialogs());
        var item = new ImageItemViewModel("https://example.test/image.jpg", "Original title", "20261009") { IsSelected = true };
        vm.Wallpapers.Add(item);
        var market = vm.SelectedMarket;
        vm.SelectedLanguage = vm.Languages.Single(x => x.Value == "en");
        Assert.Same(market, vm.SelectedMarket);
        Assert.Same(item, Assert.Single(vm.Wallpapers));
        Assert.True(item.IsSelected);
        Assert.Equal("Germany", vm.SelectedMarket.Label);
        Assert.Equal("9 October 2026", item.DisplayDate);
        Assert.Equal("Download 1 image", vm.DownloadLabel);
        Assert.Equal("New to your collection", item.SaveLabel);
        Assert.Equal("en", settings.Value.Language);
        var reopened = new MainWindowViewModel(new BingWallpaperClient(client), new WallpaperStore(client), settings, new TestDialogs());
        Assert.Equal("en", reopened.SelectedLanguage.Value);
        Assert.Equal("Refresh", reopened.ButtonText);
        vm.SelectedLanguage = vm.Languages.Single(x => x.Value == "de");
        Assert.Equal("Deutschland", vm.SelectedMarket.Label);
        Assert.Equal("9. Oktober 2026", item.DisplayDate);
        Assert.True(item.IsSelected);
    }

    [Fact]
    public async Task ErrorAndRetryRemainUsableAcrossLanguageChanges()
    {
        using var scope = new TestScope();
        using var client = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound))));
        var settings = new TestSettings { Value = new AppSettings { Language = "en", WallpaperPath = scope.Path } };
        var vm = new MainWindowViewModel(new BingWallpaperClient(client), new WallpaperStore(client), settings, new TestDialogs());
        vm.Wallpapers.Add(new ImageItemViewModel("https://example.test/image.jpg", "Title", "20261009") { IsSelected = true });
        await vm.DownloadSelectedCommand.ExecuteAsync(null);
        Assert.Contains("not available", vm.StatusMessage);
        Assert.Contains("0 of 1 images saved", vm.StatusMessage);
        Assert.True(vm.CanRetry);
        vm.SelectedLanguage = vm.Languages.Single(x => x.Value == "de");
        Assert.Contains("nicht verfügbar", vm.StatusMessage);
        Assert.Contains("0 von 1 Bildern gespeichert", vm.StatusMessage);
        Assert.True(vm.CanRetry);
    }

    [Fact]
    public void CatalogHasMatchingFormatArgumentsAndProgressIsTranslated()
    {
        var text = new UiText(); text.SetLanguage("en");
        foreach (var entry in UiText.Entries)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.En));
            var deArgs = System.Text.RegularExpressions.Regex.Matches(entry.De, @"\{\d+\}").Select(x => x.Value).Order().ToArray();
            var enArgs = System.Text.RegularExpressions.Regex.Matches(entry.En, @"\{\d+\}").Select(x => x.Value).Order().ToArray();
            Assert.Equal(deArgs, enArgs);
        }
        Assert.Equal("Bing: day 3 of 8 – loading preview …", text.Progress("Bing: Tag 3 von 8 – Vorschau laden …"));
        text.SetLanguage("unsupported"); Assert.Equal("de", text.Language);
    }
}
