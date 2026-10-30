using Avalonia.Controls;
using System.Linq;
using System.ComponentModel;
using Avalonia.Threading;
using Avalonia.Interactivity;
using BingWallE.ViewModels;

namespace BingWallE.Views;

public partial class MainWindow : Window
{
    private PreviewWindow? _previewWindow;
    private MainWindowViewModel? _previewViewModel;

    public MainWindow()
    {
        InitializeComponent();
        _ = new WindowPlacement(this, new(),
            () => (DataContext as MainWindowViewModel)?.WindowSettings ?? new(),
            state => (DataContext as MainWindowViewModel)?.SaveWindowState(state));
        DataContextChanged += (_, _) =>
        {
            if (_previewViewModel != null) _previewViewModel.PropertyChanged -= PreviewChanged;
            _previewWindow?.Close();
            _previewWindow = null;
            _previewViewModel = DataContext as MainWindowViewModel;
            if (_previewViewModel != null) _previewViewModel.PropertyChanged += PreviewChanged;
            SyncPreviewWindow();
        };
        Opened += async (_, _) =>
        {
            BingWallE.Services.DesktopPlatform.Report(this);
            if (DataContext is MainWindowViewModel vm)
            {
                await vm.InitializeAsync();
                SyncPreviewWindow();
            }
        };
        WindowFullscreenShortcuts.Attach(this, () =>
        {
            if (DataContext is not MainWindowViewModel { IsPreviewOpen: true } vm) return false;
            vm.ClosePreviewCommand.Execute(null);
            return true;
        });
        Closed += (_, _) =>
        {
            if (_previewViewModel != null) _previewViewModel.PropertyChanged -= PreviewChanged;
            _previewWindow?.Close();
            (DataContext as MainWindowViewModel)?.Shutdown();
        };
    }

    private void PreviewChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.PreviewItem))
            Dispatcher.UIThread.Post(SyncPreviewWindow);
    }

    private void SyncPreviewWindow()
    {
        if (!IsVisible) return;
        if (DataContext is MainWindowViewModel { IsPreviewOpen: true } vm)
        {
            if (_previewWindow != null) return;
            var preview = new PreviewWindow { DataContext = vm };
            _previewWindow = preview;
            preview.Closed += (_, _) =>
            {
                if (ReferenceEquals(_previewWindow, preview)) _previewWindow = null;
            };
            preview.Show(this);
        }
        else
        {
            var preview = _previewWindow;
            _previewWindow = null;
            preview?.Close();
        }
    }

    private async void OpenSettings(object? sender, RoutedEventArgs e) => await ShowSettingsAsync();

    public async System.Threading.Tasks.Task ShowSettingsAsync()
    {
        if (IsEnabled && !OwnedWindows.Any(x => x.IsDialog) && DataContext is MainWindowViewModel { IsIdle: true, IsPreviewOpen: false } vm)
            await new SettingsWindow { DataContext = vm }.ShowDialog(this);
    }

    private void ContextShow(object? sender, RoutedEventArgs e) => OpenPreview(sender, e);
    private void ContextOpenPath(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { DataContext: ImageItemViewModel item } && DataContext is MainWindowViewModel vm)
            vm.OpenImagePathCommand.Execute(item);
    }
    private void ContextIgnore(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { DataContext: ImageItemViewModel item } && DataContext is MainWindowViewModel vm)
            vm.ToggleIgnoreCommand.Execute(item);
    }
    private void ContextDelete(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { DataContext: ImageItemViewModel item } && DataContext is MainWindowViewModel vm)
            vm.DeleteLocalCommand.Execute(item);
    }

    private void OpenPreview(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: ImageItemViewModel item } && DataContext is MainWindowViewModel vm)
            vm.OpenPreviewCommand.Execute(item);
    }
}
