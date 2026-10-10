using System;
using System.Threading;
using System.Threading.Tasks;
using BingWallE.Models;

namespace BingWallE.Services;

public interface IWallpaperStore
{
    bool Exists(string folder, Wallpaper image);
    Task<WallpaperDownloadResult> DownloadAsync(string folder, Wallpaper image, CancellationToken cancellationToken, bool generateTags = false, Action<string>? progress = null);
}
