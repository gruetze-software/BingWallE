using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using BingWallE.Views;
using BingWallE.Localization;
using BingWallE.ViewModels;

namespace BingWallE.Services;

public sealed class AvaloniaAppDialogs(Func<Window?> getWindow) : IAppDialogs
{
    private UiText Text => (getWindow()?.DataContext as MainWindowViewModel)?.Text ?? new UiText();
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

    public Task ShowFileAsync(string path)
    {
        if (!System.IO.File.Exists(path)) throw new System.IO.FileNotFoundException("Local image is missing.", path);
        var start = new System.Diagnostics.ProcessStartInfo { UseShellExecute = false };
        if (OperatingSystem.IsWindows())
        {
            start.FileName = "explorer.exe";
            // Explorer also treats commas as separators; ArgumentList only quotes whitespace/quotes.
            // Always quote the file path, including names such as "Location,_Country.jpg".
            start.Arguments = "/select,\"" + System.IO.Path.GetFullPath(path) + "\"";
        }
        else if (OperatingSystem.IsMacOS())
        {
            start.FileName = "open"; start.ArgumentList.Add("-R"); start.ArgumentList.Add(System.IO.Path.GetFullPath(path));
        }
        else
        {
            start.FileName = "xdg-open"; start.ArgumentList.Add(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path))!);
        }
        System.Diagnostics.Process.Start(start);
        return Task.CompletedTask;
    }

    public async Task<bool> ConfirmDeleteAsync(string path)
    {
        if (getWindow() is not { } owner) return false;
        return await new DeleteImageWindow(Text, path).ShowDialog<bool>(owner);
    }

    public async Task<string?> SelectFolderAsync()
    {
        if (getWindow()?.StorageProvider is not { } provider) return null;
        var folders = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = Text.Get("Zielordner auswählen"), AllowMultiple = false
        });
        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }

    public async Task ShowInfoAsync(string title)
    {
        if (getWindow() is { } owner) await new AboutWindow(Text).ShowDialog(owner);
    }
}
