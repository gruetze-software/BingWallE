using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BingWallE.Models;

namespace BingWallE.Services;

// Protocol reference: ORelio/Spotlight-Downloader, SpotlightAPI.md (reverse-engineered API).
// This client is implemented independently; the service is not a supported public Microsoft API.
public sealed class SpotlightWallpaperClient(HttpClient httpClient) : IBingWallpaperClient
{
    public async IAsyncEnumerable<WallpaperPreview> ScanAsync(string market,
        [EnumeratorCancellation] CancellationToken cancellationToken, Action<string>? reportProgress = null)
    {
        var culture = CultureInfo.GetCultureInfo(string.IsNullOrWhiteSpace(market) ? "en-US" : market);
        var country = new RegionInfo(culture.Name).TwoLetterISORegionName;
        var endpoint = "https://fd.api.iris.microsoft.com/v4/api/selection?placement=88000820&bcnt=4&fmt=json"
            + "&country=" + Uri.EscapeDataString(country) + "&locale=" + Uri.EscapeDataString(culture.Name);
        reportProgress?.Invoke("Windows-Blickpunkt: Bilddaten laden …");
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 BingWallE/" + AppInfo.Version);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        if (document.RootElement.ValueKind != JsonValueKind.Object || !document.RootElement.TryGetProperty("batchrsp", out var batch) || batch.ValueKind != JsonValueKind.Object ||
            !batch.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Windows-Blickpunkt hat ungültige Bilddaten geliefert.");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var wrapper in items.EnumerateArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var image = ReadImage(wrapper, culture.Name);
            if (image == null || !seen.Add(image.SourceId ?? image.Url)) continue;
            reportProgress?.Invoke("Windows-Blickpunkt: Vorschau laden …");
            var bytes = await LoadPreviewAsync(image, cancellationToken);
            yield return new WallpaperPreview(image, bytes);
        }
    }

    public Task<byte[]> LoadPreviewAsync(Wallpaper image, CancellationToken ct) => httpClient.GetByteArrayAsync(image.Url, ct);

    private static Wallpaper? ReadImage(JsonElement wrapper, string market)
    {
        if (wrapper.ValueKind != JsonValueKind.Object || String(wrapper, "item") is not { } json) return null;
        try
        {
            using var item = JsonDocument.Parse(json);
            if (item.RootElement.ValueKind != JsonValueKind.Object || !item.RootElement.TryGetProperty("ad", out var ad) || ad.ValueKind != JsonValueKind.Object ||
                !ad.TryGetProperty("landscapeImage", out var landscape) || landscape.ValueKind != JsonValueKind.Object ||
                !Uri.TryCreate(String(landscape, "asset"), UriKind.Absolute, out var uri) || !IsImageUri(uri)) return null;
            var title = String(ad, "iconHoverText")?.Replace("\\r\\n", "\n").Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
            if (string.IsNullOrWhiteSpace(title)) title = String(ad, "title") ?? "Windows Spotlight";
            return new Wallpaper(uri.AbsoluteUri, title, "", Copyright: String(ad, "copyright") ?? "",
                CopyrightUrl: Uri.TryCreate(String(ad, "ctaUri"), UriKind.Absolute, out var info) && info.Scheme == "https" ? info.AbsoluteUri : null,
                Resolution: "Original", Market: market, Source: WallpaperSource.WindowsSpotlight,
                SourceId: String(ad, "entityId") ?? uri.GetLeftPart(UriPartial.Path), Description: String(ad, "description"));
        }
        catch (JsonException ex) { Trace.WriteLine("Skipping invalid Spotlight entry: " + ex.Message); return null; }
    }

    private static string? String(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool IsImageUri(Uri uri) => uri.Scheme == "https" &&
        (uri.Host.EndsWith(".static.microsoft", StringComparison.OrdinalIgnoreCase) ||
         uri.Host.EndsWith(".microsoft.com", StringComparison.OrdinalIgnoreCase) ||
         uri.Host.EndsWith(".msn.com", StringComparison.OrdinalIgnoreCase) ||
         uri.Host.Equals("img-prod-cms-rt-microsoft-com.akamaized.net", StringComparison.OrdinalIgnoreCase));
}
