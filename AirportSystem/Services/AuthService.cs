using System;

namespace AirportSystem.Services
{
    /// <summary>
    /// Autenticacion de demostracion: un unico usuario con credenciales fijas.
    /// Se usa tanto desde la consola como desde la interfaz grafica.
    /// </summary>
    public sealed class AuthService
    {
        public const string AdminUser = "Admin";
        private const string AdminPassword = "Password";

        public bool Authenticate(string? user, string? password)
        {
            return string.Equals(user?.Trim(), AdminUser, StringComparison.Ordinal)
                   && string.Equals(password, AdminPassword, StringComparison.Ordinal);
        }
    }
}
