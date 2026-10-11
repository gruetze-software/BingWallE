using Avalonia.Controls;
using System.Linq;
using Avalonia.Input;
using Avalonia.Interactivity;
using BingWallE.ViewModels;

namespace BingWallE.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && DataContext is MainWindowViewModel vm && vm.IsPreviewOpen)
            { vm.ClosePreviewCommand.Execute(null); e.Handled = true; }
        };
        Closed += (_, _) => (DataContext as MainWindowViewModel)?.Shutdown();
    }

    private async void OpenSettings(object? sender, RoutedEventArgs e) => await ShowSettingsAsync();

    public async System.Threading.Tasks.Task ShowSettingsAsync()
    {
        if (IsEnabled && !OwnedWindows.Any(x => x.IsDialog) && DataContext is MainWindowViewModel { IsIdle: true, IsPreviewOpen: false } vm)
            await new SettingsWindow { DataContext = vm }.ShowDialog(this);
    }

    private void OpenPreview(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: ImageItemViewModel item } && DataContext is MainWindowViewModel vm)
            vm.OpenPreviewCommand.Execute(item);
    }
}
