using Avalonia.Controls;

namespace AirportSystem.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnFlightsSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ScrollSelectedIntoView(sender);
    }

    private void OnPassengersSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ScrollSelectedIntoView(sender);
    }

    private static void ScrollSelectedIntoView(object? sender)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is not null)
        {
            // Garantiza que el elemento recién registrado/seleccionado siempre quede visible.
            listBox.ScrollIntoView(listBox.SelectedItem);
        }
    }
}
