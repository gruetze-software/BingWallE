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
    public void WritesUnicodeMetadataWithoutRecompressingAndPreservesCameraFields()
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
        Assert.Equal("Küste\nSource: Bing", result.Properties.Get(ExifTag.WindowsComment).Value.ToString());
        Assert.Contains("PublicationDate", Encoding.UTF8.GetString(after));
        Assert.Contains(Photo.Url, Encoding.UTF8.GetString(after));
        Assert.DoesNotContain("AI suggestions", result.Properties.Get(ExifTag.WindowsComment).Value.ToString());
        Assert.Contains("Küste", result.Properties.Get(ExifTag.WindowsKeywords).Value.ToString());
        Assert.Equal("Original photographer", result.Properties.Get(ExifTag.Artist).Value);
        Assert.IsType<DateTime>(result.Properties.Get(ExifTag.DateTimeOriginal).Value);
        Assert.Contains("Küste &amp; Vögel", Encoding.UTF8.GetString(after));
        Assert.Contains("http://purl.org/dc/elements/1.1/", Encoding.UTF8.GetString(after));
        Assert.Equal(ScanData(before), ScanData(after));
    }

    [Fact]
    public void XmpLanguageQualifiersAreAttributesAndNeverPartOfDisplayText()
    {
        var photo = Photo with { Description = "Über der Küste & dem Fluss" };
        var bytes = PhotoMetadataWriter.Write(TestImages.Jpeg, photo, Array.Empty<ImageTag>());
        var text = Encoding.UTF8.GetString(bytes);
        var start = text.IndexOf("<x:xmpmeta", StringComparison.Ordinal);
        var end = text.IndexOf("</x:xmpmeta>", start, StringComparison.Ordinal) + "</x:xmpmeta>".Length;
        var packet = System.Xml.Linq.XDocument.Parse(text[start..end]);
        System.Xml.Linq.XNamespace dc = "http://purl.org/dc/elements/1.1/";
        System.Xml.Linq.XNamespace rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
        var expected = new[]
        {
            ("title", photo.Title),
            ("description", photo.Description + "\nSource: Bing"),
            ("rights", WallpaperCredits.Split(photo.Copyright).Copyright)
        };
        foreach (var (name, value) in expected)
        {
            var field = Assert.Single(packet.Descendants(dc + name));
            var alternative = Assert.Single(field.Elements(rdf + "Alt"));
            var item = Assert.Single(alternative.Elements(rdf + "li"));
            Assert.Equal("x-default", item.Attribute(System.Xml.Linq.XNamespace.Xml + "lang")?.Value);
            Assert.Equal(value, item.Value);
            Assert.DoesNotContain("lang=", item.Value);
        }
        Assert.Equal(ScanData(TestImages.Jpeg), ScanData(bytes));
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
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.DownloadAsync(scope.Path, Photo, TestContext.Current.CancellationToken, true));
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

    [Fact]
    public void MissingCreationAndOriginalDatesUseTheDownloadTimeAndAreIdentifiedAsFallbacks()
    {
        var timestamp = new DateTimeOffset(2026, 10, 21, 14, 35, 12, TimeSpan.FromHours(2));
        var bytes = PhotoMetadataWriter.Write(TestImages.Jpeg, Photo, Array.Empty<ImageTag>(), timestamp);
        var result = ImageFile.FromBuffer(bytes);
        Assert.Equal(timestamp.DateTime, result.Properties.Get(ExifTag.DateTimeDigitized).Value);
        Assert.Equal(timestamp.DateTime, result.Properties.Get(ExifTag.DateTimeOriginal).Value);
        var text = Encoding.UTF8.GetString(bytes);
        Assert.Contains(timestamp.ToString("O"), text);
        Assert.Contains("CreationDateSource", text); Assert.Contains("OriginalDateSource", text);
        Assert.Contains(">Download<", text);
        Assert.Equal(ScanData(TestImages.Jpeg), ScanData(bytes));
    }

    [Fact]
    public void ExistingPhotoDatesAreNeverReplacedByTheDownloadTime()
    {
        var file = ImageFile.FromBuffer(TestImages.Jpeg);
        var created = new DateTime(2012, 3, 4, 5, 6, 7);
        var original = new DateTime(2011, 2, 3, 4, 5, 6);
        file.Properties.Set(ExifTag.DateTimeDigitized, created);
        file.Properties.Set(ExifTag.DateTimeOriginal, original);
        using var input = new MemoryStream(); file.Save(input);
        var bytes = PhotoMetadataWriter.Write(input.ToArray(), Photo, Array.Empty<ImageTag>(), DateTimeOffset.Now);
        var result = ImageFile.FromBuffer(bytes);
        Assert.Equal(created, result.Properties.Get(ExifTag.DateTimeDigitized).Value);
        Assert.Equal(original, result.Properties.Get(ExifTag.DateTimeOriginal).Value);
        Assert.Contains(created.ToString("yyyy-MM-ddTHH:mm:ss"), Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void RewritingMetadataPreservesCreationDateAndItsFallbackOrigin()
    {
        var firstTime = new DateTimeOffset(2026, 10, 21, 14, 35, 12, TimeSpan.FromHours(2));
        var first = PhotoMetadataWriter.Write(TestImages.Jpeg, Photo, Array.Empty<ImageTag>(), firstTime);
        var secondTime = firstTime.AddDays(1);
        var second = PhotoMetadataWriter.Write(first, Photo, Array.Empty<ImageTag>(), secondTime);
        var result = ImageFile.FromBuffer(second);
        Assert.Equal(firstTime.DateTime, result.Properties.Get(ExifTag.DateTimeDigitized).Value);
        Assert.Equal(firstTime.DateTime, result.Properties.Get(ExifTag.DateTimeOriginal).Value);
        var text = Encoding.UTF8.GetString(second);
        var start = text.IndexOf("<x:xmpmeta", StringComparison.Ordinal);
        var end = text.IndexOf("</x:xmpmeta>", start, StringComparison.Ordinal) + "</x:xmpmeta>".Length;
        var xmp = System.Xml.Linq.XDocument.Parse(text[start..end]);
        System.Xml.Linq.XNamespace standard = "http://ns.adobe.com/xap/1.0/";
        System.Xml.Linq.XNamespace bw = "https://github.com/gruetze-software/BingWallE/metadata/1.0/";
        Assert.Equal(firstTime.ToString("O"), Assert.Single(xmp.Descendants(standard + "CreateDate")).Value);
        Assert.Equal(secondTime.ToString("O"), Assert.Single(xmp.Descendants(bw + "DownloadDate")).Value);
        Assert.Equal("Download", Assert.Single(xmp.Descendants(bw + "CreationDateSource")).Value);
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
        var second = await tagger.RecognizeAsync(TestImages.Jpeg, TestContext.Current.CancellationToken);
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
    public ModelFactAttribute([System.Runtime.CompilerServices.CallerFilePath] string? sourceFilePath = null,
        [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0) : base(sourceFilePath, sourceLineNumber)
    {
        if (Environment.GetEnvironmentVariable("BINGWALLE_TEST_MODEL") != "1")
            Skip = "Set BINGWALLE_TEST_MODEL=1 to download and test the real local model.";
    }
}
