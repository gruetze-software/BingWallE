using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using BingWallE.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace BingWallE.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly HttpClient _httpClient = new();
    private CancellationTokenSource? _cts;

    public string AppTitle { get; set; }
    [ObservableProperty] private string? _targetFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "BingImages");
    [ObservableProperty] private ObservableCollection<ImageItemViewModel> _wallpapers = new();
    [ObservableProperty] private string _statusMessage = "Bereit";
    [ObservableProperty] private IBrush _statusColor = Brushes.Gray;
    [ObservableProperty] private string _buttonText = "Bing scannen";
    [ObservableProperty] private bool _isRunning;

    public MainWindowViewModel()
    {
        // Titelleiste aus Assembly-Infos
        var assembly = Assembly.GetExecutingAssembly();
        var title = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "Bing WallE";
        var version = assembly.GetName().Version?.ToString(3) ?? "1.0.0";
        var author = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "Grütze-Software";
        AppTitle = $"{title} v{version} by {author}";

        var settings = AppSettings.Load();
        TargetFolder = settings.WallpaperPath;

        if (!Directory.Exists(TargetFolder)) 
            Directory.CreateDirectory(TargetFolder!);
    }

    [RelayCommand]
    private async Task StartStop()
    {
        if (IsRunning) { _cts?.Cancel(); return; }

        IsRunning = true;
        ButtonText = "Stop...";
        StatusMessage = "Searching images on Bing...";
        StatusColor = Brushes.DeepSkyBlue;
        _cts = new CancellationTokenSource();

        try 
        {
            await ScanBingAsync(_cts.Token);
            StatusMessage = "Scan complete. Select images for download.";
            StatusColor = Brushes.Green;
        }
        catch (OperationCanceledException) { StatusMessage = "canceled."; StatusColor = Brushes.Orange; }
        catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; StatusColor = Brushes.Red; }
        finally 
        { 
            IsRunning = false; 
            ButtonText = "Scan Bing";
            _cts.Dispose(); 
            _cts = null; 
        }
    }

    private async Task ScanBingAsync(CancellationToken ct)
    {
        string market = System.Globalization.CultureInfo.CurrentCulture.Name ?? "en-US";
        Wallpapers.Clear();

        for (int i = 0; i <= 7; i++)
        {
            ct.ThrowIfCancellationRequested();
            var url = $"https://www.bing.com/HPImageArchive.aspx?format=js&idx={i}&n=1&mkt={market}";
            Trace.WriteLine($"Fetching: {url}");
            var response = await _httpClient.GetFromJsonAsync<BingResponse>(url, ct);

            if (response?.Images == null || response.Images.Length == 0) break;

            var img = response.Images[0];
            var fullUrl = "https://www.bing.com" + img.Url;
            
            // --- NEU: Dateiname generieren und Pfad prüfen ---
            // Wir nutzen das gleiche Namensschema wie beim Download
            string fileName = $"{img.Enddate}_{img.Title.Replace(" ", "_")}.jpg";
            string fullPath = Path.Combine(TargetFolder ?? "", fileName);
            
            // Wenn die Datei NICHT existiert, setzen wir den Haken auf 'true'
            bool shouldBeSelected = !File.Exists(fullPath);
            // -------------------------------------------------

            // Vorschau laden (Thumbnail)
            var thumbData = await _httpClient.GetByteArrayAsync(fullUrl + "&pid=hp&w=120&h=80", ct);
            using var ms = new MemoryStream(thumbData);
            var bitmap = new Bitmap(ms);

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
                var newItem = new ImageItemViewModel(fullUrl, img.Title, img.Enddate, bitmap);
                
                // Hier wird das Häkchen gesetzt, wenn das Bild neu ist
                newItem.IsSelected = shouldBeSelected; 
                
                Wallpapers.Add(newItem);
            });
        }
    }

    [RelayCommand]
    private async Task DownloadSelected()
    {
        var selected = Wallpapers.Where(w => w.IsSelected).ToList();
        if (!selected.Any()) return;

        StatusMessage = $"{selected.Count} images loading...";
        foreach (var item in selected)
        {
            var fileName = $"{item.Date}_{item.Title.Replace(" ", "_")}.jpg";
            var path = Path.Combine(TargetFolder!, fileName);
            
            var data = await _httpClient.GetByteArrayAsync(item.FullImageUrl);
            await File.WriteAllBytesAsync(path, data);
            item.IsSelected = false;
        }
        StatusMessage = "Downloads complete!";
    }

    [RelayCommand]
    private async Task SelectFolder()
    {
        var desktop = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        if (desktop?.MainWindow?.StorageProvider is not { } provider) return;

        var folder = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Select folder", AllowMultiple = false });
        if (folder.Count > 0) TargetFolder = folder[0].Path.LocalPath;
    }

    [RelayCommand]
    private async Task ShowInfo()
    {
        var box = MessageBoxManager.GetMessageBoxStandard("Über BingWallE", "BingWallE v1.0\nLäuft auf Linux, Mac & Windows.");
        await box.ShowAsync();
    }

    partial void OnTargetFolderChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            AppSettings settings = AppSettings.Load();
            settings.WallpaperPath = value;
            settings.Save();
        }
    }
}

public record BingResponse(BingImage[] Images);
public record BingImage(string Url, string Title, string Enddate);