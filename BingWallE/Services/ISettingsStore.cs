using BingWallE.Models;

namespace BingWallE.Services;

public interface ISettingsStore
{
    AppSettings Load();
    void Save(AppSettings settings);
}
