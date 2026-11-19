using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    private string? _editingFlightNumber;
    private int? _editingPassengerId;
    private int? _editingBookingId;

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

    // Edición (CRUD)
    [ObservableProperty] private bool _isEditingFlight;
    [ObservableProperty] private bool _isEditingPassenger;
    [ObservableProperty] private bool _isEditingBooking;
    [ObservableProperty] private string _flightFormTitle = "Registrar nuevo vuelo";
    [ObservableProperty] private string _flightActionText = "Registrar vuelo";
    [ObservableProperty] private string _passengerFormTitle = "Registrar pasajero";
    [ObservableProperty] private string _passengerActionText = "Registrar pasajero";
    [ObservableProperty] private string _bookingActionText = "Reservar";
    [ObservableProperty] private string _bookingFlightNumber = string.Empty;
    [ObservableProperty] private string _bookingPassengerName = string.Empty;

    /// <summary>
    /// Se asigna desde la ventana principal para pedir confirmación antes de borrar.
    /// Devuelve true si el usuario acepta.
    /// </summary>
    public Func<string, Task<bool>>? Confirm { get; set; }

    public string SeatMapDisplay => BuildSeatMap();

    public bool CanChangeSeatLayout => !IsEditingFlight;

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

    /// <summary>
    /// Recarga los datos si la app de consola (u otra instancia) los ha modificado.
    /// Se llama al volver a la ventana y antes de cada operacion que escribe.
    /// </summary>
    public void SyncFromDisk()
    {
        if (!DataStorage.HasChangedOnDisk())
            return;

        try
        {
            var (flights, passengers, bookings) = DataStorage.LoadData();
            if (flights is null || passengers is null || bookings is null)
                return;

            _service.LoadData(flights, passengers, bookings);

            // Refresh* conserva la seleccion actual y no toca los formularios.
            RefreshFlights();
            RefreshPassengers();
            RefreshBookings();

            SetStatus("Datos actualizados desde la app de consola.");
        }
        catch (Exception ex)
        {
            SetStatus($"No se pudieron recargar los datos: {ex.Message}", error: true);
        }
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

        var seats = SelectedFlight?.GetAvailableSeats() ?? Enumerable.Empty<string>();

        // Al editar una reserva, su asiento actual está ocupado, pero debe seguir
        // siendo una opción válida para poder conservarlo.
        if (IsEditingBooking && SelectedBooking is not null &&
            SelectedFlight?.FlightNumber == SelectedBooking.FlightNumber &&
            !string.IsNullOrWhiteSpace(SelectedBooking.Seat))
        {
            seats = seats.Append(SelectedBooking.Seat).Distinct();
        }

        foreach (var seat in seats)
            AvailableSeats.Add(seat);

        if (!AvailableSeats.Contains(SelectedSeat))
            SelectedSeat = AvailableSeats.FirstOrDefault() ?? string.Empty;

        OnPropertyChanged(nameof(SeatMapDisplay));
    }

    partial void OnSelectedFlightChanged(Flight? value)
    {
        ReloadAvailableSeats();
    }

    partial void OnIsEditingBookingChanged(bool value)
    {
        BookingActionText = value ? "Guardar cambios" : "Reservar";
        ReloadAvailableSeats();
    }

    partial void OnIsEditingFlightChanged(bool value)
    {
        OnPropertyChanged(nameof(CanChangeSeatLayout));
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
        SyncFromDisk();

        if (IsEditingFlight)
        {
            UpdateFlightFromForm();
            return;
        }

        if (!TryBuildFlightFromForm(out var flight, out var error))
        {
            SetStatus(error, error: true);
            return;
        }

        try
        {
            _service.AddFlight(flight!);

            // Quitamos cualquier filtro de búsqueda para que el vuelo nuevo no quede oculto.
            SearchOrigin = string.Empty;
            SearchDestination = string.Empty;
            RefreshFlights();

            // Lo movemos al principio de la lista para que sea imposible no verlo.
            MoveFlightToTop(flight!);
            SelectedFlight = flight;
            ReloadAvailableSeats();

            var saved = TrySave();
            SetStatus(saved
                ? $"Vuelo {flight!.FlightNumber} registrado y guardado. Aparece primero en la lista."
                : $"Vuelo {flight!.FlightNumber} registrado. Aparece primero, pero no se pudo guardar.",
                error: !saved);

            ClearFlightForm();
        }
        catch (Exception ex)
        {
            SetStatus($"No se pudo registrar el vuelo: {ex.Message}", error: true);
        }
    }

    private void UpdateFlightFromForm()
    {
        if (_editingFlightNumber is null)
        {
            SetStatus("No hay ningún vuelo en edición.", error: true);
            CancelFlightEdit();
            return;
        }

        if (!TryBuildFlightFromForm(out var updated, out var error))
        {
            SetStatus(error, error: true);
            return;
        }

        try
        {
            var original = _editingFlightNumber;
            _service.UpdateFlight(original, updated!);

            SearchOrigin = string.Empty;
            SearchDestination = string.Empty;
            RefreshFlights();
            RefreshBookings();
            SelectedFlight = _service.GetAllFlights()
                .FirstOrDefault(f => f.FlightNumber == updated!.FlightNumber) ?? SelectedFlight;
            ReloadAvailableSeats();

            var saved = TrySave();
            SetStatus(saved
                ? $"Vuelo {original} actualizado a {updated!.FlightNumber} y guardado."
                : $"Vuelo {original} actualizado a {updated!.FlightNumber}, pero no se pudo guardar.",
                error: !saved);

            CancelFlightEdit();
        }
        catch (Exception ex)
        {
            SetStatus($"No se pudo actualizar el vuelo: {ex.Message}", error: true);
        }
    }

    private bool TryBuildFlightFromForm(out Flight? flight, out string error)
    {
        flight = null;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(NewFlightNumber) ||
            string.IsNullOrWhiteSpace(NewAirline) ||
            string.IsNullOrWhiteSpace(NewOrigin) ||
            string.IsNullOrWhiteSpace(NewDestination) ||
            string.IsNullOrWhiteSpace(NewGate) ||
            string.IsNullOrWhiteSpace(NewColumns))
        {
            error = "Completa todos los campos obligatorios del vuelo.";
            return false;
        }

        if (!TryParseDateTime(NewDeparture, out var departure) ||
            !TryParseDateTime(NewArrival, out var arrival))
        {
            error = "Fechas inválidas. Usa el formato yyyy-MM-dd HH:mm.";
            return false;
        }

        if (arrival <= departure)
        {
            error = "La llegada debe ser posterior a la salida.";
            return false;
        }

        if (!TryParseDecimal(NewBasePrice, out var basePrice) ||
            !int.TryParse(NewTerminal, out var terminal) ||
            !int.TryParse(NewRows, out var rows) || rows <= 0)
        {
            error = "Precio, terminal o filas inválidos.";
            return false;
        }

        var flightNumber = NewFlightNumber.Trim().ToUpperInvariant();
        if (_service.GetAllFlights().Any(f =>
                string.Equals(f.FlightNumber, flightNumber, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(f.FlightNumber, _editingFlightNumber, StringComparison.OrdinalIgnoreCase) == false))
        {
            error = $"Ya existe un vuelo con el número {flightNumber}. Usa otro número.";
            return false;
        }

        var result = new Flight
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

        if (IsEditingFlight)
        {
            var current = _service.GetAllFlights()
                .FirstOrDefault(f => f.FlightNumber == _editingFlightNumber);
            if (current is null)
            {
                error = "El vuelo que se está editando ya no existe.";
                return false;
            }

            result.Status = current.Status;
            result.SeatMap = new(current.SeatMap);
            result.TotalSeats = current.TotalSeats;

            // Si no hay reservas, se permite cambiar la distribución de asientos.
            if (!_service.GetAllBookings().Any(b => b.FlightNumber == _editingFlightNumber))
                result.InitializeSeats(rows, NewColumns.Trim().ToUpperInvariant());
        }
        else
        {
            result.InitializeSeats(rows, NewColumns.Trim().ToUpperInvariant());
        }

        flight = result;
        return true;
    }

    [RelayCommand]
    private void EditFlight()
    {
        SyncFromDisk();

        if (SelectedFlight is null)
        {
            SetStatus("Selecciona un vuelo para editar.", error: true);
            return;
        }

        var flight = SelectedFlight;
        _editingFlightNumber = flight.FlightNumber;
        IsEditingFlight = true;
        FlightFormTitle = $"Editando vuelo {flight.FlightNumber}";
        FlightActionText = "Guardar cambios";

        NewFlightNumber = flight.FlightNumber;
        NewAirline = flight.Airline;
        NewOrigin = flight.Origin;
        NewDestination = flight.Destination;
        NewDeparture = flight.DepartureTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        NewArrival = flight.ArrivalTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        NewBasePrice = flight.BasePrice.ToString(CultureInfo.InvariantCulture);
        NewGate = flight.Gate;
        NewTerminal = flight.Terminal.ToString(CultureInfo.InvariantCulture);
        NewRows = "10";
        NewColumns = "ABCDEF";

        SetStatus($"Editando vuelo {flight.FlightNumber}. La distribución de asientos no se puede cambiar si hay reservas.");
    }

    [RelayCommand]
    private void CancelFlightEdit()
    {
        _editingFlightNumber = null;
        IsEditingFlight = false;
        FlightFormTitle = "Registrar nuevo vuelo";
        FlightActionText = "Registrar vuelo";
        ClearFlightForm();
        SetStatus("Edición de vuelo cancelada.");
    }

    [RelayCommand]
    private async Task DeleteFlight()
    {
        SyncFromDisk();

        if (SelectedFlight is null)
        {
            SetStatus("Selecciona un vuelo para eliminar.", error: true);
            return;
        }

        var number = SelectedFlight.FlightNumber;
        var bookings = _service.GetAllBookings().Count(b => b.FlightNumber == number);
        var detail = bookings == 0
            ? $"¿Eliminar el vuelo {number}?"
            : $"¿Eliminar el vuelo {number}? También se cancelarán {bookings} reserva(s).";

        if (Confirm is not null && !await Confirm(detail))
        {
            SetStatus("Eliminación cancelada.");
            return;
        }

        if (!_service.DeleteFlight(number, out var removed))
        {
            SetStatus($"No se encontró el vuelo {number}.", error: true);
            return;
        }

        if (IsEditingFlight && _editingFlightNumber == number)
        {
            _editingFlightNumber = null;
            IsEditingFlight = false;
            FlightFormTitle = "Registrar nuevo vuelo";
            FlightActionText = "Registrar vuelo";
            ClearFlightForm();
        }

        RefreshFlights();
        RefreshBookings();

        var saved = TrySave();
        SetStatus(saved
            ? $"Vuelo {number} eliminado. Se cancelaron {removed} reserva(s) y se guardó."
            : $"Vuelo {number} eliminado, pero no se pudo guardar.",
            error: !saved);
    }

    [RelayCommand]
    private void RegisterPassenger()
    {
        SyncFromDisk();

        if (string.IsNullOrWhiteSpace(PassengerFirstName) ||
            string.IsNullOrWhiteSpace(PassengerLastName))
        {
            SetStatus("El nombre y el apellido son obligatorios.", error: true);
            return;
        }

        if (IsEditingPassenger && _editingPassengerId is not null)
        {
            var updated = new Passenger
            {
                Id = _editingPassengerId.Value,
                FirstName = PassengerFirstName.Trim(),
                LastName = PassengerLastName.Trim(),
                Email = PassengerEmail.Trim(),
                Phone = PassengerPhone.Trim()
            };

            try
            {
                _service.UpdatePassenger(updated);
                RefreshPassengers();
                RefreshBookings();
                SelectedPassenger = Passengers.FirstOrDefault(p => p.Id == updated.Id);

                var savedEdit = TrySave();
                SetStatus(savedEdit
                    ? $"Pasajero \"{updated.FullName}\" actualizado y guardado."
                    : $"Pasajero \"{updated.FullName}\" actualizado, pero no se pudo guardar.",
                    error: !savedEdit);

                CancelPassengerEdit();
            }
            catch (Exception ex)
            {
                SetStatus($"No se pudo actualizar el pasajero: {ex.Message}", error: true);
            }

            return;
        }

        var passenger = _service.RegisterPassenger(
            PassengerFirstName.Trim(),
            PassengerLastName.Trim(),
            PassengerEmail.Trim(),
            PassengerPhone.Trim());

        RefreshPassengers();
        MovePassengerToTop(passenger);
        SelectedPassenger = passenger;

        var saved = TrySave();
        SetStatus(saved
            ? $"Pasajero \"{passenger.FullName}\" registrado con ID {passenger.Id}. Aparece primero y se guardó."
            : $"Pasajero \"{passenger.FullName}\" registrado con ID {passenger.Id}. Aparece primero, pero no se pudo guardar.",
            error: !saved);

        ClearPassengerForm();
    }

    [RelayCommand]
    private void EditPassenger()
    {
        SyncFromDisk();

        if (SelectedPassenger is null)
        {
            SetStatus("Selecciona un pasajero para editar.", error: true);
            return;
        }

        var passenger = SelectedPassenger;
        _editingPassengerId = passenger.Id;
        IsEditingPassenger = true;
        PassengerFormTitle = $"Editando pasajero #{passenger.Id}";
        PassengerActionText = "Guardar cambios";

        PassengerFirstName = passenger.FirstName;
        PassengerLastName = passenger.LastName;
        PassengerEmail = passenger.Email;
        PassengerPhone = passenger.Phone;

        SetStatus($"Editando a {passenger.FullName}.");
    }

    [RelayCommand]
    private void CancelPassengerEdit()
    {
        _editingPassengerId = null;
        IsEditingPassenger = false;
        PassengerFormTitle = "Registrar pasajero";
        PassengerActionText = "Registrar pasajero";
        ClearPassengerForm();
        SetStatus("Edición de pasajero cancelada.");
    }

    [RelayCommand]
    private async Task DeletePassenger()
    {
        SyncFromDisk();

        if (SelectedPassenger is null)
        {
            SetStatus("Selecciona un pasajero para eliminar.", error: true);
            return;
        }

        var id = SelectedPassenger.Id;
        var name = SelectedPassenger.FullName;
        var bookings = _service.GetBookingsForPassenger(id).Count;
        var detail = bookings == 0
            ? $"¿Eliminar al pasajero {name} (ID {id})?"
            : $"¿Eliminar al pasajero {name} (ID {id})? También se cancelarán {bookings} reserva(s) y se liberarán sus asientos.";

        if (Confirm is not null && !await Confirm(detail))
        {
            SetStatus("Eliminación cancelada.");
            return;
        }

        if (!_service.DeletePassenger(id, out var removed))
        {
            SetStatus($"No se encontró el pasajero #{id}.", error: true);
            return;
        }

        if (IsEditingPassenger && _editingPassengerId == id)
        {
            _editingPassengerId = null;
            IsEditingPassenger = false;
            PassengerFormTitle = "Registrar pasajero";
            PassengerActionText = "Registrar pasajero";
            ClearPassengerForm();
        }

        RefreshPassengers();
        RefreshFlights();
        RefreshBookings();

        var saved = TrySave();
        SetStatus(saved
            ? $"Pasajero {name} eliminado. Se cancelaron {removed} reserva(s) y se guardó."
            : $"Pasajero {name} eliminado, pero no se pudo guardar.",
            error: !saved);
    }

    [RelayCommand]
    private void BookTicket()
    {
        SyncFromDisk();

        if (IsEditingBooking)
        {
            UpdateBookingFromForm();
            return;
        }

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

        var flightNumber = SelectedFlight.FlightNumber;
        var seat = SelectedSeat;
        var (success, message, bookingId) = _service.BookTicket(
            flightNumber,
            SelectedPassenger.Id,
            seat);

        if (!success)
        {
            SetStatus(message, error: true);
            return;
        }

        RefreshBookings();
        RefreshFlights();
        SelectedBooking = Bookings.FirstOrDefault(b => b.BookingId == bookingId);

        var saved = TrySave();
        SetStatus(saved
            ? $"Reserva confirmada y guardada. ID: {bookingId}, vuelo {flightNumber}, asiento {seat}."
            : $"Reserva confirmada. ID: {bookingId}, vuelo {flightNumber}, asiento {seat}, pero no se pudo guardar.",
            error: !saved);
    }

    [RelayCommand]
    private void CheckIn()
    {
        SyncFromDisk();

        if (SelectedBooking is null)
        {
            SetStatus("Selecciona una reserva para hacer check-in.", error: true);
            return;
        }

        var bookingId = SelectedBooking.BookingId;
        var ok = _service.CheckIn(bookingId);

        RefreshBookings();
        SelectedBooking = Bookings.FirstOrDefault(b => b.BookingId == bookingId);

        if (!ok)
        {
            SetStatus("No se pudo hacer check-in (reserva inexistente, ya registrada o vuelo cancelado).", error: true);
            return;
        }

        var saved = TrySave();
        SetStatus(saved
            ? $"Check-in realizado y guardado para la reserva {bookingId}."
            : $"Check-in realizado para la reserva {bookingId}, pero no se pudo guardar.",
            error: !saved);
    }

    [RelayCommand]
    private void CancelBooking()
    {
        SyncFromDisk();

        if (SelectedBooking is null)
        {
            SetStatus("Selecciona una reserva para cancelar.", error: true);
            return;
        }

        var bookingId = SelectedBooking.BookingId;
        var ok = _service.CancelBooking(bookingId);

        RefreshBookings();
        RefreshFlights();

        if (!ok)
        {
            SetStatus("No se encontró la reserva.", error: true);
            return;
        }

        var saved = TrySave();
        SetStatus(saved
            ? $"Reserva {bookingId} cancelada, asiento liberado y cambios guardados."
            : $"Reserva {bookingId} cancelada, pero no se pudo guardar.",
            error: !saved);
    }

    private void UpdateBookingFromForm()
    {
        if (_editingBookingId is null)
        {
            SetStatus("No hay ninguna reserva en edición.", error: true);
            CancelBookingEdit();
            return;
        }

        if (SelectedFlight is null || SelectedPassenger is null ||
            string.IsNullOrWhiteSpace(SelectedSeat))
        {
            SetStatus("Selecciona vuelo, pasajero y asiento para guardar la reserva.", error: true);
            return;
        }

        var bookingId = _editingBookingId.Value;
        var (success, message) = _service.UpdateBooking(
            bookingId,
            SelectedFlight.FlightNumber,
            SelectedPassenger.Id,
            SelectedSeat);

        if (!success)
        {
            SetStatus(message, error: true);
            return;
        }

        RefreshBookings();
        RefreshFlights();
        SelectedBooking = Bookings.FirstOrDefault(b => b.BookingId == bookingId);

        var saved = TrySave();
        SetStatus(saved
            ? $"Reserva {bookingId} actualizada y guardada."
            : $"Reserva {bookingId} actualizada, pero no se pudo guardar.",
            error: !saved);

        CancelBookingEdit();
    }

    [RelayCommand]
    private void EditBooking()
    {
        SyncFromDisk();

        if (SelectedBooking is null)
        {
            SetStatus("Selecciona una reserva para editar.", error: true);
            return;
        }

        var row = SelectedBooking;
        var flight = _service.GetAllFlights()
            .FirstOrDefault(f => f.FlightNumber == row.FlightNumber);
        var passenger = _service.FindPassenger(
            _service.GetAllBookings().FirstOrDefault(b => b.BookingId == row.BookingId)?.PassengerId ?? -1);

        if (flight is null || passenger is null)
        {
            SetStatus("No se puede editar la reserva: su vuelo o pasajero ya no existe.", error: true);
            return;
        }

        _editingBookingId = row.BookingId;
        IsEditingBooking = true;
        BookingFlightNumber = row.FlightNumber;
        BookingPassengerName = row.Passenger;

        SelectedFlight = flight;
        SelectedPassenger = passenger;
        SelectedSeat = row.Seat;

        SetStatus($"Editando reserva #{row.BookingId}. Puedes cambiar vuelo, pasajero y asiento.");
    }

    [RelayCommand]
    private void CancelBookingEdit()
    {
        _editingBookingId = null;
        IsEditingBooking = false;
        BookingFlightNumber = string.Empty;
        BookingPassengerName = string.Empty;
        ReloadAvailableSeats();
        SetStatus("Edición de reserva cancelada.");
    }

    [RelayCommand]
    private async Task DeleteBooking()
    {
        SyncFromDisk();

        if (SelectedBooking is null)
        {
            SetStatus("Selecciona una reserva para eliminar.", error: true);
            return;
        }

        var row = SelectedBooking;
        var detail = $"¿Eliminar la reserva #{row.BookingId} de {row.Passenger} en el vuelo {row.FlightNumber}? Se liberará el asiento {row.Seat}.";

        if (Confirm is not null && !await Confirm(detail))
        {
            SetStatus("Eliminación cancelada.");
            return;
        }

        if (IsEditingBooking && _editingBookingId == row.BookingId)
            CancelBookingEdit();

        if (!_service.DeleteBooking(row.BookingId))
        {
            SetStatus($"No se encontró la reserva #{row.BookingId}.", error: true);
            return;
        }

        RefreshBookings();
        RefreshFlights();

        var saved = TrySave();
        SetStatus(saved
            ? $"Reserva {row.BookingId} eliminada, asiento liberado y cambios guardados."
            : $"Reserva {row.BookingId} eliminada, pero no se pudo guardar.",
            error: !saved);
    }

    [RelayCommand]
    private void AdvanceTime()
    {
        SyncFromDisk();

        if (!double.TryParse(AdvanceHours, NumberStyles.Float, CultureInfo.CurrentCulture, out var hours) &&
            !double.TryParse(AdvanceHours, NumberStyles.Float, CultureInfo.InvariantCulture, out hours))
        {
            SetStatus("Introduce un número de horas válido.", error: true);
            return;
        }

        _service.AdvanceTime(TimeSpan.FromHours(hours));
        RefreshFlights();
        RefreshBookings();

        var saved = TrySave();
        SetStatus(saved
            ? $"Tiempo avanzado {hours} hora(s). Estados de vuelo actualizados y guardados."
            : $"Tiempo avanzado {hours} hora(s), pero no se pudieron guardar los cambios.",
            error: !saved);
    }

    [RelayCommand]
    private void Save()
    {
        if (TrySave())
            SetStatus($"Datos guardados en {DataStorage.FilePath}.");
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

    private void MovePassengerToTop(Passenger passenger)
    {
        var index = Passengers.IndexOf(passenger);
        if (index > 0)
        {
            Passengers.RemoveAt(index);
            Passengers.Insert(0, passenger);
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
