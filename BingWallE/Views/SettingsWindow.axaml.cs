using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BingWallE.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow() => InitializeComponent();
    private void CloseDialog(object? sender, RoutedEventArgs e) => Close();
}
