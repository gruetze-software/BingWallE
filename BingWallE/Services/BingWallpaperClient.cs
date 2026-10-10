using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading;
using BingWallE.Models;

namespace BingWallE.Services;

public sealed class BingWallpaperClient(HttpClient httpClient) : IBingWallpaperClient
{
    // Use the Bing origin for both archive and relative image URLs.
    // The www CDN can resolve to unreachable IPv6 endpoints on some networks.
    public async IAsyncEnumerable<WallpaperPreview> ScanAsync(
        string market, [EnumeratorCancellation] CancellationToken cancellationToken, Action<string>? reportProgress = null)
    {
        if (string.IsNullOrWhiteSpace(market)) market = "en-US";
        Trace.WriteLine($"[{DateTimeOffset.Now:O}] Bing scan START market={market}");
        for (var day = 0; day < 8; day++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var url = $"https://bing.com/HPImageArchive.aspx?format=js&idx={day}&n=1&mkt={Uri.EscapeDataString(market)}";
            reportProgress?.Invoke($"Bing: Tag {day + 1} von 8 – Bilddaten laden …");
            var response = await httpClient.GetFromJsonAsync<BingResponse>(url, cancellationToken);
            if (response?.Images == null || response.Images.Length == 0)
            {
                Trace.WriteLine($"[{DateTimeOffset.Now:O}] Bing scan COMPLETE: archive empty at day index {day}.");
                yield break;
            }
            var image = response.Images[0];
            if (string.IsNullOrWhiteSpace(image.Url) || string.IsNullOrWhiteSpace(image.Enddate))
                throw new InvalidDataException("Bing hat unvollständige Bilddaten geliefert.");
            var fullUrl = new Uri(new Uri("https://bing.com"), image.Url).AbsoluteUri;
            var separator = fullUrl.Contains('?') ? '&' : '?';
            reportProgress?.Invoke($"Bing: Tag {day + 1} von 8 – Vorschau laden …");
            var preview = await httpClient.GetByteArrayAsync(
                fullUrl + separator + "pid=hp&w=640&h=360", cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            Trace.WriteLine($"[{DateTimeOffset.Now:O}] Bing preview ready: day={day + 1}, bytes={preview.Length}");
            yield return new WallpaperPreview(new Wallpaper(fullUrl, image.Title ?? "Bing", image.Enddate, image.Urlbase, image.Copyright ?? "", image.Copyrightlink, Market: market), preview);
        }
        Trace.WriteLine($"[{DateTimeOffset.Now:O}] Bing scan COMPLETE: 8 days.");
    }

    public System.Threading.Tasks.Task<byte[]> LoadPreviewAsync(Wallpaper image, CancellationToken ct)
    {
        var separator = image.Url.Contains('?') ? '&' : '?';
        return httpClient.GetByteArrayAsync(image.Url + separator + "w=1280&h=720", ct);
    }

    private sealed record BingResponse(BingImage[]? Images);
    private sealed record BingImage(string? Url, string? Title, string? Enddate,
        string? Urlbase = null, string? Copyright = null, string? Copyrightlink = null);
}
