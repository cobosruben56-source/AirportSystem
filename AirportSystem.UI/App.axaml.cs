using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AirportSystem.UI.ViewModels;
using AirportSystem.UI.Views;

namespace AirportSystem.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        LocalizationService.Instance.Apply();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // La ventana de login es la principal hasta que las credenciales
            // sean correctas; entonces se sustituye por la ventana principal.
            var login = new LoginWindow();
            login.Authenticated += () =>
            {
                var viewModel = new MainViewModel();
                var mainWindow = new MainWindow
                {
                    DataContext = viewModel
                };

                // El ViewModel pide confirmación desde la ventana, sin depender de Avalonia.
                viewModel.Confirm = message => mainWindow.ShowConfirmAsync(message);

                desktop.MainWindow = mainWindow;
                mainWindow.Show();
                login.Hide();
            };

            desktop.MainWindow = login;
        }

        base.OnFrameworkInitializationCompleted();
    }
}