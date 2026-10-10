using System.Threading.Tasks;

namespace BingWallE.Services;

public interface IAppDialogs
{
    Task OpenFolderAsync(string folder);
    void ApplyTheme(string theme);
    Task<string?> SelectFolderAsync();
    Task ShowInfoAsync(string title);
}
