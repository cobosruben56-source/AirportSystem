using System;
using System.Collections.Generic;
using System.Linq;
using AirportSystem.Enums;
using AirportSystem.Models;

namespace AirportSystem.Services
{
    public class AirportService
    {
        private List<Flight> _flights = new();
        private List<Passenger> _passengers = new();
        private List<Booking> _bookings = new();
        private int _nextPassengerId = 1;
        private int _nextBookingId = 1;

        public List<Flight> GetAllFlights() => _flights;
        public List<Passenger> GetAllPassengers() => _passengers;
        public List<Booking> GetAllBookings() => _bookings;

        public void LoadData(
            IEnumerable<Flight> flights,
            IEnumerable<Passenger> passengers,
            IEnumerable<Booking> bookings)
        {
            _flights = flights.ToList();
            _passengers = passengers.ToList();
            _bookings = bookings.ToList();
            _nextPassengerId = _passengers.Count == 0 ? 1 : _passengers.Max(p => p.Id) + 1;
            _nextBookingId = _bookings.Count == 0 ? 1 : _bookings.Max(b => b.BookingId) + 1;

            // Las reservas son la fuente de verdad: si un SeatMap guardado quedo
            // desincronizado, se corrige aqui para no vender dos veces el mismo asiento.
            foreach (var booking in _bookings)
            {
                if (string.IsNullOrWhiteSpace(booking.SeatNumber))
                    continue;

                var flight = _flights.FirstOrDefault(f => f.FlightNumber == booking.FlightNumber);
                var seat = NormalizeSeat(booking.SeatNumber);
                if (flight is not null && flight.SeatMap.ContainsKey(seat))
                    flight.SeatMap[seat] = true;
            }
        }

        private static string NormalizeSeat(string? seat) => seat?.Trim().ToUpperInvariant() ?? string.Empty;

        /// <summary>
        /// Un asiento ocupado aunque el SeatMap diga lo contrario, porque las
        /// reservas son las que mandan.
        /// </summary>
        private bool IsSeatTaken(string flightNumber, string seat, int? exceptBookingId = null)
        {
            return _bookings.Any(b => b.BookingId != exceptBookingId
                                      && b.FlightNumber == flightNumber
                                      && NormalizeSeat(b.SeatNumber) == seat);
        }

        public void AddFlight(Flight flight)
        {
            if (_flights.Any(f => f.FlightNumber == flight.FlightNumber))
                throw new Exception($"Flight {flight.FlightNumber} already exists.");
            _flights.Add(flight);
        }

        public List<Flight> SearchFlights(string? origin, string? destination, DateTime? date)
        {
            var query = _flights.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(origin))
                query = query.Where(f => f.Origin.Contains(origin, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(destination))
                query = query.Where(f => f.Destination.Contains(destination, StringComparison.OrdinalIgnoreCase));
            if (date.HasValue)
                query = query.Where(f => f.DepartureTime.Date == date.Value.Date);
            return query.ToList();
        }

        public Passenger RegisterPassenger(string firstName, string lastName, string email, string phone)
        {
            var passenger = new Passenger
            {
                Id = _nextPassengerId++,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Phone = phone
            };
            _passengers.Add(passenger);
            return passenger;
        }

        public Passenger? FindPassenger(int id) => _passengers.FirstOrDefault(p => p.Id == id);

        public (bool success, string message, int bookingId) BookTicket(
            string flightNumber, int passengerId, string seatNumber)
        {
            var flight = _flights.FirstOrDefault(f => f.FlightNumber == flightNumber);
            if (flight == null)
                return (false, "Flight not found.", -1);

            if (flight.Status == FlightStatus.Cancelled || flight.Status == FlightStatus.Arrived)
                return (false, "Flight is not available for booking.", -1);

            var seat = NormalizeSeat(seatNumber);
            if (!flight.SeatMap.ContainsKey(seat))
                return (false, "Invalid seat number.", -1);

            if (flight.SeatMap[seat] || IsSeatTaken(flightNumber, seat))
                return (false, "Seat already booked.", -1);

            var passenger = _passengers.FirstOrDefault(p => p.Id == passengerId);
            if (passenger == null)
                return (false, "Passenger not found. Please register first.", -1);

            flight.BookSeat(seat);

            var booking = new Booking
            {
                BookingId = _nextBookingId++,
                FlightNumber = flightNumber,
                PassengerId = passengerId,
                SeatNumber = seat,
                BookingDate = DateTime.Now,
                IsCheckedIn = false
            };
            _bookings.Add(booking);

            return (true, $"Booking confirmed! Booking ID: {booking.BookingId}", booking.BookingId);
        }

        public bool CancelBooking(int bookingId)
        {
            var booking = _bookings.FirstOrDefault(b => b.BookingId == bookingId);
            if (booking == null) return false;

            var flight = _flights.FirstOrDefault(f => f.FlightNumber == booking.FlightNumber);
            if (flight != null)
                flight.FreeSeat(NormalizeSeat(booking.SeatNumber));

            _bookings.Remove(booking);
            return true;
        }

        public List<Booking> GetBookingsForPassenger(int passengerId)
        {
            return _bookings.Where(b => b.PassengerId == passengerId).ToList();
        }

        public bool CheckIn(int bookingId)
        {
            var booking = _bookings.FirstOrDefault(b => b.BookingId == bookingId);
            if (booking == null || booking.IsCheckedIn) return false;

            var flight = _flights.FirstOrDefault(f => f.FlightNumber == booking.FlightNumber);
            if (flight == null || flight.Status == FlightStatus.Cancelled)
                return false;

            // Nunca dos reservas del mismo vuelo pueden usar el mismo asiento,
            // aunque el SeatMap guardado estuviera desincronizado.
            if (!string.IsNullOrWhiteSpace(booking.SeatNumber) &&
                IsSeatTaken(booking.FlightNumber, NormalizeSeat(booking.SeatNumber), booking.BookingId))
                return false;

            booking.IsCheckedIn = true;
            return true;
        }

        public void AdvanceTime(TimeSpan delta)
        {
            DateTime now = DateTime.Now.Add(delta);
            foreach (var flight in _flights)
            {
                if (flight.Status == FlightStatus.Cancelled || flight.Status == FlightStatus.Arrived)
                    continue;

                if (now >= flight.DepartureTime.AddMinutes(-30) && now < flight.DepartureTime)
                    flight.Status = FlightStatus.Boarding;
                else if (now >= flight.DepartureTime && now < flight.ArrivalTime)
                    flight.Status = FlightStatus.Departed;
                else if (now >= flight.ArrivalTime)
                    flight.Status = FlightStatus.Arrived;
                else
                    flight.Status = FlightStatus.Scheduled;
            }
        }

        public bool DeleteBooking(int bookingId) => CancelBooking(bookingId);

        public void UpdateFlight(string originalNumber, Flight updated)
        {
            var index = _flights.FindIndex(f => f.FlightNumber == originalNumber);
            if (index < 0)
                throw new Exception($"El vuelo {originalNumber} no existe.");

            ValidateFlight(updated);
            if (!string.Equals(originalNumber, updated.FlightNumber, StringComparison.Ordinal)
                && _flights.Any(f => f.FlightNumber == updated.FlightNumber))
                throw new Exception($"Ya existe un vuelo con el número {updated.FlightNumber}.");

            var current = _flights[index];
            var hasBookings = _bookings.Any(b => b.FlightNumber == originalNumber);
            if (hasBookings)
            {
                // No se puede cambiar la distribución si hay asientos vendidos.
                updated.SeatMap = current.SeatMap;
                updated.TotalSeats = current.TotalSeats;
            }
            else if (updated.SeatMap.Count == 0)
            {
                throw new Exception("El vuelo debe tener asientos configurados.");
            }

            _flights[index] = updated;

            if (!string.Equals(originalNumber, updated.FlightNumber, StringComparison.Ordinal))
            {
                foreach (var booking in _bookings.Where(b => b.FlightNumber == originalNumber))
                    booking.FlightNumber = updated.FlightNumber;
            }
        }

        public bool DeleteFlight(string flightNumber, out int bookingsRemoved)
        {
            var flight = _flights.FirstOrDefault(f => f.FlightNumber == flightNumber);
            bookingsRemoved = 0;
            if (flight is null)
                return false;

            bookingsRemoved = _bookings.RemoveAll(b => b.FlightNumber == flightNumber);
            _flights.Remove(flight);
            return true;
        }

        public void UpdatePassenger(Passenger updated)
        {
            if (string.IsNullOrWhiteSpace(updated.FirstName) || string.IsNullOrWhiteSpace(updated.LastName))
                throw new Exception("El nombre y el apellido son obligatorios.");

            var index = _passengers.FindIndex(p => p.Id == updated.Id);
            if (index < 0)
                throw new Exception($"El pasajero #{updated.Id} no existe.");

            _passengers[index] = updated;
        }

        public bool DeletePassenger(int passengerId, out int bookingsRemoved)
        {
            var passenger = FindPassenger(passengerId);
            bookingsRemoved = 0;
            if (passenger is null)
                return false;

            var bookings = _bookings.Where(b => b.PassengerId == passengerId).ToList();
            foreach (var booking in bookings)
            {
                var flight = _flights.FirstOrDefault(f => f.FlightNumber == booking.FlightNumber);
                flight?.FreeSeat(NormalizeSeat(booking.SeatNumber));
                _bookings.Remove(booking);
            }

            _passengers.Remove(passenger);
            bookingsRemoved = bookings.Count;
            return true;
        }

        public (bool success, string message) UpdateBooking(
            int bookingId, string flightNumber, int passengerId, string seatNumber)
        {
            var booking = _bookings.FirstOrDefault(b => b.BookingId == bookingId);
            if (booking is null)
                return (false, "La reserva no existe.");

            var flight = _flights.FirstOrDefault(f => f.FlightNumber == flightNumber);
            if (flight is null)
                return (false, "El vuelo no existe.");

            if (flight.Status == FlightStatus.Cancelled || flight.Status == FlightStatus.Arrived)
                return (false, "Ese vuelo ya no admite reservas.");

            if (FindPassenger(passengerId) is null)
                return (false, "El pasajero no existe.");

            var seat = NormalizeSeat(seatNumber);
            if (!flight.SeatMap.ContainsKey(seat))
                return (false, "Ese asiento no existe en el vuelo.");

            if (IsSeatTaken(flightNumber, seat, bookingId))
                return (false, "Ese asiento ya está ocupado por otra reserva.");

            // El único caso en el que el asiento puede estar marcado como ocupado
            // es cuando esta misma reserva se mantiene en vuelo y asiento.
            var keepsSameSeat = booking.FlightNumber == flightNumber
                                && NormalizeSeat(booking.SeatNumber) == seat;
            if (flight.SeatMap[seat] && !keepsSameSeat)
                return (false, "Ese asiento ya está ocupado.");

            var previousFlight = _flights.FirstOrDefault(f => f.FlightNumber == booking.FlightNumber);
            var changed = booking.FlightNumber != flightNumber
                          || booking.PassengerId != passengerId
                          || NormalizeSeat(booking.SeatNumber) != seat;

            previousFlight?.FreeSeat(NormalizeSeat(booking.SeatNumber));
            flight.BookSeat(seat);

            booking.FlightNumber = flightNumber;
            booking.PassengerId = passengerId;
            booking.SeatNumber = seat;
            if (changed)
                booking.IsCheckedIn = false;

            return (true, "Reserva actualizada.");
        }

        private static void ValidateFlight(Flight flight)
        {
            if (string.IsNullOrWhiteSpace(flight.FlightNumber))
                throw new Exception("El número de vuelo es obligatorio.");
            if (string.IsNullOrWhiteSpace(flight.Airline))
                throw new Exception("La aerolínea es obligatoria.");
            if (string.IsNullOrWhiteSpace(flight.Origin) || string.IsNullOrWhiteSpace(flight.Destination))
                throw new Exception("El origen y el destino son obligatorios.");
            if (flight.ArrivalTime <= flight.DepartureTime)
                throw new Exception("La llegada debe ser posterior a la salida.");
            if (flight.BasePrice < 0)
                throw new Exception("El precio base no puede ser negativo.");
            if (flight.Terminal <= 0)
                throw new Exception("El terminal debe ser un número positivo.");
        }

        public void SeedData()
        {
            var f1 = new Flight
            {
                FlightNumber = "TK001",
                Airline = "Turkish Airlines",
                Origin = "IST",
                Destination = "JFK",
                DepartureTime = DateTime.Now.AddDays(2).AddHours(10),
                ArrivalTime = DateTime.Now.AddDays(2).AddHours(15),
                BasePrice = 450.00m,
                Gate = "A12",
                Terminal = 1,
                Status = FlightStatus.Scheduled
            };
            f1.InitializeSeats(20, "ABCDEF");
            _flights.Add(f1);

            var f2 = new Flight
            {
                FlightNumber = "BA203",
                Airline = "British Airways",
                Origin = "LHR",
                Destination = "CDG",
                DepartureTime = DateTime.Now.AddDays(1).AddHours(8),
                ArrivalTime = DateTime.Now.AddDays(1).AddHours(9.5),
                BasePrice = 120.00m,
                Gate = "B05",
                Terminal = 2,
                Status = FlightStatus.Scheduled
            };
            f2.InitializeSeats(15, "ABCD"); 
            _flights.Add(f2);

            RegisterPassenger("John", "Doe", "john@example.com", "1234567890");
        }
    }
}