using Avalonia;
using Avalonia.Controls;
using Path = Avalonia.Controls.Shapes.Path;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using BingWallE.Controls;
using BingWallE.Services;
using BingWallE.ViewModels;
using BingWallE.Views;
using Xunit;

namespace BingWallE.Tests;

public class ButtonLayoutTests
{
    [AvaloniaTheory]
    [InlineData("de")]
    [InlineData("en")]
    public void LabelsStayCenteredAndOnlyCoffeeHasAnIcon(string language)
    {
        using var http = new HttpClient(new Handler((_, _) => throw new Exception("Unexpected HTTP")));
        var vm = new MainWindowViewModel(new BingWallpaperClient(http), new WallpaperStore(http),
            new TestSettings { Value = new() { Language = language } }, new TestDialogs());
        var gallery = new MainWindow { DataContext = vm, Width = 640, Height = 820 };
        gallery.Show();
        try
        {
            Assert.True(CheckButtons(gallery) >= 9);
            foreach (var dialog in new Window[] { new SettingsWindow { DataContext = vm }, new AboutWindow(vm.Text), new DeleteImageWindow(vm.Text, @"C:\Pictures\Example.jpg") })
            {
                dialog.Show();
                try { Assert.True(CheckButtons(dialog) > 0); }
                finally { dialog.Close(); }
            }
        }
        finally { gallery.Close(); }
    }

    [AvaloniaFact]
    public async Task RefreshButtonChangesToCancelAndBackWithCenteredText()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var http = new HttpClient(new Handler(async (_, ct) =>
        {
            started.TrySetResult(); await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            throw new Exception("Expected cancellation");
        }));
        var vm = new MainWindowViewModel(new BingWallpaperClient(http), new WallpaperStore(http), new TestSettings(), new TestDialogs());
        var window = new MainWindow { DataContext = vm }; window.Show();
        try
        {
            Dispatcher.UIThread.RunJobs();
            var refresh = window.GetVisualDescendants().OfType<Button>().Single(b => ReferenceEquals(b.Command, vm.StartStopCommand));
            Assert.Null(ButtonIcons.GetIcon(refresh));
            var pending = vm.StartStopCommand.ExecuteAsync(null);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Dispatcher.UIThread.RunJobs();
            Assert.Null(ButtonIcons.GetIcon(refresh));
            Assert.Equal(vm.Text.Get("Abbrechen"), refresh.Content);
            await vm.StartStopCommand.ExecuteAsync(null); await pending;
            Dispatcher.UIThread.RunJobs();
            Assert.Null(ButtonIcons.GetIcon(refresh));
            Assert.Equal(vm.Text.Get("Aktualisieren"), refresh.Content);
        }
        finally { window.Close(); }
    }

    private static int CheckButtons(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        var buttons = window.GetVisualDescendants().OfType<Button>()
            .Where(b => b.GetType() == typeof(Button) && b.Content is string && b.IsEffectivelyVisible && b.Bounds.Width > 0).ToArray();
        foreach (var button in buttons)
        {
            Assert.Equal(HorizontalAlignment.Center, button.HorizontalContentAlignment);
            Assert.Equal(VerticalAlignment.Center, button.VerticalContentAlignment);
            Assert.False(string.IsNullOrWhiteSpace(Assert.IsType<string>(button.Content)));
            var caption = (string)button.Content!;
            var label = button.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == caption);
            Avalonia.Visual content;
            if (button.Classes.Contains("coffee"))
            {
                Assert.Same(ButtonIcons.Coffee, ButtonIcons.GetIcon(button));
                var row = button.GetVisualDescendants().OfType<StackPanel>().Single(p => p.Children.OfType<Path>().Any());
                Assert.NotNull(Assert.Single(row.Children.OfType<Path>()).Data);
                content = row;
            }
            else
            {
                Assert.Null(ButtonIcons.GetIcon(button));
                Assert.Empty(button.GetVisualDescendants().OfType<Path>());
                content = label;
            }
            var center = content.TranslatePoint(new Point(content.Bounds.Width / 2, content.Bounds.Height / 2), button)!.Value;
            Assert.InRange(Math.Abs(center.X - button.Bounds.Width / 2), 0, 1);
            Assert.InRange(Math.Abs(center.Y - button.Bounds.Height / 2), 0, 1);
        }
        return buttons.Length;
    }
}
