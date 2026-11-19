using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using AirportSystem.Models;
using AirportSystem.Services;

namespace AirportSystem.Utilities
{
    public static class DataStorage
    {
        private const string FileName = "airport_data.json";
        private const int RetryCount = 5;
        private const int RetryDelayMs = 100;

        private static readonly JsonSerializerOptions _saveOptions = new()
        {
            WriteIndented = true,
            IncludeFields = false
        };

        private static readonly JsonSerializerOptions _loadOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // Ruta compartida por la app de consola y la UI: no depende de la carpeta
        // desde la que se lance cada ejecutable. Se puede sobrescribir con la
        // variable de entorno AIRPORT_DATA_PATH (util para pruebas).
        private static readonly string _filePath = BuildSharedPath();

        // Ultima version del archivo que este proceso ha leido o escrito.
        private static DateTime _lastKnownWriteUtc;

        public static string FilePath => _filePath;

        public static DateTime LastKnownWriteUtc => _lastKnownWriteUtc;

        private static string BuildSharedPath()
        {
            var overridePath = Environment.GetEnvironmentVariable("AIRPORT_DATA_PATH");
            if (!string.IsNullOrWhiteSpace(overridePath))
                return Path.GetFullPath(overridePath);

            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(localAppData))
                localAppData = AppContext.BaseDirectory;

            return Path.Combine(localAppData, "AirportSystem", FileName);
        }

        private static string GetLoadPath()
        {
            if (File.Exists(_filePath))
                return _filePath;

            // Compatibilidad: datos guardados por versiones anteriores junto al
            // ejecutable o en el directorio desde el que se lanzaba la app.
            foreach (var legacyPath in GetLegacyPaths())
            {
                if (File.Exists(legacyPath))
                    return legacyPath;
            }

            return _filePath;
        }

        private static IEnumerable<string> GetLegacyPaths()
        {
            yield return Path.Combine(AppContext.BaseDirectory, FileName);

            var currentDirectory = Directory.GetCurrentDirectory();
            if (!string.Equals(
                    Path.GetFullPath(currentDirectory),
                    Path.GetFullPath(AppContext.BaseDirectory),
                    StringComparison.OrdinalIgnoreCase))
            {
                yield return Path.Combine(currentDirectory, FileName);
            }
        }

        /// <summary>
        /// Indica si otro proceso (la otra app) ha guardado cambios despues de la
        /// ultima lectura o escritura de este proceso.
        /// </summary>
        public static bool HasChangedOnDisk()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return false;

                return File.GetLastWriteTimeUtc(_filePath) > _lastKnownWriteUtc;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        public static void SaveData(AirportService service)
        {
            var data = new
            {
                Flights = service.GetAllFlights(),
                Passengers = service.GetAllPassengers(),
                Bookings = service.GetAllBookings()
            };

            string json = JsonSerializer.Serialize(data, _saveOptions);

            // Escritura atomica: se escribe a un temporal y se reemplaza, para que
            // una lectura simultanea nunca encuentre un JSON a medias.
            WriteAllTextWithRetry(_filePath, json);
            _lastKnownWriteUtc = File.GetLastWriteTimeUtc(_filePath);
        }

        public static (List<Flight>? flights, List<Passenger>? passengers, List<Booking>? bookings) LoadData()
        {
            var loadPath = GetLoadPath();
            if (!File.Exists(loadPath))
                return (null, null, null);

            string json = ReadAllTextWithRetry(loadPath);

            var data = JsonSerializer.Deserialize<DataContainer>(json, _loadOptions);
            if (data == null)
                return (null, null, null);

            _lastKnownWriteUtc = File.GetLastWriteTimeUtc(loadPath);
            return (data.Flights, data.Passengers, data.Bookings);
        }

        private static string ReadAllTextWithRetry(string path)
        {
            Exception? lastError = null;

            for (var attempt = 0; attempt < RetryCount; attempt++)
            {
                try
                {
                    // FileShare.ReadWrite permite leer aunque el otro proceso
                    // este escribiendo en ese instante.
                    using var stream = new FileStream(
                        path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    lastError = ex;
                    Thread.Sleep(RetryDelayMs);
                }
            }

            throw new IOException($"No se pudo leer el archivo de datos '{path}'.", lastError);
        }

        private static void WriteAllTextWithRetry(string path, string content)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var tempPath = $"{path}.{Environment.ProcessId}.tmp";
            Exception? lastError = null;

            for (var attempt = 0; attempt < RetryCount; attempt++)
            {
                try
                {
                    File.WriteAllText(tempPath, content);
                    File.Move(tempPath, path, overwrite: true);
                    return;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    lastError = ex;
                    Thread.Sleep(RetryDelayMs);
                }
            }

            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch
            {
                // Si no se puede limpiar el temporal, el error real es el de escritura.
            }

            throw new IOException($"No se pudo escribir el archivo de datos '{path}'.", lastError);
        }

        private class DataContainer
        {
            public List<Flight>? Flights { get; set; }
            public List<Passenger>? Passengers { get; set; }
            public List<Booking>? Bookings { get; set; }
        }
    }
}
