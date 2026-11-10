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
        if (sender is ListBox listBox && listBox.SelectedItem is not null)
        {
            // Garantiza que el vuelo recién registrado/ seleccionado siempre quede visible.
            listBox.ScrollIntoView(listBox.SelectedItem);
        }
    }
}
