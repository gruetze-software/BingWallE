using System.Net;
using System.Globalization;
using System.Text.Json;
using BingWallE.Localization;
using BingWallE.Models;
using BingWallE.Services;
using BingWallE.ViewModels;
using Xunit;

namespace BingWallE.Tests;

public class LocalizationTests
{
    [Theory]
    [InlineData("en-GB", "de-DE", "en")]
    [InlineData("en-US", "en-US", "en")]
    [InlineData("de-DE", "en-GB", "de")]
    [InlineData("de-AT", "en-US", "de")]
    [InlineData("de-CH", "fr-CH", "de")]
    [InlineData("fr-FR", "fr-FR", "en")]
    [InlineData("", "", "en")]
    public void FirstStartUsesSystemUiLanguageWithoutChangingTheImageRegion(string ui, string format, string expected)
    {
        WithCulture(ui, format, () =>
        {
            using var scope = new TestScope();
            using var http = Offline();
            var store = new JsonSettingsStore(Path.Combine(scope.Path, "settings.json"));
            var settings = store.Load();
            Assert.Equal(expected, settings.Language);
            Assert.Equal(format, settings.Market);
            var vm = new MainWindowViewModel(new BingWallpaperClient(http), new WallpaperStore(http), store, new TestDialogs());
            Assert.Equal(expected, vm.SelectedLanguage.Value);
            Assert.Equal(expected == "de" ? "Aktualisieren" : "Refresh", vm.ButtonText);
            Assert.False(File.Exists(Path.Combine(scope.Path, "settings.json")));
        });
    }

    [Theory]
    [InlineData("en-GB", "de")]
    [InlineData("de-DE", "en")]
    public void SavedLanguageOverridesSystemLanguage(string ui, string saved)
    {
        WithCulture(ui, "fr-FR", () =>
        {
            using var scope = new TestScope();
            using var http = Offline();
            var store = new JsonSettingsStore(Path.Combine(scope.Path, "settings.json"));
            store.Save(new AppSettings { Language = saved, Market = "en-US", WallpaperPath = scope.Path });
            var vm = new MainWindowViewModel(new BingWallpaperClient(http), new WallpaperStore(http), store, new TestDialogs());
            Assert.Equal(saved, vm.SelectedLanguage.Value);
            Assert.Equal("en-US", vm.SelectedMarket.Value);
            Assert.Equal(saved, store.Load().Language);
        });
    }

    [Theory]
    [InlineData("en-GB", "en")]
    [InlineData("de-AT", "de")]
    public void LegacyOrInvalidLanguageFallsBackToSystemUiLanguage(string ui, string expected)
    {
        WithCulture(ui, "en-US", () =>
        {
            using var scope = new TestScope();
            using var http = Offline();
            var path = Path.Combine(scope.Path, "settings.json");
            var store = new JsonSettingsStore(path);
            File.WriteAllText(path, JsonSerializer.Serialize(new { WallpaperPath = scope.Path, Market = "en-US" }));
            var legacy = new MainWindowViewModel(new BingWallpaperClient(http), new WallpaperStore(http), store, new TestDialogs());
            Assert.Equal(expected, legacy.SelectedLanguage.Value);
            foreach (var language in new string?[] { null, "", "unsupported" })
            {
                File.WriteAllText(path, JsonSerializer.Serialize(new { WallpaperPath = scope.Path, Language = language }));
                var vm = new MainWindowViewModel(new BingWallpaperClient(http), new WallpaperStore(http), store, new TestDialogs());
                Assert.Equal(expected, vm.SelectedLanguage.Value);
            }
            File.WriteAllText(path, "invalid json");
            Assert.Equal(expected, store.Load().Language);
        });
    }

    private static HttpClient Offline() => new(new Handler((_, _) => throw new Exception("Unexpected HTTP")));
    private static void WithCulture(string ui, string format, Action action)
    {
        var previousUi = CultureInfo.CurrentUICulture;
        var previousFormat = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(ui);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(format);
            action();
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUi;
            CultureInfo.CurrentCulture = previousFormat;
        }
    }

    [Fact]
    public async Task EnglishDownloadsUseEnglishMarketWithoutChangingAppLanguage()
    {
        using var scope = new TestScope();
        string? requestedMarket = null;
        using var client = new HttpClient(new Handler((request, _) =>
        {
            requestedMarket = request.RequestUri!.Query;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent("{\"images\":[]}") });
        }));
        var settings = new TestSettings { Value = new AppSettings { Language = "de", Market = "de-DE", WallpaperPath = scope.Path } };
        var vm = new MainWindowViewModel(new BingWallpaperClient(client), new WallpaperStore(client), settings, new TestDialogs());
        vm.SelectedMarket = vm.Markets.Single(x => x.Value == "en-US");
        await vm.StartStopCommand.ExecuteAsync(null);
        Assert.Contains("mkt=en-US", requestedMarket);
        Assert.Equal("de", vm.SelectedLanguage.Value);
        Assert.Equal("en-US", settings.Value.Market);
        Assert.Equal("de", settings.Value.Language);
        Assert.Equal("Aktualisieren", vm.ButtonText);
    }

    [Fact]
    public void SwitchingLanguagePreservesSelectionAndPersistsPreference()
    {
        using var client = new HttpClient(new Handler((_, _) => throw new Exception("Unexpected HTTP")));
        var settings = new TestSettings { Value = new AppSettings { Language = "de", Market = "de-DE" } };
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
        text.SetLanguage("unsupported"); Assert.Equal(UiText.SystemLanguage, text.Language);
    }
}
