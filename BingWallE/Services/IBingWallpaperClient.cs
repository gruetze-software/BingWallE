using System;
using System.Collections.Generic;
using System.Threading;
using BingWallE.Models;

namespace BingWallE.Services;

public interface IBingWallpaperClient
{
    System.Threading.Tasks.Task<byte[]> LoadPreviewAsync(Wallpaper image, CancellationToken ct);
    IAsyncEnumerable<WallpaperPreview> ScanAsync(string market, CancellationToken cancellationToken, Action<string>? reportProgress = null);
}
