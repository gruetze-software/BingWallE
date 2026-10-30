using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Media.Imaging;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using BingWallE.Models;
using BingWallE.Services;
using BingWallE.ViewModels;
using BingWallE.Views;
using Xunit;

namespace BingWallE.Tests;

public class ContextMenuLayoutTests
{
    [AvaloniaTheory]
    [InlineData("en", false)]
    [InlineData("de", true)]
    public void FirstAndSubsequentMenusDisplayEveryCommand(string language, bool dark)
    {
        var previousTheme = Application.Current!.RequestedThemeVariant;
        Application.Current.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
        using var http = new HttpClient(new Handler((_, _) => throw new Exception("Unexpected HTTP")));
        var vm = new MainWindowViewModel(new BingWallpaperClient(http), new WallpaperStore(http),
            new TestSettings { Value = new() { Language = language } }, new TestDialogs());
        vm.Wallpapers.Add(new(new Wallpaper("https://example.test/first.jpg", "First", "")));
        vm.Wallpapers.Add(new(new Wallpaper("https://example.test/second.jpg", "Second", "")) { IsSaved = true });
        var window = new MainWindow { DataContext = vm };
        window.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            var cards = window.GetVisualDescendants().OfType<Border>().Where(b => b.ContextMenu != null).ToArray();
            Assert.Equal(2, cards.Length);
            foreach (var card in cards)
            {
                var menu = card.ContextMenu!;
                for (var i = 0; i < 2; i++)
                {
                    menu.Open(card);
                    Dispatcher.UIThread.RunJobs();
                    AvaloniaHeadlessPlatform.ForceRenderTimerTick(3);
                    Assert.Single(menu.GetVisualAncestors().OfType<OverlayPopupHost>());
                    Assert.True(menu.Bounds.Width >= 260);
                    var items = menu.Items.OfType<MenuItem>().ToArray();
                    Assert.Equal(4, items.Length);
                    Assert.All(items, item => Assert.Same(card.DataContext, item.DataContext));
                    Assert.Equal(((ImageItemViewModel)card.DataContext!).IsSaved, items[1].IsEnabled);
                    Assert.Equal(((ImageItemViewModel)card.DataContext!).IsSaved, items[3].IsEnabled);
                    foreach (var item in items)
                    {
                        var header = item.GetVisualDescendants().OfType<ContentPresenter>().Single(p => p.Name == "PART_HeaderPresenter");
                        Assert.True(header.Bounds.Width >= header.DesiredSize.Width - 1,
                            $"Clipped {item.Header}: {header.Bounds} / {header.DesiredSize}; menu {menu.Bounds}");
                        Assert.True(header.Bounds.Height >= 16, $"Clipped command height: {item.Header}");
                    }
                    if (Environment.GetEnvironmentVariable("BINGWALLE_UI_SNAPSHOTS") is { Length: > 0 } directory)
                    {
                        Directory.CreateDirectory(directory);
                        using var frame = window.CaptureRenderedFrame();
                        frame?.Save(Path.Combine(directory, $"context-{language}-{Array.IndexOf(cards, card)}-{i}.png"), PngBitmapEncoderOptions.Default);
                    }
                    menu.Close();
                }
            }
        }
        finally { window.Close(); Application.Current.RequestedThemeVariant = previousTheme; }
    }
}
