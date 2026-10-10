using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
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
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            System.Diagnostics.Trace.AutoFlush = true;
            var httpClient = new HttpClient(new HttpTraceHandler(new HttpClientHandler()))
            {
                Timeout = System.TimeSpan.FromSeconds(30)
            };
            System.Diagnostics.Trace.WriteLine($"[{System.DateTimeOffset.Now:O}] BingWallE START. HTTP timeout: 30 seconds per request.");
            desktop.Exit += (_, _) => httpClient.Dispose();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(new BingWallpaperClient(httpClient),
                    new WallpaperStore(httpClient), new JsonSettingsStore(JsonSettingsStore.DefaultFilePath),
                    new AvaloniaAppDialogs(() => desktop.MainWindow)),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
