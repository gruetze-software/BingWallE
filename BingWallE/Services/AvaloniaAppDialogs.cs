using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using BingWallE.Views;

namespace BingWallE.Services;

public sealed class AvaloniaAppDialogs(Func<Window?> getWindow) : IAppDialogs
{
    public void ApplyTheme(string theme)
    {
        if (Avalonia.Application.Current is { } app)
            app.RequestedThemeVariant = theme switch
            {
                "Light" => Avalonia.Styling.ThemeVariant.Light,
                "Dark" => Avalonia.Styling.ThemeVariant.Dark,
                _ => Avalonia.Styling.ThemeVariant.Default
            };
    }

    public Task OpenFolderAsync(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) throw new System.IO.IOException("Kein Zielordner ausgewählt.");
        System.IO.Directory.CreateDirectory(folder);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = System.IO.Path.GetFullPath(folder), UseShellExecute = true
        });
        return Task.CompletedTask;
    }

    public async Task<string?> SelectFolderAsync()
    {
        if (getWindow()?.StorageProvider is not { } provider) return null;
        var folders = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Zielordner auswählen", AllowMultiple = false
        });
        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }

    public async Task ShowInfoAsync(string title)
    {
        if (getWindow() is { } owner) await new AboutWindow().ShowDialog(owner);
    }
}
