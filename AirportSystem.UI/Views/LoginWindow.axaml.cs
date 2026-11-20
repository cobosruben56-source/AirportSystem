using System;
using AirportSystem.Services;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AirportSystem.UI.Views;

public partial class LoginWindow : Window
{
    private readonly AuthService _auth = new();
    private readonly LocalizationService _loc = LocalizationService.Instance;

    /// <summary>Se dispara cuando las credenciales son correctas.</summary>
    public event Action? Authenticated;

    public LoginWindow()
    {
        InitializeComponent();
        Opened += (_, _) => UserBox.Focus();
        _loc.LanguageChanged += OnLanguageChanged;
        Closed += (_, _) => _loc.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLoginClick(object? sender, RoutedEventArgs e) => TryLogin();

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close();

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            TryLogin();
    }

    private void TryLogin()
    {
        if (!_auth.Authenticate(UserBox.Text, PasswordBox.Text))
        {
            ErrorText.Text = _loc.Get("LoginError");
            PasswordBox.Text = string.Empty;
            PasswordBox.Focus();
            return;
        }

        ErrorText.Text = string.Empty;
        Authenticated?.Invoke();
    }

    private void OnLanguageChanged()
    {
        if (!string.IsNullOrEmpty(ErrorText.Text))
            ErrorText.Text = _loc.Get("LoginError");
    }
}
