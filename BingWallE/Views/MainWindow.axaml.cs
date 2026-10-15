using Avalonia.Controls;
using System.Linq;
using Avalonia.Input;
using Avalonia.Interactivity;
using BingWallE.ViewModels;

namespace BingWallE.Views;

public partial class MainWindow : Window
{
    private BingWallE.Models.SavedWindowState _normalSize = new();
    private bool _maximized;
    private bool _restoring;
    private bool _placementApplied;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (!IsVisible && DataContext is MainWindowViewModel vm) RestorePlacement(vm);
        };
        Opened += async (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                if (!_placementApplied) RestorePlacement(vm);
                await vm.InitializeAsync();
            }
        };
        PropertyChanged += (_, e) =>
        {
            if (_restoring || !IsVisible) return;
            if (e.Property == WindowStateProperty && WindowState != WindowState.Minimized)
                _maximized = WindowState == WindowState.Maximized;
            if (e.Property == ClientSizeProperty && WindowState == WindowState.Normal && ClientSize.Width > 0 && ClientSize.Height > 0)
                _normalSize = new(ClientSize.Width, ClientSize.Height);
        };
        Closing += (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm) vm.SaveWindowState(_normalSize with { IsMaximized = _maximized });
        };
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && DataContext is MainWindowViewModel vm && vm.IsPreviewOpen)
            { vm.ClosePreviewCommand.Execute(null); e.Handled = true; }
        };
        Closed += (_, _) => (DataContext as MainWindowViewModel)?.Shutdown();
    }

    private void RestorePlacement(MainWindowViewModel vm)
    {
        _restoring = true;
        _normalSize = WindowScreenFit.Apply(this, vm.WindowSettings);
        _maximized = vm.WindowSettings.IsMaximized;
        WindowState = _maximized ? WindowState.Maximized : WindowState.Normal;
        _placementApplied = true;
        _restoring = false;
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
