using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AirportSystem.UI;

/// <summary>
/// Textos de la interfaz. El idioma por defecto es ingles y se puede cambiar
/// en caliente desde el login o desde la ventana principal.
/// </summary>
public sealed partial class LocalizationService : ObservableObject
{
    // SettingsPath debe inicializarse antes que Instance: el constructor de
    // Instance llama a LoadSettings(), que necesita la ruta ya calculada.
    public static string SettingsPath { get; } = BuildSettingsPath();

    public static LocalizationService Instance { get; } = new();

    public const string EnglishLabel = "English";
    public const string SpanishLabel = "Español";

    public ObservableCollection<string> Languages { get; } = new() { EnglishLabel, SpanishLabel };

    private string _language = "en";
    private bool _resourcesApplied;

    public string Language => _language;

    public string SelectedLanguage
    {
        get => _language == "es" ? SpanishLabel : EnglishLabel;
        set
        {
            var code = string.Equals(value, SpanishLabel, StringComparison.Ordinal) ? "es" : "en";
            SetLanguage(code);
        }
    }

    public event Action? LanguageChanged;

    private static readonly (string Key, string En, string Es)[] Rows =
    {
        ("AppTitle", "Airport Management System", "Sistema de Gestión Aeroportuaria"),
        ("SaveData", "Save data", "Guardar datos"),
        ("FlightsTab", "Flights", "Vuelos"),
        ("Origin", "Origin", "Origen"),
        ("Destination", "Destination", "Destino"),
        ("Search", "Search", "Buscar"),
        ("ViewAll", "View all", "Ver todos"),
        ("Flight", "Flight", "Vuelo"),
        ("Route", "Route", "Ruta"),
        ("Departure", "Departure", "Salida"),
        ("Status", "Status", "Estado"),
        ("Seats", "Seats", "Asientos"),
        ("GateTerminal", "Gate / Term.", "Puerta / Term."),
        ("SeatMap", "Seat map", "Mapa de asientos"),
        ("SeatMapLegend", "(X = booked, . = available)", "(X = ocupado, . = libre)"),
        ("FlightNumber", "Flight no.", "N° de vuelo"),
        ("Airline", "Airline", "Aerolínea"),
        ("Arrival", "Arrival", "Llegada"),
        ("BasePrice", "Base price", "Precio base"),
        ("Gate", "Gate", "Puerta"),
        ("Terminal", "Terminal", "Terminal"),
        ("SeatRows", "Seat rows", "Filas de asientos"),
        ("Columns", "Columns", "Columnas"),
        ("EditSelected", "Edit selected", "Editar seleccionado"),
        ("CancelEdit", "Cancel edit", "Cancelar edición"),
        ("DeleteSelected", "Delete selected", "Eliminar seleccionado"),
        ("TimeSimulation", "Time simulation", "Simulación de tiempo"),
        ("AdvanceHours", "Advance (hours)", "Avanzar (horas)"),
        ("Advance", "Advance", "Avanzar"),
        ("TimeSimulationHint", "Automatically updates flight statuses.", "Actualiza automáticamente el estado de los vuelos."),
        ("PassengersTab", "Passengers", "Pasajeros"),
        ("RegisteredPassengers", "Registered passengers", "Pasajeros registrados"),
        ("Id", "ID", "ID"),
        ("Name", "Name", "Nombre"),
        ("Email", "Email", "Email"),
        ("Phone", "Phone", "Teléfono"),
        ("LastName", "Last name", "Apellido"),
        ("PassengersSavedHint", "Passengers are saved automatically.", "Los pasajeros se guardan automáticamente."),
        ("BookingsTab", "Bookings and check-in", "Reservas y check-in"),
        ("Passenger", "Passenger", "Pasajero"),
        ("Seat", "Seat", "Asiento"),
        ("CheckIn", "Check-in", "Check-in"),
        ("EditBooking", "Edit booking", "Editar reserva"),
        ("CancelBooking", "Cancel booking", "Cancelar reserva"),
        ("DeleteBooking", "Delete booking", "Eliminar reserva"),
        ("Book", "Book", "Reservar"),
        ("SaveChanges", "Save changes", "Guardar cambios"),
        ("RegisterFlight", "Register flight", "Registrar vuelo"),
        ("RegisterNewFlight", "Register new flight", "Registrar nuevo vuelo"),
        ("RegisterPassenger", "Register passenger", "Registrar pasajero"),
        ("EditFlightTitle", "Editing flight {0}", "Editando vuelo {0}"),
        ("EditPassengerTitle", "Editing passenger #{0}", "Editando pasajero #{0}"),
        ("LoginTitle", "Sign in", "Iniciar sesión"),
        ("LoginPrompt", "Sign in to continue", "Inicia sesión para continuar"),
        ("User", "User", "Usuario"),
        ("Password", "Password", "Contraseña"),
        ("Cancel", "Cancel", "Cancelar"),
        ("SignIn", "Sign in", "Entrar"),
        ("LoginError", "Invalid user or password. Try again.", "Usuario o contraseña incorrectos. Intenta de nuevo."),
        ("ConfirmTitle", "Confirm action", "Confirmar acción"),
        ("Accept", "Accept", "Aceptar"),

        ("LoadedSaved", "Saved data loaded: {0} flights, {1} passengers, {2} bookings.", "Datos guardados cargados: {0} vuelos, {1} pasajeros, {2} reservas."),
        ("LoadError", "Could not load saved data: {0}", "No se pudieron cargar los datos guardados: {0}"),
        ("Seeded", "Sample data loaded. Welcome to the airport system!", "Datos de ejemplo cargados. ¡Bienvenido al sistema aeroportuario!"),
        ("Synced", "Data updated from the console app.", "Datos actualizados desde la app de consola."),
        ("ReloadError", "Could not reload data: {0}", "No se pudieron recargar los datos: {0}"),
        ("PassengerFallback", "Passenger #{0}", "Pasajero #{0}"),
        ("BookingCheckedIn", "Checked-in", "Checked-in"),
        ("BookingPending", "Pending", "Pendiente"),
        ("FlightsFound", "{0} flight(s) found.", "{0} vuelo(s) encontrado(s)."),
        ("FlightsTotal", "{0} flight(s) in total.", "{0} vuelo(s) en total."),
        ("FlightRegisteredSaved", "Flight {0} registered and saved. It appears first in the list.", "Vuelo {0} registrado y guardado. Aparece primero en la lista."),
        ("FlightRegisteredNotSaved", "Flight {0} registered. It appears first, but could not be saved.", "Vuelo {0} registrado. Aparece primero, pero no se pudo guardar."),
        ("RegisterFlightError", "Could not register the flight: {0}", "No se pudo registrar el vuelo: {0}"),
        ("NoFlightEditing", "No flight is being edited.", "No hay ningún vuelo en edición."),
        ("FlightUpdatedSaved", "Flight {0} updated to {1} and saved.", "Vuelo {0} actualizado a {1} y guardado."),
        ("FlightUpdatedNotSaved", "Flight {0} updated to {1}, but could not be saved.", "Vuelo {0} actualizado a {1}, pero no se pudo guardar."),
        ("UpdateFlightError", "Could not update the flight: {0}", "No se pudo actualizar el vuelo: {0}"),
        ("FillFlightFields", "Fill in all required flight fields.", "Completa todos los campos obligatorios del vuelo."),
        ("InvalidDates", "Invalid dates. Use the format yyyy-MM-dd HH:mm.", "Fechas inválidas. Usa el formato yyyy-MM-dd HH:mm."),
        ("ArrivalAfterDeparture", "Arrival must be after departure.", "La llegada debe ser posterior a la salida."),
        ("InvalidPriceTerminalRows", "Invalid price, terminal or rows.", "Precio, terminal o filas inválidos."),
        ("DuplicateFlight", "A flight with number {0} already exists. Use another number.", "Ya existe un vuelo con el número {0}. Usa otro número."),
        ("EditingFlightGone", "The flight being edited no longer exists.", "El vuelo que se está editando ya no existe."),
        ("SelectFlightToEdit", "Select a flight to edit.", "Selecciona un vuelo para editar."),
        ("EditingFlightStatus", "Editing flight {0}. The seat layout cannot be changed if there are bookings.", "Editando vuelo {0}. La distribución de asientos no se puede cambiar si hay reservas."),
        ("FlightEditCancelled", "Flight edit cancelled.", "Edición de vuelo cancelada."),
        ("SelectFlightToDelete", "Select a flight to delete.", "Selecciona un vuelo para eliminar."),
        ("ConfirmDeleteFlight", "Delete flight {0}?", "¿Eliminar el vuelo {0}?"),
        ("ConfirmDeleteFlightBookings", "Delete flight {0}? {1} booking(s) will also be cancelled.", "¿Eliminar el vuelo {0}? También se cancelarán {1} reserva(s)."),
        ("DeleteCancelled", "Deletion cancelled.", "Eliminación cancelada."),
        ("FlightNotFound", "Flight {0} was not found.", "No se encontró el vuelo {0}."),
        ("FlightDeletedSaved", "Flight {0} deleted. {1} booking(s) cancelled and changes saved.", "Vuelo {0} eliminado. Se cancelaron {1} reserva(s) y se guardó."),
        ("FlightDeletedNotSaved", "Flight {0} deleted, but changes could not be saved.", "Vuelo {0} eliminado, pero no se pudo guardar."),
        ("NameLastNameRequired", "First name and last name are required.", "El nombre y el apellido son obligatorios."),
        ("PassengerUpdatedSaved", "Passenger {0} updated and saved.", "Pasajero {0} actualizado y guardado."),
        ("PassengerUpdatedNotSaved", "Passenger {0} updated, but could not be saved.", "Pasajero {0} actualizado, pero no se pudo guardar."),
        ("UpdatePassengerError", "Could not update the passenger: {0}", "No se pudo actualizar el pasajero: {0}"),
        ("PassengerRegisteredSaved", "Passenger {0} registered with ID {1}. It appears first and was saved.", "Pasajero {0} registrado con ID {1}. Aparece primero y se guardó."),
        ("PassengerRegisteredNotSaved", "Passenger {0} registered with ID {1}. It appears first, but could not be saved.", "Pasajero {0} registrado con ID {1}. Aparece primero, pero no se pudo guardar."),
        ("SelectPassengerToEdit", "Select a passenger to edit.", "Selecciona un pasajero para editar."),
        ("EditingPassenger", "Editing {0}.", "Editando a {0}."),
        ("PassengerEditCancelled", "Passenger edit cancelled.", "Edición de pasajero cancelada."),
        ("SelectPassengerToDelete", "Select a passenger to delete.", "Selecciona un pasajero para eliminar."),
        ("ConfirmDeletePassenger", "Delete passenger {0} (ID {1})?", "¿Eliminar al pasajero {0} (ID {1})?"),
        ("ConfirmDeletePassengerBookings", "Delete passenger {0} (ID {1})? {2} booking(s) will be cancelled and their seats released.", "¿Eliminar al pasajero {0} (ID {1})? También se cancelarán {2} reserva(s) y se liberarán sus asientos."),
        ("PassengerNotFound", "Passenger #{0} was not found.", "No se encontró el pasajero #{0}."),
        ("PassengerDeletedSaved", "Passenger {0} deleted. {1} booking(s) cancelled and changes saved.", "Pasajero {0} eliminado. Se cancelaron {1} reserva(s) y se guardó."),
        ("PassengerDeletedNotSaved", "Passenger {0} deleted, but changes could not be saved.", "Pasajero {0} eliminado, pero no se pudo guardar."),
        ("SelectFlight", "Select a flight.", "Selecciona un vuelo."),
        ("SelectPassenger", "Select a passenger (or register one first).", "Selecciona un pasajero (o regístralo primero)."),
        ("SelectSeat", "Select an available seat.", "Selecciona un asiento disponible."),
        ("BookingConfirmedSaved", "Booking confirmed and saved. ID: {0}, flight {1}, seat {2}.", "Reserva confirmada y guardada. ID: {0}, vuelo {1}, asiento {2}."),
        ("BookingConfirmedNotSaved", "Booking confirmed. ID: {0}, flight {1}, seat {2}, but could not be saved.", "Reserva confirmada. ID: {0}, vuelo {1}, asiento {2}, pero no se pudo guardar."),
        ("SelectBookingToCheckIn", "Select a booking to check in.", "Selecciona una reserva para hacer check-in."),
        ("CheckInFailed", "Check-in failed (booking missing, already checked in or flight cancelled).", "No se pudo hacer check-in (reserva inexistente, ya registrada o vuelo cancelado)."),
        ("CheckInSaved", "Check-in completed and saved for booking {0}.", "Check-in realizado y guardado para la reserva {0}."),
        ("CheckInNotSaved", "Check-in completed for booking {0}, but could not be saved.", "Check-in realizado para la reserva {0}, pero no se pudo guardar."),
        ("SelectBookingToCancel", "Select a booking to cancel.", "Selecciona una reserva para cancelar."),
        ("BookingNotFound", "Booking was not found.", "No se encontró la reserva."),
        ("BookingCancelledSaved", "Booking {0} cancelled, seat released and changes saved.", "Reserva {0} cancelada, asiento liberado y cambios guardados."),
        ("BookingCancelledNotSaved", "Booking {0} cancelled, but changes could not be saved.", "Reserva {0} cancelada, pero no se pudo guardar."),
        ("NoBookingEditing", "No booking is being edited.", "No hay ninguna reserva en edición."),
        ("SelectFlightPassengerSeat", "Select flight, passenger and seat to save the booking.", "Selecciona vuelo, pasajero y asiento para guardar la reserva."),
        ("BookingUpdatedSaved", "Booking {0} updated and saved.", "Reserva {0} actualizada y guardada."),
        ("BookingUpdatedNotSaved", "Booking {0} updated, but could not be saved.", "Reserva {0} actualizada, pero no se pudo guardar."),
        ("SelectBookingToEdit", "Select a booking to edit.", "Selecciona una reserva para editar."),
        ("BookingEditUnavailable", "Cannot edit the booking: its flight or passenger no longer exists.", "No se puede editar la reserva: su vuelo o pasajero ya no existe."),
        ("EditingBooking", "Editing booking #{0}. You can change flight, passenger and seat.", "Editando reserva #{0}. Puedes cambiar vuelo, pasajero y asiento."),
        ("BookingEditCancelled", "Booking edit cancelled.", "Edición de reserva cancelada."),
        ("SelectBookingToDelete", "Select a booking to delete.", "Selecciona una reserva para eliminar."),
        ("ConfirmDeleteBooking", "Delete booking #{0} for {1} on flight {2}? Seat {3} will be released.", "¿Eliminar la reserva #{0} de {1} en el vuelo {2}? Se liberará el asiento {3}."),
        ("BookingNotFoundId", "Booking #{0} was not found.", "No se encontró la reserva #{0}."),
        ("BookingDeletedSaved", "Booking {0} deleted, seat released and changes saved.", "Reserva {0} eliminada, asiento liberado y cambios guardados."),
        ("BookingDeletedNotSaved", "Booking {0} deleted, but changes could not be saved.", "Reserva {0} eliminada, pero no se pudo guardar."),
        ("InvalidHours", "Enter a valid number of hours.", "Introduce un número de horas válido."),
        ("TimeAdvancedSaved", "Time advanced {0} hour(s). Flight statuses updated and saved.", "Tiempo avanzado {0} hora(s). Estados de vuelo actualizados y guardados."),
        ("TimeAdvancedNotSaved", "Time advanced {0} hour(s), but changes could not be saved.", "Tiempo avanzado {0} hora(s), pero no se pudieron guardar los cambios."),
        ("DataSavedAt", "Data saved to {0}.", "Datos guardados en {0}."),
        ("SaveError", "Could not save data: {0}", "No se pudieron guardar los datos: {0}"),
        ("SeatMapSelectFlight", "Select a flight to view the seat map.", "Selecciona un vuelo para ver el mapa de asientos."),
        ("SeatMapNoSeats", "This flight has no configured seats.", "Este vuelo no tiene asientos configurados."),
        ("LanguageChanged", "Language changed to {0}.", "Idioma cambiado a {0}."),

        ("ServiceFlightNotFound", "Flight not found.", "Vuelo no encontrado."),
        ("ServiceFlightUnavailable", "Flight is not available for booking.", "Ese vuelo ya no admite reservas."),
        ("ServiceInvalidSeat", "Invalid seat number.", "Ese asiento no existe en el vuelo."),
        ("ServiceSeatBooked", "Seat already booked.", "Ese asiento ya está ocupado."),
        ("ServicePassengerNotFound", "Passenger not found. Please register first.", "El pasajero no existe. Regístralo primero."),
        ("ServiceBookingNotFound", "Booking does not exist.", "La reserva no existe."),
        ("ServiceFlightDoesNotExist", "Flight does not exist.", "El vuelo no existe."),
        ("ServicePassengerDoesNotExist", "Passenger does not exist.", "El pasajero no existe."),
        ("ServiceSeatOccupiedOther", "That seat is already taken by another booking.", "Ese asiento ya está ocupado por otra reserva."),
        ("ServiceBookingUpdated", "Booking updated.", "Reserva actualizada.")
    };

    private static readonly Dictionary<string, string> English = BuildDictionary("en");
    private static readonly Dictionary<string, string> Spanish = BuildDictionary("es");

    private static readonly Dictionary<string, string> ServiceMessageKeys = new(StringComparer.Ordinal)
    {
        ["Flight not found."] = "ServiceFlightNotFound",
        ["Vuelo no encontrado."] = "ServiceFlightNotFound",
        ["Flight is not available for booking."] = "ServiceFlightUnavailable",
        ["Ese vuelo ya no admite reservas."] = "ServiceFlightUnavailable",
        ["Invalid seat number."] = "ServiceInvalidSeat",
        ["Ese asiento no existe en el vuelo."] = "ServiceInvalidSeat",
        ["Seat already booked."] = "ServiceSeatBooked",
        ["Ese asiento ya está ocupado."] = "ServiceSeatBooked",
        ["Passenger not found. Please register first."] = "ServicePassengerNotFound",
        ["El pasajero no existe. Regístralo primero."] = "ServicePassengerNotFound",
        ["La reserva no existe."] = "ServiceBookingNotFound",
        ["Booking does not exist."] = "ServiceBookingNotFound",
        ["El vuelo no existe."] = "ServiceFlightDoesNotExist",
        ["Flight does not exist."] = "ServiceFlightDoesNotExist",
        ["El pasajero no existe."] = "ServicePassengerDoesNotExist",
        ["Passenger does not exist."] = "ServicePassengerDoesNotExist",
        ["Ese asiento ya está ocupado por otra reserva."] = "ServiceSeatOccupiedOther",
        ["That seat is already taken by another booking."] = "ServiceSeatOccupiedOther",
        ["Reserva actualizada."] = "ServiceBookingUpdated",
        ["Booking updated."] = "ServiceBookingUpdated"
    };

    private LocalizationService()
    {
        LoadSettings();
    }

    private static Dictionary<string, string> BuildDictionary(string column)
    {
        return column == "es"
            ? Rows.ToDictionary(row => row.Key, row => row.Es)
            : Rows.ToDictionary(row => row.Key, row => row.En);
    }

    private static string BuildSettingsPath()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(local) && !OperatingSystem.IsWindows())
        {
            var xdg = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrWhiteSpace(home))
                home = Environment.GetEnvironmentVariable("HOME");

            if (!string.IsNullOrWhiteSpace(xdg))
                local = xdg;
            else if (!string.IsNullOrWhiteSpace(home))
                local = Path.Combine(home, ".local", "share");
        }

        if (string.IsNullOrWhiteSpace(local))
            local = AppContext.BaseDirectory;

        return Path.Combine(local, "AirportSystem", "ui_settings.json");
    }

    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return;

            using var document = JsonDocument.Parse(File.ReadAllText(SettingsPath));
            if (document.RootElement.TryGetProperty("Language", out var language))
            {
                var code = language.GetString();
                if (code is "en" or "es")
                    _language = code;
            }
        }
        catch
        {
            // Preferencia defecto: ingles.
        }
    }

    private void SaveSettings()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new { Language = _language }));
        }
        catch
        {
            // No se puede guardar: el cambio sigue activo mientras dure el proceso.
        }
    }

    public string this[string key] => Get(key);

    public string Get(string key, params object[] args)
    {
        var strings = _language == "es" ? Spanish : English;
        var template = strings.TryGetValue(key, out var value) ? value : key;
        return args.Length == 0
            ? template
            : string.Format(CultureInfo.CurrentCulture, template, args);
    }

    public string LocalizeServiceMessage(string message)
    {
        return ServiceMessageKeys.TryGetValue(message, out var key)
            ? Get(key)
            : message;
    }

    public void Apply()
    {
        var app = Application.Current;
        if (app is null)
            return;

        // Reemplazar el diccionario completo hace que Avalonia notifique el
        // cambio a todos los DynamicResource ya cargados en las ventanas.
        var strings = _language == "es" ? Spanish : English;
        var resources = new ResourceDictionary();
        foreach (var pair in strings)
            resources[pair.Key] = pair.Value;

        app.Resources = resources;
        _resourcesApplied = true;
    }

    public void SetLanguage(string code)
    {
        if (code != "en" && code != "es")
            return;

        if (_language == code && _resourcesApplied)
            return;

        _language = code;
        SaveSettings();
        Apply();
        OnPropertyChanged(nameof(SelectedLanguage));
        OnPropertyChanged(nameof(Language));
        LanguageChanged?.Invoke();
    }
}
