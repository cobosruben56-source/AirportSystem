using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using AirportSystem.Models;
using AirportSystem.Services;
using AirportSystem.Utilities;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AirportSystem.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly AirportService _service = new();

    public ObservableCollection<Flight> Flights { get; } = new();
    public ObservableCollection<Passenger> Passengers { get; } = new();
    public ObservableCollection<BookingRow> Bookings { get; } = new();
    public ObservableCollection<string> AvailableSeats { get; } = new();

    [ObservableProperty] private Flight? _selectedFlight;
    [ObservableProperty] private Passenger? _selectedPassenger;
    [ObservableProperty] private BookingRow? _selectedBooking;
    [ObservableProperty] private string _selectedSeat = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private IBrush _statusBrush = new SolidColorBrush(Color.Parse("#16A34A"));

    // Búsqueda de vuelos
    [ObservableProperty] private string _searchOrigin = string.Empty;
    [ObservableProperty] private string _searchDestination = string.Empty;

    // Formulario de vuelo
    [ObservableProperty] private string _newFlightNumber = string.Empty;
    [ObservableProperty] private string _newAirline = string.Empty;
    [ObservableProperty] private string _newOrigin = string.Empty;
    [ObservableProperty] private string _newDestination = string.Empty;
    [ObservableProperty] private string _newDeparture = string.Empty;
    [ObservableProperty] private string _newArrival = string.Empty;
    [ObservableProperty] private string _newBasePrice = string.Empty;
    [ObservableProperty] private string _newGate = string.Empty;
    [ObservableProperty] private string _newTerminal = "1";
    [ObservableProperty] private string _newRows = "10";
    [ObservableProperty] private string _newColumns = "ABCDEF";

    // Formulario de pasajero
    [ObservableProperty] private string _passengerFirstName = string.Empty;
    [ObservableProperty] private string _passengerLastName = string.Empty;
    [ObservableProperty] private string _passengerEmail = string.Empty;
    [ObservableProperty] private string _passengerPhone = string.Empty;

    // Simulación
    [ObservableProperty] private string _advanceHours = "1";

    public string SeatMapDisplay => BuildSeatMap();

    public MainViewModel()
    {
        LoadInitialData();
    }

    private void SetStatus(string message, bool error = false)
    {
        StatusMessage = message;
        StatusBrush = new SolidColorBrush(Color.Parse(error ? "#DC2626" : "#16A34A"));
    }

    private void LoadInitialData()
    {
        var loaded = false;
        try
        {
            var (flights, passengers, bookings) = DataStorage.LoadData();
            if (flights is not null && passengers is not null && bookings is not null)
            {
                _service.LoadData(flights, passengers, bookings);
                SetStatus($"Datos guardados cargados: {flights.Count} vuelos, {passengers.Count} pasajeros, {bookings.Count} reservas.");
                loaded = true;
            }
        }
        catch (Exception ex)
        {
            SetStatus($"No se pudieron cargar los datos guardados: {ex.Message}", error: true);
        }

        if (!loaded)
        {
            _service.SeedData();
            SetStatus("Datos de ejemplo cargados. ¡Bienvenido al sistema aeroportuario!");
        }

        RefreshFlights();
        RefreshPassengers();
        RefreshBookings();
    }

    private void RefreshFlights()
    {
        var selectedNumber = SelectedFlight?.FlightNumber;

        Flights.Clear();
        foreach (var flight in _service.GetAllFlights())
            Flights.Add(flight);

        SelectedFlight = Flights.FirstOrDefault(f => f.FlightNumber == selectedNumber)
                         ?? Flights.FirstOrDefault();
        ReloadAvailableSeats();
    }

    private void RefreshPassengers()
    {
        var selectedId = SelectedPassenger?.Id;

        Passengers.Clear();
        foreach (var passenger in _service.GetAllPassengers())
            Passengers.Add(passenger);

        SelectedPassenger = Passengers.FirstOrDefault(p => p.Id == selectedId)
                            ?? Passengers.FirstOrDefault();
    }

    private void RefreshBookings()
    {
        var selectedId = SelectedBooking?.BookingId;

        Bookings.Clear();
        foreach (var booking in _service.GetAllBookings())
        {
            var passenger = _service.FindPassenger(booking.PassengerId);
            Bookings.Add(new BookingRow(
                booking.BookingId,
                booking.FlightNumber,
                passenger?.FullName ?? $"Pasajero #{booking.PassengerId}",
                booking.SeatNumber,
                booking.IsCheckedIn ? "Checked-in" : "Pendiente"));
        }

        SelectedBooking = Bookings.FirstOrDefault(b => b.BookingId == selectedId)
                          ?? Bookings.FirstOrDefault();
    }

    private void ReloadAvailableSeats()
    {
        AvailableSeats.Clear();
        if (SelectedFlight is not null)
        {
            foreach (var seat in SelectedFlight.GetAvailableSeats())
                AvailableSeats.Add(seat);
        }

        SelectedSeat = AvailableSeats.FirstOrDefault() ?? string.Empty;
        OnPropertyChanged(nameof(SeatMapDisplay));
    }

    partial void OnSelectedFlightChanged(Flight? value)
    {
        ReloadAvailableSeats();
    }

    [RelayCommand]
    private void SearchFlights()
    {
        var results = _service.SearchFlights(SearchOrigin, SearchDestination, null);

        Flights.Clear();
        foreach (var flight in results)
            Flights.Add(flight);

        SelectedFlight = Flights.FirstOrDefault();
        ReloadAvailableSeats();
        SetStatus($"{Flights.Count} vuelo(s) encontrado(s).");
    }

    [RelayCommand]
    private void ShowAllFlights()
    {
        SearchOrigin = string.Empty;
        SearchDestination = string.Empty;
        RefreshFlights();
        SetStatus($"{Flights.Count} vuelo(s) en total.");
    }

    [RelayCommand]
    private void RegisterFlight()
    {
        if (string.IsNullOrWhiteSpace(NewFlightNumber) ||
            string.IsNullOrWhiteSpace(NewAirline) ||
            string.IsNullOrWhiteSpace(NewOrigin) ||
            string.IsNullOrWhiteSpace(NewDestination) ||
            string.IsNullOrWhiteSpace(NewGate) ||
            string.IsNullOrWhiteSpace(NewColumns))
        {
            SetStatus("Completa todos los campos obligatorios del vuelo.", error: true);
            return;
        }

        if (!TryParseDateTime(NewDeparture, out var departure) ||
            !TryParseDateTime(NewArrival, out var arrival))
        {
            SetStatus("Fechas inválidas. Usa el formato yyyy-MM-dd HH:mm.", error: true);
            return;
        }

        if (arrival <= departure)
        {
            SetStatus("La llegada debe ser posterior a la salida.", error: true);
            return;
        }

        if (!TryParseDecimal(NewBasePrice, out var basePrice) ||
            !int.TryParse(NewTerminal, out var terminal) ||
            !int.TryParse(NewRows, out var rows) || rows <= 0)
        {
            SetStatus("Precio, terminal o filas inválidos.", error: true);
            return;
        }

        var flightNumber = NewFlightNumber.Trim().ToUpperInvariant();
        if (_service.GetAllFlights().Any(f =>
                string.Equals(f.FlightNumber, flightNumber, StringComparison.OrdinalIgnoreCase)))
        {
            SetStatus($"Ya existe un vuelo con el número {flightNumber}. Usa otro número.", error: true);
            return;
        }

        var flight = new Flight
        {
            FlightNumber = flightNumber,
            Airline = NewAirline.Trim(),
            Origin = NewOrigin.Trim().ToUpperInvariant(),
            Destination = NewDestination.Trim().ToUpperInvariant(),
            DepartureTime = departure,
            ArrivalTime = arrival,
            BasePrice = basePrice,
            Gate = NewGate.Trim().ToUpperInvariant(),
            Terminal = terminal
        };
        flight.InitializeSeats(rows, NewColumns.Trim().ToUpperInvariant());

        try
        {
            _service.AddFlight(flight);

            // Quitamos cualquier filtro de búsqueda para que el vuelo nuevo no quede oculto.
            SearchOrigin = string.Empty;
            SearchDestination = string.Empty;
            RefreshFlights();

            // Lo movemos al principio de la lista para que sea imposible no verlo.
            MoveFlightToTop(flight);
            SelectedFlight = flight;
            ReloadAvailableSeats();

            var saved = TrySave();
            SetStatus(saved
                ? $"Vuelo {flight.FlightNumber} registrado y guardado. Aparece primero en la lista."
                : $"Vuelo {flight.FlightNumber} registrado. Aparece primero, pero no se pudo guardar.",
                error: !saved);

            ClearFlightForm();
        }
        catch (Exception ex)
        {
            SetStatus($"No se pudo registrar el vuelo: {ex.Message}", error: true);
        }
    }

    [RelayCommand]
    private void RegisterPassenger()
    {
        if (string.IsNullOrWhiteSpace(PassengerFirstName) ||
            string.IsNullOrWhiteSpace(PassengerLastName))
        {
            SetStatus("El nombre y el apellido son obligatorios.", error: true);
            return;
        }

        var passenger = _service.RegisterPassenger(
            PassengerFirstName.Trim(),
            PassengerLastName.Trim(),
            PassengerEmail.Trim(),
            PassengerPhone.Trim());

        RefreshPassengers();
        SelectedPassenger = Passengers.FirstOrDefault(p => p.Id == passenger.Id);
        SetStatus($"Pasajero registrado con ID {passenger.Id}: {passenger.FullName}.");
        ClearPassengerForm();
    }

    [RelayCommand]
    private void BookTicket()
    {
        if (SelectedFlight is null)
        {
            SetStatus("Selecciona un vuelo.", error: true);
            return;
        }

        if (SelectedPassenger is null)
        {
            SetStatus("Selecciona un pasajero (o regístralo primero).", error: true);
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedSeat))
        {
            SetStatus("Selecciona un asiento disponible.", error: true);
            return;
        }

        var (success, message, bookingId) = _service.BookTicket(
            SelectedFlight.FlightNumber,
            SelectedPassenger.Id,
            SelectedSeat);

        SetStatus(success
            ? $"Reserva confirmada. ID: {bookingId}, vuelo {SelectedFlight.FlightNumber}, asiento {SelectedSeat}."
            : message, error: !success);

        if (success)
        {
            RefreshBookings();
            RefreshFlights();
            SelectedBooking = Bookings.FirstOrDefault(b => b.BookingId == bookingId);
        }
    }

    [RelayCommand]
    private void CheckIn()
    {
        if (SelectedBooking is null)
        {
            SetStatus("Selecciona una reserva para hacer check-in.", error: true);
            return;
        }

        var bookingId = SelectedBooking.BookingId;
        var ok = _service.CheckIn(bookingId);
        SetStatus(ok
            ? $"Check-in realizado para la reserva {bookingId}."
            : "No se pudo hacer check-in (reserva inexistente, ya registrada o vuelo cancelado).",
            error: !ok);

        RefreshBookings();
        SelectedBooking = Bookings.FirstOrDefault(b => b.BookingId == bookingId);
    }

    [RelayCommand]
    private void CancelBooking()
    {
        if (SelectedBooking is null)
        {
            SetStatus("Selecciona una reserva para cancelar.", error: true);
            return;
        }

        var bookingId = SelectedBooking.BookingId;
        var ok = _service.CancelBooking(bookingId);
        SetStatus(ok
            ? $"Reserva {bookingId} cancelada y asiento liberado."
            : "No se encontró la reserva.",
            error: !ok);

        RefreshBookings();
        RefreshFlights();
    }

    [RelayCommand]
    private void AdvanceTime()
    {
        if (!double.TryParse(AdvanceHours, NumberStyles.Float, CultureInfo.CurrentCulture, out var hours) &&
            !double.TryParse(AdvanceHours, NumberStyles.Float, CultureInfo.InvariantCulture, out hours))
        {
            SetStatus("Introduce un número de horas válido.", error: true);
            return;
        }

        _service.AdvanceTime(TimeSpan.FromHours(hours));
        RefreshFlights();
        RefreshBookings();
        SetStatus($"Tiempo avanzado {hours} hora(s). Estados de vuelo actualizados.");
    }

    [RelayCommand]
    private void Save()
    {
        if (TrySave())
            SetStatus("Datos guardados en airport_data.json.");
    }

    private bool TrySave()
    {
        try
        {
            DataStorage.SaveData(_service);
            return true;
        }
        catch (Exception ex)
        {
            SetStatus($"No se pudieron guardar los datos: {ex.Message}", error: true);
            return false;
        }
    }

    private void MoveFlightToTop(Flight flight)
    {
        var index = Flights.IndexOf(flight);
        if (index > 0)
        {
            Flights.RemoveAt(index);
            Flights.Insert(0, flight);
        }
    }

    private void ClearFlightForm()
    {
        NewFlightNumber = string.Empty;
        NewAirline = string.Empty;
        NewOrigin = string.Empty;
        NewDestination = string.Empty;
        NewDeparture = string.Empty;
        NewArrival = string.Empty;
        NewBasePrice = string.Empty;
        NewGate = string.Empty;
        NewTerminal = "1";
        NewRows = "10";
        NewColumns = "ABCDEF";
    }

    private void ClearPassengerForm()
    {
        PassengerFirstName = string.Empty;
        PassengerLastName = string.Empty;
        PassengerEmail = string.Empty;
        PassengerPhone = string.Empty;
    }

    private string BuildSeatMap()
    {
        if (SelectedFlight is null)
            return "Selecciona un vuelo para ver el mapa de asientos.";

        var keys = SelectedFlight.SeatMap.Keys.ToList();
        if (keys.Count == 0)
            return "Este vuelo no tiene asientos configurados.";

        var seats = keys
            .Select(key => new SeatPosition(key, RowOf(key), key[^1]))
            .ToList();

        var columns = seats.Select(s => s.Column).Distinct().OrderBy(c => c).ToList();
        var rows = seats.Select(s => s.Row).Distinct().OrderBy(r => r).ToList();

        var builder = new StringBuilder();
        builder.Append("     ");
        foreach (var column in columns)
            builder.Append(column).Append("  ");
        builder.AppendLine();

        foreach (var row in rows)
        {
            builder.Append(row.ToString().PadLeft(3)).Append("  ");
            foreach (var column in columns)
            {
                var seat = seats.FirstOrDefault(s => s.Row == row && s.Column == column);
                if (seat is null)
                {
                    builder.Append("   ");
                    continue;
                }

                builder.Append(SelectedFlight.SeatMap[seat.Number] ? "X  " : ".  ");
            }

            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    private static int RowOf(string seatNumber)
    {
        var digits = new string(seatNumber.TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(digits, out var row) ? row : 0;
    }

    private static bool TryParseDateTime(string text, out DateTime value)
    {
        return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out value)
               || DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out value);
    }

    private static bool TryParseDecimal(string text, out decimal value)
    {
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
               || decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value);
    }

    private sealed record SeatPosition(string Number, int Row, char Column);
}

public sealed record BookingRow(
    int BookingId,
    string FlightNumber,
    string Passenger,
    string Seat,
    string Status);
