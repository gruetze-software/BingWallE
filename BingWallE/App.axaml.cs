using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.ComponentModel;
using Avalonia.Controls.ApplicationLifetimes;
using System.Linq;
using System.Net.Http;
using BingWallE.Services;
using Avalonia.Markup.Xaml;
using BingWallE.ViewModels;
using BingWallE.Views;

namespace BingWallE;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            System.Diagnostics.Trace.AutoFlush = true;
            var httpClient = new HttpClient(new HttpTraceHandler(new HttpClientHandler()))
            {
                Timeout = System.TimeSpan.FromSeconds(30)
            };
            System.Diagnostics.Trace.WriteLine($"[{System.DateTimeOffset.Now:O}] BingWallE START. HTTP timeout: 30 seconds per request.");
            var tagger = new LocalImageTagger(LocalImageTagger.DefaultDirectory);
            desktop.Exit += (_, _) => { httpClient.Dispose(); tagger.Dispose(); };
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(new BingWallpaperClient(httpClient),
                    new WallpaperStore(httpClient, tagger), new JsonSettingsStore(JsonSettingsStore.DefaultFilePath),
                    new AvaloniaAppDialogs(() => desktop.MainWindow), tagger, new SpotlightWallpaperClient(httpClient)),
            };
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: MainWindow window }
            && window.DataContext is MainWindowViewModel vm)
        {
            vm.PropertyChanged += MenuStateChanged;
            window.PropertyChanged += WindowMenuStateChanged;
            window.Closed += (_, _) =>
            {
                vm.PropertyChanged -= MenuStateChanged;
                window.PropertyChanged -= WindowMenuStateChanged;
            };
            UpdateMenuState();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private MainWindow? MenuWindow => (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow as MainWindow;

    private void MenuStateChanged(object? sender, PropertyChangedEventArgs e) => UpdateMenuState();
    private void WindowMenuStateChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == InputElement.IsEnabledProperty) UpdateMenuState();
    }
    private void MenuNeedsUpdate(object? sender, EventArgs e) => UpdateMenuState();
    private bool CanUseMenu => MenuWindow is { IsEnabled: true, DataContext: MainWindowViewModel { IsIdle: true, IsPreviewOpen: false } vm } window
        && !window.OwnedWindows.Any(x => x.IsDialog)
        && !vm.SelectFolderCommand.IsRunning && !vm.ShowInfoCommand.IsRunning;
    private void UpdateMenuState()
    {
        // Only our entries: Avalonia appends the standard macOS Services/Hide/Quit actions.
        if (NativeMenu.GetMenu(this) is { } menu)
            foreach (var item in menu.Items.Take(4).OfType<NativeMenuItem>()) item.IsEnabled = CanUseMenu;
    }
    private void AboutMenuClick(object? sender, EventArgs e)
    {
        if (CanUseMenu && MenuWindow?.DataContext is MainWindowViewModel vm) vm.ShowInfoCommand.Execute(null);
    }
    private async void SettingsMenuClick(object? sender, EventArgs e)
    {
        if (CanUseMenu && MenuWindow is { } window) await window.ShowSettingsAsync();
    }
    private void RefreshMenuClick(object? sender, EventArgs e)
    {
        if (CanUseMenu && MenuWindow?.DataContext is MainWindowViewModel vm) vm.StartStopCommand.Execute(null);
    }

}
