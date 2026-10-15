using System.Threading.Tasks;

namespace BingWallE.Services;

public interface IAppDialogs
{
    Task OpenFolderAsync(string folder);
    Task ShowFileAsync(string path) => OpenFolderAsync(System.IO.Path.GetDirectoryName(path)!);
    Task<bool> ConfirmDeleteAsync(string path) => Task.FromResult(false);
    void ApplyTheme(string theme);
    Task<string?> SelectFolderAsync();
    Task ShowInfoAsync(string title);
}
