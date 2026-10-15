using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BingWallE.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        Opened += (_, _) => WindowScreenFit.Apply(this, new(Width, Height));
    }
    private void CloseDialog(object? sender, RoutedEventArgs e) => Close();
}
