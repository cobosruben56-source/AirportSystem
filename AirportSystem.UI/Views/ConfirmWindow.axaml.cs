using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AirportSystem.UI.Views;

public partial class ConfirmWindow : Window
{
    public ConfirmWindow()
    {
        InitializeComponent();
    }

    public string Message
    {
        get => MessageText.Text ?? string.Empty;
        set => MessageText.Text = value;
    }

    private void OnAcceptClick(object? sender, RoutedEventArgs e) => Close((object?)true);

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close((object?)false);
}
