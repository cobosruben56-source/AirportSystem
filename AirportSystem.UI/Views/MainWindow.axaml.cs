using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using AirportSystem.UI.ViewModels;

namespace AirportSystem.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Activated += OnActivated;
    }

    private void OnActivated(object? sender, EventArgs e)
    {
        // Al volver a la ventana, recoge los cambios hechos desde la app de consola.
        if (DataContext is MainViewModel viewModel)
            viewModel.SyncFromDisk();
    }

    /// <summary>Muestra el diálogo de confirmación para borrados.</summary>
    public async Task<bool> ShowConfirmAsync(string message)
    {
        var dialog = new ConfirmWindow
        {
            Message = message
        };

        return await dialog.ShowDialog<bool>(this);
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
