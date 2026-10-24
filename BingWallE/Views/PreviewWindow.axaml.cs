using Avalonia.Controls;
using BingWallE.Models;
using BingWallE.ViewModels;

namespace BingWallE.Views;

public partial class PreviewWindow : Window
{
    public PreviewWindow()
    {
        InitializeComponent();
        Opened += (_, _) => WindowScreenFit.Apply(this, new SavedWindowState(Width, Height));
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
}
