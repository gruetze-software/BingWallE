using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using BingWallE.Models;
using BingWallE.Services;
using BingWallE.ViewModels;
using ExifLibrary;
using Xunit;
using Avalonia.Headless.XUnit;

namespace BingWallE.Tests;

public class SpotlightTests
{
    private const string Asset = "https://res.public.onecdn.static.microsoft/creativeservice/sample.jpg";
    private static string Entry(string url = Asset, string id = "42", string caption = "Küste, Deutschland") =>
        JsonSerializer.Serialize(new { ad = new { entityId = id, iconHoverText = caption + @"\r\n© Photographer\r\nMore information",
            title = "Story", description = "Vögel am Meer", copyright = "© Photographer", ctaUri = "https://www.bing.com/spotlight?id=42",
            landscapeImage = new { asset = url } } });
    private static string Response(params string[] entries) => JsonSerializer.Serialize(new { batchrsp = new { items = entries.Select(x => new { item = x }) } });
    private static HttpResponseMessage Json(string data) => new(HttpStatusCode.OK) { Content = new StringContent(data, Encoding.UTF8, "application/json") };
    private static Wallpaper Photo => new(Asset, "Küste, Deutschland", "", Copyright: "© Photographer", Resolution: "Original",
        Source: WallpaperSource.WindowsSpotlight, SourceId: "42", Description: "Vögel am Meer");

    [Theory]
    [InlineData("de-DE", "DE")]
    [InlineData("en-US", "US")]
    [InlineData("en-GB", "GB")]
    public async Task UsesSelectedContentLanguageAndReadsMetadataWithoutTracking(string market, string country)
    {
        var requests = new List<Uri>();
        using var client = new HttpClient(new Handler((request, _) =>
        {
            requests.Add(request.RequestUri!);
            return Task.FromResult(request.RequestUri!.Host == "fd.api.iris.microsoft.com"
                ? Json(Response(Entry(), Entry(), "not json", Entry("http://unsafe.test/image.jpg", "bad"), Entry("https://unsafe.test/image.jpg", "other")))
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(TestImages.Jpeg) });
        }));
        var result = new List<WallpaperPreview>();
        await foreach (var image in new SpotlightWallpaperClient(client).ScanAsync(market, TestContext.Current.CancellationToken)) result.Add(image);
        var photo = Assert.Single(result).Image;
        Assert.Contains("locale=" + market, requests[0].Query);
        Assert.Contains("country=" + country, requests[0].Query);
        Assert.Equal(2, requests.Count);
        Assert.Equal(WallpaperSource.WindowsSpotlight, photo.Source);
        Assert.Equal("42", photo.SourceId); Assert.Equal(market, photo.Market);
        Assert.Equal("Küste, Deutschland", photo.Title); Assert.Equal("Vögel am Meer", photo.Description);
        Assert.Equal("© Photographer", photo.Copyright); Assert.Empty(photo.Date);
        Assert.Equal("https://www.bing.com/spotlight?id=42", photo.CopyrightUrl);
    }

    [AvaloniaFact]
    public async Task OptionalSourcePersistsAndBothSourcesAppearInGallery()
    {
        using var scope = new TestScope();
        var spotlightRequests = 0;
        using var http = new HttpClient(new Handler((request, _) =>
        {
            var uri = request.RequestUri!;
            if (uri.Host == "fd.api.iris.microsoft.com") { spotlightRequests++; return Task.FromResult(Json(Response(Entry()))); }
            if (uri.AbsolutePath.Contains("HPImageArchive")) return Task.FromResult(Json(uri.Query.Contains("idx=0&")
                ? """{"images":[{"url":"/bing.jpg","title":"Bing image","enddate":"20261021"}]}""" : """{"images":[]}"""));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(TestImages.Jpeg) });
        }));
        var settings = new TestSettings { Value = new() { WallpaperPath = scope.Path, Market = "de-DE", Language = "en" } };
        MainWindowViewModel Create() => new(new BingWallpaperClient(http), new WallpaperStore(http), settings, new TestDialogs(), spotlight: new SpotlightWallpaperClient(http));
        var vm = Create();
        await vm.StartStopCommand.ExecuteAsync(null);
        Assert.Single(vm.Wallpapers); Assert.Equal(0, spotlightRequests); Assert.False(vm.IncludeWindowsSpotlight);
        vm.IncludeWindowsSpotlight = true;
        Assert.True(settings.Value.IncludeWindowsSpotlight);
        var reopened = Create(); Assert.True(reopened.IncludeWindowsSpotlight);
        await reopened.StartStopCommand.ExecuteAsync(null);
        Assert.Equal(2, reopened.Wallpapers.Count); Assert.False(reopened.HasError);
        Assert.Equal("en", reopened.SelectedLanguage.Value); Assert.Equal("de-DE", reopened.SelectedMarket.Value);
        Assert.Equal("Source: Windows Spotlight", reopened.Wallpapers[1].SourceLabel);
        Assert.Contains("original resolution", reopened.Wallpapers[1].ResolutionLabel);
        Assert.Equal("Source: Bing", reopened.Wallpapers[0].SourceLabel);
        reopened.IncludeWindowsSpotlight = false;
        await reopened.StartStopCommand.ExecuteAsync(null);
        Assert.Single(reopened.Wallpapers); Assert.Equal(1, spotlightRequests);
    }

    [AvaloniaFact]
    public async Task SpotlightFailureKeepsBingImagesAndOffersRetry()
    {
        using var scope = new TestScope();
        using var http = new HttpClient(new Handler((request, _) => Task.FromResult(
            request.RequestUri!.Host == "fd.api.iris.microsoft.com" ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            : request.RequestUri.AbsolutePath.Contains("HPImageArchive") ? Json(request.RequestUri.Query.Contains("idx=0&")
                ? """{"images":[{"url":"/bing.jpg","title":"Bing image","enddate":"20261021"}]}""" : """{"images":[]}""")
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(TestImages.Jpeg) })));
        var vm = new MainWindowViewModel(new BingWallpaperClient(http), new WallpaperStore(http),
            new TestSettings { Value = new() { WallpaperPath = scope.Path, IncludeWindowsSpotlight = true } }, new TestDialogs(), spotlight: new SpotlightWallpaperClient(http));
        await vm.StartStopCommand.ExecuteAsync(null);
        Assert.Single(vm.Wallpapers); Assert.True(vm.HasError); Assert.True(vm.CanRetry); Assert.False(vm.IsRunning);
        Assert.Contains("Windows-Blickpunkt", vm.StatusMessage);
    }

    [Fact]
    public async Task OriginalDownloadKeepsSourceAndPixelsForEverySelectedResolution()
    {
        using var scope = new TestScope();
        using var http = new HttpClient(new Handler((request, _) =>
        {
            Assert.Equal(Asset, request.RequestUri!.AbsoluteUri);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(TestImages.Jpeg) });
        }));
        var store = new WallpaperStore(http);
        foreach (var resolution in new[] { "1920x1080", "1366x768", "UHD" })
        {
            var image = WallpaperVariants.ForResolution(Photo, resolution);
            Assert.Equal(Photo, image);
            await store.DownloadAsync(scope.Path, image, TestContext.Current.CancellationToken);
        }
        Assert.Single(Directory.GetFiles(scope.Path));
        var file = ImageFile.FromFile(store.GetPath(scope.Path, Photo));
        Assert.Contains("Source: Windows Spotlight", file.Properties.Get(ExifTag.WindowsComment).Value.ToString());
        Assert.Contains("Windows Spotlight", file.Properties.Get(ExifTag.WindowsKeywords).Value.ToString());
        Assert.DoesNotContain("Bing;", file.Properties.Get(ExifTag.WindowsKeywords).Value.ToString());
        Assert.Equal("© Photographer", file.Properties.Get(ExifTag.Copyright).Value);
        var bytes = await File.ReadAllBytesAsync(store.GetPath(scope.Path, Photo), TestContext.Current.CancellationToken);
        var text = Encoding.UTF8.GetString(bytes);
        var start = text.IndexOf("<x:xmpmeta", StringComparison.Ordinal);
        var end = text.IndexOf("</x:xmpmeta>", start, StringComparison.Ordinal) + "</x:xmpmeta>".Length;
        var xmp = XDocument.Parse(text[start..end]);
        XNamespace bw = "https://github.com/gruetze-software/BingWallE/metadata/1.0/";
        Assert.Equal("Windows Spotlight", Assert.Single(xmp.Descendants(bw + "Source")).Value);
        Assert.Equal("42", Assert.Single(xmp.Descendants(bw + "SourceId")).Value);
        Assert.Equal(Asset, Assert.Single(xmp.Descendants(bw + "SourceUrl")).Value);
        Assert.Empty(xmp.Descendants(bw + "PublicationDate"));
        Assert.NotEqual(store.GetPath(scope.Path, Photo), store.GetPath(scope.Path, Photo with { SourceId = "43" }));
    }

    [Fact]
    public async Task InvalidResponseIsReportedAndCancellationIsNotSwallowed()
    {
        using var invalid = new HttpClient(new Handler((_, _) => Task.FromResult(Json("{}"))));
        await Assert.ThrowsAsync<InvalidDataException>(async () =>
        { await foreach (var _ in new SpotlightWallpaperClient(invalid).ScanAsync("de-DE", TestContext.Current.CancellationToken)) { } });
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        { await foreach (var _ in new SpotlightWallpaperClient(invalid).ScanAsync("de-DE", cancellation.Token)) { } });
    }
}
