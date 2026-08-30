using System;
using System.IO;
using System.Text.Json;
using AirportSystem.Models;
using AirportSystem.Services;

namespace AirportSystem.Utilities
{
    public static class DataStorage
    {
        private static readonly string _filePath = "airport_data.json";

        public static void SaveData(AirportService service)
        {
            var data = new
            {
                Flights = service.GetAllFlights(),
                Passengers = service.GetAllPassengers(),
                Bookings = service.GetAllBookings()
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                IncludeFields = false
            };

            string json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(_filePath, json);
        }

        public static (List<Flight>? flights, List<Passenger>? passengers, List<Booking>? bookings) LoadData()
        {
            if (!File.Exists(_filePath))
                return (null, null, null);

            string json = File.ReadAllText(_filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // Deserialize into an anonymous type
            var data = JsonSerializer.Deserialize<DataContainer>(json, options);
            if (data == null)
                return (null, null, null);

            return (data.Flights, data.Passengers, data.Bookings);
        }

        // Helper class for deserialization
        private class DataContainer
        {
            public List<Flight>? Flights { get; set; }
            public List<Passenger>? Passengers { get; set; }
            public List<Booking>? Bookings { get; set; }
        }
    }
}