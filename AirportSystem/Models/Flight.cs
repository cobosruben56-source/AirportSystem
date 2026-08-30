using System;
using System.Collections.Generic;
using AirportSystem.Enums;

namespace AirportSystem.Models
{
    public class Flight
    {
        public string FlightNumber { get; set; } = string.Empty;
        public string Airline { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public int TotalSeats { get; set; }
        public decimal BasePrice { get; set; }
        public string Gate { get; set; } = string.Empty;
        public int Terminal { get; set; }
        public FlightStatus Status { get; set; }

        // Seat map: key = seat number (e.g. "1A"), value = true if booked
        public Dictionary<string, bool> SeatMap { get; set; } = new();

        public int AvailableSeats
        {
            get
            {
                int booked = 0;
                foreach (var seat in SeatMap.Values)
                    if (seat) booked++;
                return TotalSeats - booked;
            }
        }

        // Helper to initialize seat map (e.g. rows 1-20, columns A-F)
        public void InitializeSeats(int rows, string columns)
        {
            SeatMap.Clear();
            for (int r = 1; r <= rows; r++)
            {
                foreach (char c in columns)
                {
                    string seat = r + c.ToString();
                    SeatMap[seat] = false; // false = available
                }
            }
            TotalSeats = SeatMap.Count;
        }

        // Get list of available seat numbers
        public List<string> GetAvailableSeats()
        {
            var available = new List<string>();
            foreach (var kvp in SeatMap)
                if (!kvp.Value)
                    available.Add(kvp.Key);
            return available;
        }

        // Book a specific seat
        public bool BookSeat(string seatNumber)
        {
            if (SeatMap.ContainsKey(seatNumber) && !SeatMap[seatNumber])
            {
                SeatMap[seatNumber] = true;
                return true;
            }
            return false;
        }

        // Free a seat (for cancellations)
        public void FreeSeat(string seatNumber)
        {
            if (SeatMap.ContainsKey(seatNumber))
                SeatMap[seatNumber] = false;
        }
    }
}