using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using BingWallE.Models;
using BingWallE.Services;
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

    private async void CopyKeywordPrompt(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel { CanEditKeywords: true, PreviewItem: { } item } vm) return;
        try
        {
            if (Clipboard == null) throw new System.InvalidOperationException("Clipboard unavailable.");
            await Clipboard.SetValueAsync(DataFormat.Text, KeywordPromptBuilder.Build(item.Image, item.Description, vm.Text));
            if (ReferenceEquals(vm.PreviewItem, item))
                vm.KeywordStatus = vm.Text.Get("KI-Prompt kopiert. In deinen KI-Chat einfügen und die Antwort hier als Keywords übernehmen.");
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex);
            if (ReferenceEquals(vm.PreviewItem, item))
                vm.KeywordStatus = vm.Text.Get("Der KI-Prompt konnte nicht in die Zwischenablage kopiert werden.");
        }
    }
}
