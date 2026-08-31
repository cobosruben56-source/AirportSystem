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

            if (!flight.SeatMap.ContainsKey(seatNumber))
                return (false, "Invalid seat number.", -1);

            if (flight.SeatMap[seatNumber])
                return (false, "Seat already booked.", -1);

            var passenger = _passengers.FirstOrDefault(p => p.Id == passengerId);
            if (passenger == null)
                return (false, "Passenger not found. Please register first.", -1);

            flight.BookSeat(seatNumber);

            var booking = new Booking
            {
                BookingId = _nextBookingId++,
                FlightNumber = flightNumber,
                PassengerId = passengerId,
                SeatNumber = seatNumber,
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
                flight.FreeSeat(booking.SeatNumber);

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