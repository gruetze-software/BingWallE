using System.Net;
using System.Text;
using BingWallE.Models;
using BingWallE.Services;
using BingWallE.ViewModels;
using ExifLibrary;
using Xunit;

namespace BingWallE.Tests;

public class PhotoMetadataTests
{
    private static Wallpaper Photo => new("https://bing.com/image.jpg", "Küste & Vögel", "20261010", "/image", "Küste (© Jörg Müller)", "https://bing.com/info");

    [Theory]
    [InlineData("Ohrenscharben über der Monterey Bay, Kalifornien, USA (© Hiroya Minakuchi/Minden Pictures)", "Ohrenscharben über der Monterey Bay, Kalifornien, USA", "© Hiroya Minakuchi/Minden Pictures")]
    [InlineData("Ort (Zusatz) (© Name/Agentur)", "Ort (Zusatz)", "© Name/Agentur")]
    [InlineData("Beschreibung ohne Copyright", "Beschreibung ohne Copyright", "")]
    [InlineData("© Name", "", "© Name")]
    public void SeparatesCopyrightNoticeFromCaption(string input, string description, string credit)
    {
        Assert.Equal((description, credit), WallpaperCredits.Split(input));
        Assert.Equal(credit, new ImageItemViewModel(Photo with { Copyright = input }).CopyrightCredit);
    }

    [Fact]
    public void WritesUnicodeMetadataWithoutRecompressingOrInventingCameraData()
    {
        var original = ImageFile.FromBuffer(TestImages.Jpeg);
        original.Properties.Set(ExifTag.Artist, "Original photographer");
        using var input = new MemoryStream(); original.Save(input);
        var before = input.ToArray();
        var after = PhotoMetadataWriter.Write(before, Photo, new[] { new ImageTag("seashore", .85f) });
        var result = ImageFile.FromBuffer(after);
        Assert.Equal(Photo.Title, result.Properties.Get(ExifTag.WindowsTitle).Value);
        Assert.Equal(Photo.Title, result.Properties.Get(ExifTag.ImageDescription).Value);
        Assert.Equal("© Jörg Müller", result.Properties.Get(ExifTag.Copyright).Value);
        Assert.Equal("Küste", result.Properties.Get(ExifTag.WindowsSubject).Value);
        Assert.Contains("Bing publication date: 2026-10-10", result.Properties.Get(ExifTag.WindowsComment).Value.ToString());
        Assert.Contains("AI suggestions", result.Properties.Get(ExifTag.WindowsComment).Value.ToString());
        Assert.Contains("seashore", result.Properties.Get(ExifTag.WindowsKeywords).Value.ToString());
        Assert.Equal("Original photographer", result.Properties.Get(ExifTag.Artist).Value);
        Assert.DoesNotContain(result.Properties, x => x.Tag == ExifTag.DateTimeOriginal);
        Assert.Contains("Küste &amp; Vögel", Encoding.UTF8.GetString(after));
        Assert.Contains("http://purl.org/dc/elements/1.1/", Encoding.UTF8.GetString(after));
        Assert.Equal(ScanData(before), ScanData(after));
    }

    [Fact]
    public void UpdatingMetadataKeepsOneXmpPacketAndOriginalPixels()
    {
        var first = PhotoMetadataWriter.Write(TestImages.Jpeg, Photo, Array.Empty<ImageTag>());
        var second = PhotoMetadataWriter.Write(first, Photo with { Title = "New title" }, Array.Empty<ImageTag>());
        Assert.Equal(2, Encoding.UTF8.GetString(second).Split("http://ns.adobe.com/xap/1.0/\0").Length);
        Assert.Equal("New title", ImageFile.FromBuffer(second).Properties.Get(ExifTag.WindowsTitle).Value);
        Assert.Equal(ScanData(first), ScanData(second));
    }

    [Fact]
    public async Task FailedRecognitionLeavesExistingImageUntouched()
    {
        using var scope = new TestScope();
        var destination = Path.Combine(scope.Path, WallpaperVariants.FileName(Photo));
        File.WriteAllText(destination, "original");
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(TestImages.Jpeg) })));
        var store = new WallpaperStore(http, new FailingTagger());
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.DownloadAsync(scope.Path, Photo, default, true));
        Assert.Equal("original", File.ReadAllText(destination)); Assert.Single(Directory.GetFiles(scope.Path));
    }

    [Fact]
    public void UncertainRecognitionProducesNoTagsAndResolutionFollowsSelection()
    {
        Assert.Empty(LocalImageTagger.SelectTags(new float[] { 0, 0, 0 }, new[] { "coast", "bird", "mountain" }));
        Assert.Equal("coast", Assert.Single(LocalImageTagger.SelectTags(new float[] { 4, 0, 0 }, new[] { "coast", "bird", "mountain" })).Label);
        var item = new ImageItemViewModel(Photo);
        Assert.Contains("1920 × 1080", item.ResolutionLabel);
        item.DownloadResolution = "UHD"; Assert.Contains("UHD", item.ResolutionLabel);
        item.ActualDimensions = "3840 × 2160"; Assert.Contains("3840 × 2160", item.ResolutionLabel);
    }

    [ModelFact]
    public async Task NativeModelRunsOnCurrentPlatform()
    {
        using var scope = new TestScope();
        using var tagger = new LocalImageTagger(scope.Path);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(4));
        var result = await tagger.RecognizeAsync(TestImages.Jpeg, cancellation.Token);
        Assert.All(result, x => Assert.InRange(x.Score, .5f, 1f));
        // Reuse must work with the cached model, without downloading again.
        var second = await tagger.RecognizeAsync(TestImages.Jpeg, default);
        Assert.Equal(result, second);
    }

    private sealed class FailingTagger : IImageTagger
    {
        public Task<IReadOnlyList<ImageTag>> RecognizeAsync(byte[] image, CancellationToken ct, Action<string>? progress = null) => throw new InvalidOperationException("test failure");
    }
    private static byte[] ScanData(byte[] jpeg)
    {
        var offset = 2;
        while (offset + 4 <= jpeg.Length)
        {
            Assert.Equal(255, jpeg[offset]);
            if (jpeg[offset + 1] == 0xDA) return jpeg[offset..];
            offset += 2 + (jpeg[offset + 2] << 8 | jpeg[offset + 3]);
        }
        throw new InvalidDataException("No JPEG scan");
    }
}

public sealed class ModelFactAttribute : FactAttribute
{
    public ModelFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("BINGWALLE_TEST_MODEL") != "1")
            Skip = "Set BINGWALLE_TEST_MODEL=1 to download and test the real local model.";
    }
}
