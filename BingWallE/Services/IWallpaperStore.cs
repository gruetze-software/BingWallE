using System.Threading;
using System.Threading.Tasks;
using BingWallE.Models;

namespace BingWallE.Services;

public interface IWallpaperStore
{
    bool Exists(string folder, Wallpaper image);
    Task DownloadAsync(string folder, Wallpaper image, CancellationToken cancellationToken);
}
