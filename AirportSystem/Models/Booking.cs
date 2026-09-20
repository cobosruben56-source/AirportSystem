using System;

namespace AirportSystem.Models
{
    public class Booking
    {
        public int BookingId { get; set; }
        public string FlightNumber { get; set; } = string.Empty;
        public int PassengerId { get; set; }
        public string SeatNumber { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public bool IsCheckedIn { get; set; }
    }
}