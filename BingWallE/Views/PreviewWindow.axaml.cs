using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using BingWallE.Models;
using BingWallE.ViewModels;

namespace BingWallE.Views;

public partial class PreviewWindow : Window
{
    public PreviewWindow()
    {
        InitializeComponent();
        _ = new WindowPlacement(this, new(980, 850),
            () => (DataContext as MainWindowViewModel)?.PreviewWindowSettings ?? new(980, 850),
            state => (DataContext as MainWindowViewModel)?.SavePreviewWindowState(state));
        WindowFullscreenShortcuts.Attach(this, () =>
        {
            if (WindowState == WindowState.FullScreen) return false;
            Close();
            return true;
        });
        Closed += (_, _) =>
        {
            if (DataContext is MainWindowViewModel { IsPreviewOpen: true } vm)
                vm.ClosePreviewCommand.Execute(null);
        };
    }
    private void RemoveKeyword(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: KeywordChip keyword } && DataContext is MainWindowViewModel vm)
            vm.RemovePreviewKeywordCommand.Execute(keyword);
    }

    private void KeywordEntryKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is MainWindowViewModel vm && vm.AddPreviewKeywordCommand.CanExecute(null))
        { vm.AddPreviewKeywordCommand.Execute(null); e.Handled = true; }
    }
}
