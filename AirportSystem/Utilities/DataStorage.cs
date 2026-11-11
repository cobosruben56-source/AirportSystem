using System;
using System.IO;
using System.Text.Json;
using AirportSystem.Models;
using AirportSystem.Services;

namespace AirportSystem.Utilities
{
    public static class DataStorage
    {
        // Ruta estable junto al ejecutable: no depende de la carpeta desde la que se lance la app.
        private static readonly string _filePath =
            Path.Combine(AppContext.BaseDirectory, "airport_data.json");

        public static string FilePath => _filePath;

        private static string GetLoadPath()
        {
            if (File.Exists(_filePath))
                return _filePath;

            // Compatibilidad con datos guardados por versiones anteriores en la carpeta actual.
            var legacyPath = Path.Combine(Directory.GetCurrentDirectory(), "airport_data.json");
            return File.Exists(legacyPath) ? legacyPath : _filePath;
        }

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
            var loadPath = GetLoadPath();
            if (!File.Exists(loadPath))
                return (null, null, null);

            string json = File.ReadAllText(loadPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var data = JsonSerializer.Deserialize<DataContainer>(json, options);
            if (data == null)
                return (null, null, null);

            return (data.Flights, data.Passengers, data.Bookings);
        }

        private class DataContainer
        {
            public List<Flight>? Flights { get; set; }
            public List<Passenger>? Passengers { get; set; }
            public List<Booking>? Bookings { get; set; }
        }
    }
}