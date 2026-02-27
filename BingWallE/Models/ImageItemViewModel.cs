using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BingWallE.ViewModels;

public partial class ImageItemViewModel : ViewModelBase
{
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private string _title;
    [ObservableProperty] private string _date;
    [ObservableProperty] private string _fullImageUrl;
    [ObservableProperty] private Bitmap? _preview;
    public ImageItemViewModel(string fullImageUrl, string title, string date, Bitmap? preview = null)
    {
        FullImageUrl = fullImageUrl;
        Title = title;
        Date = date;
        Preview = preview;
    }

    private async Task LoadPreviewAsync(string path)
    {
        if (!File.Exists(path)) return;

        try
        {
            // Datei asynchron in den Speicher laden
            byte[] imageData = await File.ReadAllBytesAsync(path);
            using var stream = new MemoryStream(imageData);

            // Bitmap-Erzeugung (muss oft zurück auf den UI-Thread)
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Preview = Bitmap.DecodeToWidth(stream, 120);
            });
        }
        catch (Exception ex)
        {
            // Optional: Ein Fehler-Icon setzen, falls das Bild korrupt ist
            System.Diagnostics.Debug.WriteLine($"Fail loading preview: {ex.Message}");
        }
    }
}