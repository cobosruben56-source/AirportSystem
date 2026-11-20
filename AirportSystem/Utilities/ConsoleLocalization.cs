using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AirportSystem.Utilities;

/// <summary>
/// Localización de la aplicación de consola. Comparte con la UI el archivo
/// ui_settings.json, por lo que el idioma elegido en una app se aplica a la otra.
/// </summary>
public sealed class ConsoleLocalization
{
    public static ConsoleLocalization Instance { get; } = new();

    public const string English = "en";
    public const string Spanish = "es";

    public string Language { get; private set; } = English;

    public string SelectedLanguage => Language == Spanish ? "Español" : "English";

    private static readonly (string Key, string En, string Es)[] Rows =
    {
        ("Separator", "========================================", "========================================"),
        ("AppTitle", "          AIRPORT MANAGEMENT SYSTEM    ", "      SISTEMA DE GESTIÓN AEROPORTUARIA  "),
        ("SelectLanguageTitle", "          SELECT LANGUAGE              ", "         SELECCIONAR IDIOMA             "),
        ("LanguagePrompt", "1. English\n2. Español\nChoose language [{0}]: ", "1. English\n2. Español\nElige idioma [{0}]: "),
        ("LanguageSelected", "Language: {0}", "Idioma: {0}"),

        ("Menu1", "1. List all flights / register new flight", "1. Listar vuelos / registrar nuevo vuelo"),
        ("Menu2", "2. Search flights", "2. Buscar vuelos"),
        ("Menu3", "3. List all passengers / register new passenger", "3. Listar pasajeros / registrar nuevo pasajero"),
        ("Menu4", "4. Book a ticket", "4. Reservar un billete"),
        ("Menu5", "5. Cancel booking", "5. Cancelar reserva"),
        ("Menu6", "6. View my bookings", "6. Ver mis reservas"),
        ("Menu7", "7. Check-in", "7. Check-in"),
        ("Menu8", "8. Advance time (simulate)", "8. Avanzar tiempo (simulación)"),
        ("Menu9", "9. Show seat map for a flight", "9. Ver mapa de asientos de un vuelo"),
        ("Menu10", "10. Edit flight", "10. Editar vuelo"),
        ("Menu11", "11. Edit passenger", "11. Editar pasajero"),
        ("Menu12", "12. Edit booking", "12. Editar reserva"),
        ("Menu13", "13. Delete flight (cancels its bookings)", "13. Eliminar vuelo (cancela sus reservas)"),
        ("Menu14", "14. Delete passenger (frees seats)", "14. Eliminar pasajero (libera asientos)"),
        ("Menu15", "15. Delete booking (frees seat)", "15. Eliminar reserva (libera asiento)"),
        ("MenuExit", "0. Save & Exit", "0. Guardar y salir"),
        ("DataFile", "\nData file (shared with the UI): {0}", "\nArchivo de datos (compartido con la UI): {0}"),
        ("YourChoice", "\nYour choice: ", "\nTu opción: "),
        ("DataSavedGoodbye", "Data saved. Goodbye!", "Datos guardados. ¡Hasta luego!"),
        ("DataNotSavedExit", "Data could not be saved. Exiting anyway.", "No se pudieron guardar los datos. Se cierra de todos modos."),
        ("InvalidOption", "Invalid option. Press any key to continue.", "Opción inválida. Pulsa cualquier tecla para continuar."),
        ("PressAnyKey", "\nPress any key to continue...", "\nPulsa cualquier tecla para continuar..."),

        ("LoginTitle", "              SIGN IN                  ", "         INICIAR SESIÓN                "),
        ("Attempt", "Attempt {0} of {1}", "Intento {0} de {1}"),
        ("UserPrompt", "User: ", "Usuario: "),
        ("PasswordPrompt", "Password: ", "Contraseña: "),
        ("LoginOk", "\nAccess granted. Welcome, Admin.", "\nAcceso concedido. Bienvenido, Admin."),
        ("LoginError", "\nInvalid user or password.", "\nUsuario o contraseña incorrectos."),
        ("LoginRetry", "Press any key to try again...", "Pulsa una tecla para intentar de nuevo..."),
        ("LoginTooMany", "Too many failed attempts. Closing the application.", "Demasiados intentos fallidos. Cerrando la aplicación."),

        ("LoadedSaved", "Saved data loaded: {0} flights, {1} passengers, {2} bookings.", "Datos guardados cargados: {0} vuelos, {1} pasajeros, {2} reservas."),
        ("LoadError", "Saved data could not be loaded: {0}", "No se pudieron cargar los datos guardados: {0}"),
        ("SeedKeep", "Starting with seed data. The existing file will be kept until you make a change.", "Se cargaron datos de ejemplo. El archivo existente se conservará hasta que hagas un cambio."),
        ("NoSavedData", "No saved data found. Seeding initial data...", "No hay datos guardados. Cargando datos iniciales..."),
        ("Synced", ">> Data was updated by the desktop app and has been reloaded.", ">> Los datos fueron actualizados por la app de escritorio y se recargaron."),
        ("ReloadError", ">> Could not reload data changed by the desktop app: {0}", ">> No se pudieron recargar los datos cambiados por la app de escritorio: {0}"),
        ("SaveError", "❌ Data could not be saved: {0}", "❌ No se pudieron guardar los datos: {0}"),

        ("AllFlights", "=== ALL FLIGHTS ===", "=== TODOS LOS VUELOS ==="),
        ("NoFlights", "No flights available.", "No hay vuelos disponibles."),
        ("FlightRow", "{0} | {1} | {2} -> {3} | Dep: {4:g} | Seats: {5}/{6} | Status: {7} | Gate: {8} | Term: {9}", "{0} | {1} | {2} -> {3} | Salida: {4:g} | Asientos: {5}/{6} | Estado: {7} | Puerta: {8} | Term: {9}"),
        ("SearchRow", "{0} | {1}->{2} | Dep: {3:g} | Seats: {4}/{5} | {6}", "{0} | {1}->{2} | Salida: {3:g} | Asientos: {4}/{5} | {6}"),
        ("RegisterFlightQuestion", "\nDo you want to register a new flight? (y/n) ", "\n¿Quieres registrar un nuevo vuelo? (s/n) "),

        ("RegisterNewFlight", "=== REGISTER NEW FLIGHT ===", "=== REGISTRAR NUEVO VUELO ==="),
        ("FlightNumberPrompt", "Flight number (e.g. AA101): ", "Número de vuelo (ej. AA101): "),
        ("AirlinePrompt", "Airline name: ", "Aerolínea: "),
        ("OriginPrompt", "Origin code (e.g. IST): ", "Código de origen (ej. IST): "),
        ("DestinationPrompt", "Destination code (e.g. JFK): ", "Código de destino (ej. JFK): "),
        ("DeparturePrompt", "Departure time (yyyy-mm-dd hh:mm): ", "Fecha de salida (aaaa-mm-dd hh:mm): "),
        ("ArrivalPrompt", "Arrival time (yyyy-mm-dd hh:mm): ", "Fecha de llegada (aaaa-mm-dd hh:mm): "),
        ("BasePricePrompt", "Base price: ", "Precio base: "),
        ("GatePrompt", "Gate (e.g. A12): ", "Puerta (ej. A12): "),
        ("TerminalPrompt", "Terminal (number): ", "Terminal (número): "),
        ("SeatRowsPrompt", "Number of seat rows: ", "Número de filas de asientos: "),
        ("SeatColumnsPrompt", "Seat columns (e.g. ABCDEF): ", "Columnas de asientos (ej. ABCDEF): "),
        ("InvalidDeparture", "❌ Invalid departure time.", "❌ Fecha de salida inválida."),
        ("InvalidArrival", "❌ Invalid arrival time.", "❌ Fecha de llegada inválida."),
        ("InvalidPrice", "❌ Invalid price.", "❌ Precio inválido."),
        ("InvalidTerminal", "❌ Invalid terminal.", "❌ Terminal inválida."),
        ("InvalidRowCount", "❌ Invalid row count.", "❌ Número de filas inválido."),
        ("AllFieldsRequired", "❌ All required fields must be filled.", "❌ Hay que completar todos los campos obligatorios."),
        ("FlightRegistered", "✅ Flight {0} registered successfully with {1} seats.", "✅ Vuelo {0} registrado correctamente con {1} asientos."),

        ("SearchFlights", "=== SEARCH FLIGHTS ===", "=== BUSCAR VUELOS ==="),
        ("OriginEmptyPrompt", "Origin (or leave empty): ", "Origen (o dejar vacío): "),
        ("DestinationEmptyPrompt", "Destination (or leave empty): ", "Destino (o dejar vacío): "),
        ("NoFlightsFound", "No flights found.", "No se encontraron vuelos."),

        ("AllPassengers", "=== ALL PASSENGERS ===", "=== TODOS LOS PASAJEROS ==="),
        ("NoPassengers", "No passengers registered yet.", "Todavía no hay pasajeros registrados."),
        ("PassengerRow", "#{0} | {1} | Email: {2} | Phone: {3} | Bookings: {4}", "#{0} | {1} | Email: {2} | Teléfono: {3} | Reservas: {4}"),
        ("RegisterPassengerQuestion", "\nDo you want to register a new passenger? (y/n) ", "\n¿Quieres registrar un nuevo pasajero? (s/n) "),
        ("RegisterPassenger", "=== REGISTER PASSENGER ===", "=== REGISTRAR PASAJERO ==="),
        ("FirstNamePrompt", "First name: ", "Nombre: "),
        ("LastNamePrompt", "Last name: ", "Apellido: "),
        ("EmailPrompt", "Email: ", "Email: "),
        ("PhonePrompt", "Phone: ", "Teléfono: "),
        ("NameLastNameRequired", "First and last name are required.", "El nombre y el apellido son obligatorios."),
        ("PassengerRegistered", "Passenger registered with ID: {0}", "Pasajero registrado con ID: {0}"),

        ("BookTicket", "=== BOOK A TICKET ===", "=== RESERVAR UN BILLETE ==="),
        ("FlightNumberRequired", "Flight number required.", "El número de vuelo es obligatorio."),
        ("FlightNumberRequiredPrompt", "Flight number: ", "Número de vuelo: "),
        ("PassengerIdPrompt", "Passenger ID: ", "ID del pasajero: "),
        ("InvalidId", "Invalid ID.", "ID inválido."),
        ("FlightNotFound", "Flight not found.", "Vuelo no encontrado."),
        ("AvailableSeats", "\nAvailable seats for {0}:", "\nAsientos disponibles para {0}:"),
        ("NoSeatsAvailable", "No seats available.", "No hay asientos disponibles."),
        ("SelectSeatPrompt", "Select seat number: ", "Selecciona un asiento: "),
        ("InvalidSeat", "Invalid seat.", "Asiento inválido."),
        ("BookingConfirmed", "✅ {0}", "✅ {0}"),

        ("CancelBooking", "=== CANCEL BOOKING ===", "=== CANCELAR RESERVA ==="),
        ("BookingIdPrompt", "Booking ID: ", "ID de reserva: "),
        ("BookingCancelled", "✅ Booking cancelled.", "✅ Reserva cancelada."),
        ("BookingNotFound", "❌ Booking not found.", "❌ No se encontró la reserva."),

        ("MyBookings", "=== MY BOOKINGS ===", "=== MIS RESERVAS ==="),
        ("NoBookings", "No bookings found.", "No se encontraron reservas."),
        ("CheckedIn", "✅ Checked-in", "✅ Check-in realizado"),
        ("NotCheckedIn", "⏳ Not checked-in", "⏳ Sin check-in"),
        ("BookingRow", "Booking #{0} | Flight: {1} | Seat: {2} | {3}", "Reserva #{0} | Vuelo: {1} | Asiento: {2} | {3}"),

        ("CheckIn", "=== CHECK-IN ===", "=== CHECK-IN ==="),
        ("CheckInOk", "✅ Check-in successful! Here is your boarding pass:", "✅ ¡Check-in realizado! Aquí tienes tu tarjeta de embarque:"),
        ("BoardingPass", "  BOARDING PASS", "  TARJETA DE EMBARQUE"),
        ("PassengerLabel", "  Passenger: {0}", "  Pasajero: {0}"),
        ("FlightLabel", "  Flight:    {0}", "  Vuelo:     {0}"),
        ("SeatLabel", "  Seat:      {0}", "  Asiento:   {0}"),
        ("GateLabel", "  Gate:      {0}", "  Puerta:    {0}"),
        ("TerminalLabel", "  Terminal:  {0}", "  Terminal:  {0}"),
        ("DepartureLabel", "  Departure: {0:g}", "  Salida:    {0:g}"),
        ("Unknown", "Unknown", "Desconocido"),
        ("NotAvailable", "N/A", "N/D"),
        ("CheckInFailed", "❌ Check-in failed. Booking not found or already checked in.", "❌ No se pudo hacer el check-in. La reserva no existe o ya tiene check-in."),

        ("AdvanceTime", "=== ADVANCE TIME (SIMULATION) ===", "=== AVANZAR TIEMPO (SIMULACIÓN) ==="),
        ("HoursPrompt", "Enter hours to advance: ", "Introduce las horas a avanzar: "),
        ("InvalidInput", "Invalid input.", "Entrada inválida."),
        ("TimeAdvanced", "Time advanced by {0} hour(s). Flight statuses updated.", "Tiempo avanzado {0} hora(s). Estados de vuelo actualizados."),

        ("SeatMap", "=== SEAT MAP ===", "=== MAPA DE ASIENTOS ==="),
        ("InvalidFlight", "Invalid flight.", "Vuelo inválido."),
        ("SeatMapTitle", "Seat map for {0} (X = booked, . = available):", "Mapa de asientos de {0} (X = ocupado, . = libre):"),
        ("SeatMapColumns", "     A  B  C  D  E  F", "     A  B  C  D  E  F"),

        ("EditFlight", "=== EDIT FLIGHT ===", "=== EDITAR VUELO ==="),
        ("FlightEditPrompt", "Flight number to edit: ", "Número de vuelo a editar: "),
        ("FlightNotFoundId", "❌ Flight {0} not found.", "❌ No se encontró el vuelo {0}."),
        ("NewFlightNumber", "New flight number", "Nuevo número de vuelo"),
        ("Airline", "Airline", "Aerolínea"),
        ("Origin", "Origin", "Origen"),
        ("Destination", "Destination", "Destino"),
        ("Gate", "Gate", "Puerta"),
        ("SeatColumns", "Seat columns", "Columnas de asientos"),
        ("NumberSeatRows", "Number of seat rows", "Número de filas de asientos"),
        ("SeatLayoutPreserved", "This flight has bookings; its seat layout will be preserved.", "Este vuelo tiene reservas; su distribución de asientos se conservará."),
        ("FlightUpdated", "✅ Flight updated: {0} -> {1}", "✅ Vuelo actualizado: {0} -> {1}"),
        ("UpdatePrefix", "❌ {0}", "❌ {0}"),

        ("EditPassenger", "=== EDIT PASSENGER ===", "=== EDITAR PASAJERO ==="),
        ("PassengerNotFound", "❌ Passenger #{0} not found.", "❌ No se encontró el pasajero #{0}."),
        ("PassengerUpdated", "✅ Passenger #{0} updated: {1}", "✅ Pasajero #{0} actualizado: {1}"),

        ("EditBooking", "=== EDIT BOOKING ===", "=== EDITAR RESERVA ==="),
        ("BookingNotFoundId", "❌ Booking #{0} not found.", "❌ No se encontró la reserva #{0}."),
        ("CurrentBooking", "Current: flight {0}, passenger {1}, seat {2}", "Actual: vuelo {0}, pasajero {1}, asiento {2}"),
        ("NewPassengerId", "New passenger ID", "Nuevo ID de pasajero"),
        ("InvalidPassengerId", "❌ Invalid passenger ID.", "❌ ID de pasajero inválido."),
        ("NewSeat", "New seat", "Nuevo asiento"),
        ("BookingUpdated", "Reserva actualizada.", "Reserva actualizada."),

        ("DeleteFlight", "=== DELETE FLIGHT ===", "=== ELIMINAR VUELO ==="),
        ("ConfirmDeleteFlight", "Delete flight {0}?", "¿Eliminar el vuelo {0}?"),
        ("ConfirmDeleteFlightBookings", "Delete flight {0}? Its {1} booking(s) will also be cancelled.", "¿Eliminar el vuelo {0}? También se cancelarán {1} reserva(s)."),
        ("DeletionCancelled", "Deletion cancelled.", "Eliminación cancelada."),
        ("FlightDeleted", "✅ Flight {0} deleted. {1} booking(s) cancelled.", "✅ Vuelo {0} eliminado. {1} reserva(s) cancelada(s)."),

        ("DeletePassenger", "=== DELETE PASSENGER ===", "=== ELIMINAR PASAJERO ==="),
        ("ConfirmDeletePassenger", "Delete passenger {0} (#{1})?", "¿Eliminar al pasajero {0} (#{1})?"),
        ("ConfirmDeletePassengerBookings", "Delete passenger {0} (#{1})? {2} booking(s) will be cancelled and seats freed.", "¿Eliminar al pasajero {0} (#{1})? Se cancelarán {2} reserva(s) y se liberarán asientos."),
        ("PassengerDeleted", "✅ Passenger #{0} deleted. {1} booking(s) cancelled.", "✅ Pasajero #{0} eliminado. {1} reserva(s) cancelada(s)."),

        ("DeleteBooking", "=== DELETE BOOKING ===", "=== ELIMINAR RESERVA ==="),
        ("ConfirmDeleteBooking", "Delete booking #{0} (flight {1}, seat {2})?", "¿Eliminar la reserva #{0} (vuelo {1}, asiento {2})?"),
        ("BookingDeleted", "✅ Booking #{0} deleted. Seat freed.", "✅ Reserva #{0} eliminada. Asiento liberado."),

        ("ServiceFlightUnavailable", "Flight is not available for booking.", "Ese vuelo ya no admite reservas."),
        ("ServiceInvalidSeatNumber", "Invalid seat number.", "Ese asiento no existe en el vuelo."),
        ("ServiceSeatBooked", "Seat already booked.", "Ese asiento ya está ocupado."),
        ("ServicePassengerNotFound", "Passenger not found. Please register first.", "El pasajero no existe. Regístralo primero."),
        ("ServiceDuplicateFlight", "Flight {0} already exists.", "Ya existe un vuelo con el número {0}."),
        ("ServiceFlightNoSeats", "The flight must have configured seats.", "El vuelo debe tener asientos configurados."),
        ("ServicePassengerDoesNotExist", "Passenger #{0} does not exist.", "El pasajero #{0} no existe."),
        ("ServiceArrivalAfterDeparture", "Arrival must be after departure.", "La llegada debe ser posterior a la salida."),
        ("ServiceNegativePrice", "Base price cannot be negative.", "El precio base no puede ser negativo."),
        ("ServiceInvalidTerminal", "Terminal must be a positive number.", "El terminal debe ser un número positivo."),
        ("ServiceOriginDestinationRequired", "Origin and destination are required.", "El origen y el destino son obligatorios."),
        ("ServiceAirlineRequired", "Airline is required.", "La aerolínea es obligatoria."),
        ("ServiceBookingUpdated", "Booking updated.", "Reserva actualizada.")
    };

    private static readonly Dictionary<string, string> EnglishMap = Build(English);
    private static readonly Dictionary<string, string> SpanishMap = Build(Spanish);

    private static readonly Dictionary<string, string> MessageKeys = new(StringComparer.Ordinal)
    {
        ["Flight not found."] = "FlightNotFound",
        ["Vuelo no encontrado."] = "FlightNotFound",
        ["Flight is not available for booking."] = "ServiceFlightUnavailable",
        ["Ese vuelo ya no admite reservas."] = "ServiceFlightUnavailable",
        ["Invalid seat number."] = "ServiceInvalidSeatNumber",
        ["Ese asiento no existe en el vuelo."] = "ServiceInvalidSeatNumber",
        ["Seat already booked."] = "ServiceSeatBooked",
        ["Ese asiento ya está ocupado."] = "ServiceSeatBooked",
        ["Ese asiento ya está ocupado por otra reserva."] = "ServiceSeatBooked",
        ["Passenger not found. Please register first."] = "ServicePassengerNotFound",
        ["El pasajero no existe. Regístralo primero."] = "ServicePassengerNotFound",
        ["El pasajero no existe."] = "ServicePassengerNotFound",
        ["Passenger does not exist."] = "ServicePassengerNotFound",
        ["La reserva no existe."] = "BookingNotFound",
        ["Booking does not exist."] = "BookingNotFound",
        ["El vuelo no existe."] = "FlightNotFound",
        ["Flight does not exist."] = "FlightNotFound",
        ["El nombre y el apellido son obligatorios."] = "NameLastNameRequired",
        ["El vuelo debe tener asientos configurados."] = "ServiceFlightNoSeats",
        ["La llegada debe ser posterior a la salida."] = "ServiceArrivalAfterDeparture",
        ["El precio base no puede ser negativo."] = "ServiceNegativePrice",
        ["El terminal debe ser un número positivo."] = "ServiceInvalidTerminal",
        ["El origen y el destino son obligatorios."] = "ServiceOriginDestinationRequired",
        ["La aerolínea es obligatoria."] = "ServiceAirlineRequired",
        ["El número de vuelo es obligatorio."] = "FlightNumberRequired",
        ["Reserva actualizada."] = "ServiceBookingUpdated",
        ["Booking updated."] = "ServiceBookingUpdated"
    };

    private ConsoleLocalization()
    {
        LoadSettings();
    }

    private static Dictionary<string, string> Build(string column)
    {
        return column == Spanish
            ? Rows.ToDictionary(row => row.Key, row => row.Es)
            : Rows.ToDictionary(row => row.Key, row => row.En);
    }

    private static string SettingsPath { get; } = BuildSettingsPath();

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
            if (document.RootElement.TryGetProperty("Language", out var language) &&
                language.GetString() is string code && code is English or Spanish)
            {
                Language = code;
            }
        }
        catch
        {
            // Preferencia por defecto: inglés.
        }
    }

    private void SaveSettings()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new { Language }));
        }
        catch
        {
            // El cambio sigue activo mientras dure el proceso.
        }
    }

    public void SetLanguage(string code)
    {
        if (code != English && code != Spanish)
            return;

        Language = code;
        SaveSettings();
    }

    public string Get(string key, params object[] args)
    {
        var source = Language == Spanish ? SpanishMap : EnglishMap;
        var template = source.TryGetValue(key, out var value) ? value : key;
        return args.Length == 0
            ? template
            : string.Format(CultureInfo.CurrentCulture, template, args);
    }

    /// <summary>Traduce mensajes devueltos por AirportService.</summary>
    public string T(string message)
    {
        return MessageKeys.TryGetValue(message, out var key) ? Get(key) : message;
    }
}
