using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AirportSystem.Enums;
using AirportSystem.Models;
using AirportSystem.Services;
using AirportSystem.Utilities;

namespace AirportSystem
{
    class Program
    {
        private static AirportService _service = new();
        private static readonly AuthService _auth = new();

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            if (!Login())
                return;

            LoadSavedData();

            bool exit = false;
            while (!exit)
            {
                ClearScreen();
                ReloadIfChanged();

                Console.WriteLine("========================================");
                Console.WriteLine("          AIRPORT MANAGEMENT SYSTEM    ");
                Console.WriteLine("========================================");
                Console.WriteLine("1. List all flights / register new flight");
                Console.WriteLine("2. Search flights");
                Console.WriteLine("3. List all passengers / register new passenger");
                Console.WriteLine("4. Book a ticket");
                Console.WriteLine("5. Cancel booking");
                Console.WriteLine("6. View my bookings");
                Console.WriteLine("7. Check-in");
                Console.WriteLine("8. Advance time (simulate)");
                Console.WriteLine("9. Show seat map for a flight");
                Console.WriteLine("10. Edit flight");
                Console.WriteLine("11. Edit passenger");
                Console.WriteLine("12. Edit booking");
                Console.WriteLine("13. Delete flight (cancels its bookings)");
                Console.WriteLine("14. Delete passenger (frees seats)");
                Console.WriteLine("15. Delete booking (frees seat)");
                Console.WriteLine("0. Save & Exit");
                Console.WriteLine($"\nData file (shared with the UI): {DataStorage.FilePath}");
                Console.Write("\nYour choice: ");

                string? choice = Console.ReadLine();
                switch (choice)
                {
                    case "1": ListAllFlights(); break;
                    case "2": SearchFlights(); break;
                    case "3": ManagePassengers(); break;
                    case "4": BookTicket(); break;
                    case "5": CancelBooking(); break;
                    case "6": ViewMyBookings(); break;
                    case "7": CheckIn(); break;
                    case "8": AdvanceTime(); break;
                    case "9": ShowSeatMap(); break;
                    case "10": EditFlight(); break;
                    case "11": EditPassenger(); break;
                    case "12": EditBooking(); break;
                    case "13": DeleteFlight(); break;
                    case "14": DeletePassenger(); break;
                    case "15": DeleteBooking(); break;
                    case "0":
                        if (Persist())
                            Console.WriteLine("Data saved. Goodbye!");
                        else
                            Console.WriteLine("Data could not be saved. Exiting anyway.");
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("Invalid option. Press any key to continue.");
                        Pause();
                        break;
                }
            }
        }


        private static bool Login()
        {
            const int maxAttempts = 3;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                ClearScreen();
                Console.WriteLine("========================================");
                Console.WriteLine("              INICIAR SESION");
                Console.WriteLine("========================================");
                Console.WriteLine($"Intento {attempt} de {maxAttempts}");
                Console.Write("Usuario: ");
                var user = Console.ReadLine();
                Console.Write("Password: ");
                var password = ReadPassword();

                if (_auth.Authenticate(user, password))
                {
                    Console.WriteLine("\nAcceso concedido. Bienvenido, Admin.");
                    return true;
                }

                Console.WriteLine("\nUsuario o password incorrectos.");
                if (attempt < maxAttempts)
                {
                    Console.WriteLine("Pulsa una tecla para intentar de nuevo...");
                    Pause();
                }
            }

            Console.WriteLine("Demasiados intentos fallidos. Cerrando la aplicacion.");
            return false;
        }

        private static string ReadPassword()
        {
            // Con entrada redirigida (scripts) no se puede usar ReadKey.
            if (Console.IsInputRedirected)
                return Console.ReadLine() ?? string.Empty;

            var password = new StringBuilder();

            while (true)
            {
                var key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return password.ToString();
                }

                if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password.Length--;
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    password.Append(key.KeyChar);
                    Console.Write("*");
                }
            }
        }

        /// <summary>Con salida redirigida, Clear lanzaría una excepción.</summary>
        private static void ClearScreen()
        {
            if (!Console.IsOutputRedirected)
                Console.Clear();
        }

        /// <summary>
        /// Pausa solo en consola interactiva; con entrada redirigida (pruebas)
        /// ReadKey lanzaría una excepción.
        /// </summary>
        private static void Pause()
        {
            if (!Console.IsInputRedirected)
                Console.ReadKey();
        }

        private static void LoadSavedData()
        {
            try
            {
                var (flights, passengers, bookings) = DataStorage.LoadData();
                if (flights is not null && passengers is not null && bookings is not null)
                {
                    _service.LoadData(flights, passengers, bookings);
                    Console.WriteLine(
                        $"Saved data loaded: {flights.Count} flights, {passengers.Count} passengers, " +
                        $"{bookings.Count} bookings.");
                    return;
                }
            }
            catch (Exception ex)
            {
                // Si el archivo esta corrupto no lo sobrescribimos automaticamente.
                Console.WriteLine($"Saved data could not be loaded: {ex.Message}");
                Console.WriteLine("Starting with seed data. The existing file will be kept until you make a change.");
                _service.SeedData();
                return;
            }

            Console.WriteLine("No saved data found. Seeding initial data...");
            _service.SeedData();
            Persist();
        }

        private static void ReloadIfChanged()
        {
            if (!DataStorage.HasChangedOnDisk())
                return;

            try
            {
                var (flights, passengers, bookings) = DataStorage.LoadData();
                if (flights is null || passengers is null || bookings is null)
                    return;

                _service.LoadData(flights, passengers, bookings);
                Console.WriteLine(">> Data was updated by the desktop app and has been reloaded.");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($">> Could not reload data changed by the desktop app: {ex.Message}");
                Console.WriteLine();
            }
        }

        private static bool Persist()
        {
            try
            {
                DataStorage.SaveData(_service);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Data could not be saved: {ex.Message}");
                return false;
            }
        }

        static void ListAllFlights()
        {
            ClearScreen();
            Console.WriteLine("=== ALL FLIGHTS ===");
            var flights = _service.GetAllFlights();
            if (!flights.Any())
            {
                Console.WriteLine("No flights available.");
                Pause();
                return;
            }

            foreach (var f in flights)
            {
                Console.WriteLine($"{f.FlightNumber} | {f.Airline} | {f.Origin} -> {f.Destination} | " +
                                  $"Dep: {f.DepartureTime:g} | Seats: {f.AvailableSeats}/{f.TotalSeats} | " +
                                  $"Status: {f.Status} | Gate: {f.Gate} | Term: {f.Terminal}");
            }

            Console.WriteLine("\nDo you want to register a new flight? (y/n) ");
            string? answer = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(answer) && answer.Trim().ToLower() == "y")
                RegisterFlight();

            Console.WriteLine("\nPress any key to continue...");
            Pause();
            return;
        }

        private static void RegisterFlight()
        {
            ClearScreen();
            Console.WriteLine("=== REGISTER NEW FLIGHT ===");

            Console.Write("Flight number (e.g. AA101): ");
            string? flightNumber = Console.ReadLine();
            Console.Write("Airline name: ");
            string? airline = Console.ReadLine();
            Console.Write("Origin code (e.g. IST): ");
            string? origin = Console.ReadLine();
            Console.Write("Destination code (e.g. JFK): ");
            string? destination = Console.ReadLine();
            Console.Write("Departure time (yyyy-mm-dd hh:mm): ");
            if (!DateTime.TryParse(Console.ReadLine(), out DateTime departure))
            {
                Console.WriteLine("❌ Invalid departure time.");
                return;
            }
            Console.Write("Arrival time (yyyy-mm-dd hh:mm): ");
            if (!DateTime.TryParse(Console.ReadLine(), out DateTime arrival))
            {
                Console.WriteLine("❌ Invalid arrival time.");
                return;
            }
            Console.Write("Base price: ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal price))
            {
                Console.WriteLine("❌ Invalid price.");
                return;
            }
            Console.Write("Gate (e.g. A12): ");
            string? gate = Console.ReadLine();
            Console.Write("Terminal (number): ");
            if (!int.TryParse(Console.ReadLine(), out int terminal))
            {
                Console.WriteLine("❌ Invalid terminal.");
                return;
            }
            Console.Write("Number of seat rows: ");
            if (!int.TryParse(Console.ReadLine(), out int rows))
            {
                Console.WriteLine("❌ Invalid row count.");
                return;
            }
            Console.Write("Seat columns (e.g. ABCDEF): ");
            string? columns = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(flightNumber) || string.IsNullOrWhiteSpace(airline) ||
                string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination) ||
                string.IsNullOrWhiteSpace(gate) || string.IsNullOrWhiteSpace(columns))
            {
                Console.WriteLine("❌ All required fields must be filled.");
                return;
            }

            var flight = new Flight
            {
                FlightNumber = flightNumber.Trim(),
                Airline = airline.Trim(),
                Origin = origin.Trim().ToUpper(),
                Destination = destination.Trim().ToUpper(),
                DepartureTime = departure,
                ArrivalTime = arrival,
                BasePrice = price,
                Gate = gate.Trim().ToUpper(),
                Terminal = terminal,
                Status = FlightStatus.Scheduled
            };
            flight.InitializeSeats(rows, columns.Trim().ToUpper());

            try
            {
                _service.AddFlight(flight);
                Console.WriteLine($"✅ Flight {flight.FlightNumber} registered successfully with {flight.TotalSeats} seats.");
                Persist();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {ex.Message}");
            }
        }

        static void SearchFlights()
        {
            ClearScreen();
            Console.WriteLine("=== SEARCH FLIGHTS ===");
            Console.Write("Origin (or leave empty): ");
            string? origin = Console.ReadLine();
            Console.Write("Destination (or leave empty): ");
            string? dest = Console.ReadLine();

            var results = _service.SearchFlights(origin, dest, null);
            if (!results.Any())
            {
                Console.WriteLine("No flights found.");
                Pause();
                return;
            }

            foreach (var f in results)
            {
                Console.WriteLine($"{f.FlightNumber} | {f.Origin}->{f.Destination} | Dep: {f.DepartureTime:g} | " +
                                  $"Seats: {f.AvailableSeats}/{f.TotalSeats} | {f.Status}");
            }
            Console.WriteLine("\nPress any key to continue...");
            Pause();
        }

        static void ManagePassengers()
        {
            ClearScreen();
            Console.WriteLine("=== ALL PASSENGERS ===");

            var passengers = _service.GetAllPassengers();
            if (!passengers.Any())
            {
                Console.WriteLine("No passengers registered yet.");
            }
            else
            {
                foreach (var p in passengers)
                {
                    var bookingCount = _service.GetBookingsForPassenger(p.Id).Count;
                    Console.WriteLine(
                        $"#{p.Id} | {p.FullName} | Email: {p.Email} | Phone: {p.Phone} | " +
                        $"Bookings: {bookingCount}");
                }
            }

            Console.WriteLine("\nDo you want to register a new passenger? (y/n) ");
            string? answer = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(answer) && answer.Trim().ToLower() == "y")
            {
                // RegisterPassenger muestra su propia pantalla y espera una tecla al terminar.
                RegisterPassenger();
                return;
            }

            Console.WriteLine("\nPress any key to continue...");
            Pause();
        }

        static void RegisterPassenger()
        {
            ClearScreen();
            Console.WriteLine("=== REGISTER PASSENGER ===");
            Console.Write("First name: ");
            string? fn = Console.ReadLine();
            Console.Write("Last name: ");
            string? ln = Console.ReadLine();
            Console.Write("Email: ");
            string? email = Console.ReadLine();
            Console.Write("Phone: ");
            string? phone = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(fn) || string.IsNullOrWhiteSpace(ln))
            {
                Console.WriteLine("First and last name are required.");
                Pause();
                return;
            }

            var p = _service.RegisterPassenger(fn, ln, email ?? "", phone ?? "");
            Console.WriteLine($"Passenger registered with ID: {p.Id}");
            Persist();
            Pause();
        }

        static void BookTicket()
        {
            ClearScreen();
            Console.WriteLine("=== BOOK A TICKET ===");
            Console.Write("Flight number: ");
            string? fn = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(fn))
            {
                Console.WriteLine("Flight number required.");
                Pause();
                return;
            }

            Console.Write("Passenger ID: ");
            if (!int.TryParse(Console.ReadLine(), out int pid))
            {
                Console.WriteLine("Invalid ID.");
                Pause();
                return;
            }

            // Show available seats
            var flight = _service.GetAllFlights().FirstOrDefault(f => f.FlightNumber == fn);
            if (flight == null)
            {
                Console.WriteLine("Flight not found.");
                Pause();
                return;
            }

            Console.WriteLine($"\nAvailable seats for {fn}:");
            var avail = flight.GetAvailableSeats();
            if (!avail.Any())
            {
                Console.WriteLine("No seats available.");
                Pause();
                return;
            }
            Console.WriteLine(string.Join(", ", avail.Take(20)) + (avail.Count > 20 ? " ..." : ""));

            Console.Write("Select seat number: ");
            string? seat = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(seat))
            {
                Console.WriteLine("Invalid seat.");
                Pause();
                return;
            }

            var result = _service.BookTicket(fn, pid, seat);
            if (result.success)
            {
                Console.WriteLine($"✅ {result.message}");
                Persist();
            }
            else
            {
                Console.WriteLine($"❌ {result.message}");
            }
            Pause();
        }

        static void CancelBooking()
        {
            ClearScreen();
            Console.WriteLine("=== CANCEL BOOKING ===");
            Console.Write("Booking ID: ");
            if (!int.TryParse(Console.ReadLine(), out int bid))
            {
                Console.WriteLine("Invalid ID.");
                Pause();
                return;
            }

            if (_service.CancelBooking(bid))
            {
                Console.WriteLine("✅ Booking cancelled.");
                Persist();
            }
            else
                Console.WriteLine("❌ Booking not found.");
            Pause();
        }

        static void ViewMyBookings()
        {
            ClearScreen();
            Console.WriteLine("=== MY BOOKINGS ===");
            Console.Write("Passenger ID: ");
            if (!int.TryParse(Console.ReadLine(), out int pid))
            {
                Console.WriteLine("Invalid ID.");
                Pause();
                return;
            }

            var bookings = _service.GetBookingsForPassenger(pid);
            if (!bookings.Any())
            {
                Console.WriteLine("No bookings found.");
                Pause();
                return;
            }

            foreach (var b in bookings)
            {
                var flight = _service.GetAllFlights().FirstOrDefault(f => f.FlightNumber == b.FlightNumber);
                string status = b.IsCheckedIn ? "✅ Checked-in" : "⏳ Not checked-in";
                Console.WriteLine($"Booking #{b.BookingId} | Flight: {b.FlightNumber} | Seat: {b.SeatNumber} | {status}");
                if (flight != null)
                    Console.WriteLine($"  {flight.Origin} -> {flight.Destination} | {flight.DepartureTime:g}");
            }
            Pause();
        }

        static void CheckIn()
        {
            ClearScreen();
            Console.WriteLine("=== CHECK-IN ===");
            Console.Write("Booking ID: ");
            if (!int.TryParse(Console.ReadLine(), out int bid))
            {
                Console.WriteLine("Invalid ID.");
                Pause();
                return;
            }

            if (_service.CheckIn(bid))
            {
                Persist();
                Console.WriteLine("✅ Check-in successful! Here is your boarding pass:");
                var booking = _service.GetAllBookings().FirstOrDefault(b => b.BookingId == bid);
                if (booking != null)
                {
                    var flight = _service.GetAllFlights().FirstOrDefault(f => f.FlightNumber == booking.FlightNumber);
                    var passenger = _service.FindPassenger(booking.PassengerId);
                    Console.WriteLine("----------------------------------------");
                    Console.WriteLine("  BOARDING PASS");
                    Console.WriteLine($"  Passenger: {passenger?.FullName ?? "Unknown"}");
                    Console.WriteLine($"  Flight:    {booking.FlightNumber}");
                    Console.WriteLine($"  Seat:      {booking.SeatNumber}");
                    Console.WriteLine($"  Gate:      {flight?.Gate ?? "N/A"}");
                    Console.WriteLine($"  Terminal:  {flight?.Terminal}");
                    Console.WriteLine($"  Departure: {flight?.DepartureTime:g}");
                    Console.WriteLine("----------------------------------------");
                }
            }
            else
            {
                Console.WriteLine("❌ Check-in failed. Booking not found or already checked in.");
            }
            Pause();
        }

        static void AdvanceTime()
        {
            ClearScreen();
            Console.WriteLine("=== ADVANCE TIME (SIMULATION) ===");
            Console.Write("Enter hours to advance: ");
            if (!double.TryParse(Console.ReadLine(), out double hours))
            {
                Console.WriteLine("Invalid input.");
                Pause();
                return;
            }

            _service.AdvanceTime(TimeSpan.FromHours(hours));
            Console.WriteLine($"Time advanced by {hours} hour(s). Flight statuses updated.");
            Persist();
            Pause();
        }

        static void ShowSeatMap()
        {
            ClearScreen();
            Console.WriteLine("=== SEAT MAP ===");
            Console.Write("Flight number: ");
            string? fn = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(fn))
            {
                Console.WriteLine("Invalid flight.");
                Pause();
                return;
            }

            var flight = _service.GetAllFlights().FirstOrDefault(f => f.FlightNumber == fn);
            if (flight == null)
            {
                Console.WriteLine("Flight not found.");
                Pause();
                return;
            }

            Console.WriteLine($"Seat map for {fn} (X = booked, . = available):");
            Console.WriteLine("     A  B  C  D  E  F");
            int row = 1;
            foreach (var kvp in flight.SeatMap.OrderBy(k => k.Key))
            {
                string seat = kvp.Key;
                bool booked = kvp.Value;
                if (seat.StartsWith(row.ToString()))
                {
                    Console.Write($" {row.ToString().PadLeft(2)}  ");
                    row++;
                }
                Console.Write($" {(booked ? 'X' : '.')}  ");
                if (seat.EndsWith("F"))
                    Console.WriteLine();
            }
            Console.WriteLine("\nPress any key to continue...");
            Pause();
        }

        private static string ReadWithDefault(string prompt, string current)
        {
            Console.Write($"{prompt} [{current}]: ");
            var value = Console.ReadLine();
            return string.IsNullOrWhiteSpace(value) ? current : value.Trim();
        }

        private static bool ConfirmAction(string question)
        {
            Console.Write($"{question} (y/n): ");
            var answer = Console.ReadLine();
            return string.Equals(answer?.Trim(), "y", StringComparison.OrdinalIgnoreCase);
        }

        static void EditFlight()
        {
            ClearScreen();
            Console.WriteLine("=== EDIT FLIGHT ===");
            Console.Write("Flight number to edit: ");
            var original = Console.ReadLine()?.Trim();
            var current = _service.GetAllFlights()
                .FirstOrDefault(f => f.FlightNumber == original);

            if (current is null)
            {
                Console.WriteLine($"❌ Flight {original} not found.");
                Pause();
                return;
            }

            var flightNumber = ReadWithDefault("New flight number", current.FlightNumber).ToUpperInvariant();
            var airline = ReadWithDefault("Airline", current.Airline);
            var origin = ReadWithDefault("Origin", current.Origin).ToUpperInvariant();
            var destination = ReadWithDefault("Destination", current.Destination).ToUpperInvariant();

            Console.Write($"Departure [{current.DepartureTime:yyyy-MM-dd HH:mm}]: ");
            var departureText = Console.ReadLine();
            var departure = current.DepartureTime;
            if (!string.IsNullOrWhiteSpace(departureText) &&
                !DateTime.TryParse(departureText, out departure))
            {
                Console.WriteLine("❌ Invalid departure time.");
                Pause();
                return;
            }

            Console.Write($"Arrival [{current.ArrivalTime:yyyy-MM-dd HH:mm}]: ");
            var arrivalText = Console.ReadLine();
            var arrival = current.ArrivalTime;
            if (!string.IsNullOrWhiteSpace(arrivalText) &&
                !DateTime.TryParse(arrivalText, out arrival))
            {
                Console.WriteLine("❌ Invalid arrival time.");
                Pause();
                return;
            }

            Console.Write($"Base price [{current.BasePrice}]: ");
            var priceText = Console.ReadLine();
            var price = current.BasePrice;
            if (!string.IsNullOrWhiteSpace(priceText) &&
                !decimal.TryParse(priceText, out price))
            {
                Console.WriteLine("❌ Invalid price.");
                Pause();
                return;
            }

            var gate = ReadWithDefault("Gate", current.Gate).ToUpperInvariant();

            Console.Write($"Terminal [{current.Terminal}]: ");
            var terminalText = Console.ReadLine();
            var terminal = current.Terminal;
            if (!string.IsNullOrWhiteSpace(terminalText) &&
                !int.TryParse(terminalText, out terminal))
            {
                Console.WriteLine("❌ Invalid terminal.");
                Pause();
                return;
            }

            var hasBookings = _service.GetAllBookings().Any(b => b.FlightNumber == current.FlightNumber);
            var rows = current.SeatMap.Keys
                .Select(key => new string(key.TakeWhile(char.IsDigit).ToArray()))
                .Distinct()
                .Count();
            var columns = string.Concat(current.SeatMap.Keys
                .Where(k => k.Length > 1)
                .Select(k => k[^1])
                .Distinct()
                .OrderBy(c => c));

            var updated = new Flight
            {
                FlightNumber = flightNumber,
                Airline = airline,
                Origin = origin,
                Destination = destination,
                DepartureTime = departure,
                ArrivalTime = arrival,
                BasePrice = price,
                Gate = gate,
                Terminal = terminal,
                Status = current.Status,
                SeatMap = new Dictionary<string, bool>(current.SeatMap),
                TotalSeats = current.TotalSeats
            };

            if (!hasBookings)
            {
                Console.Write($"Number of seat rows [{Math.Max(1, rows)}]: ");
                var rowsText = Console.ReadLine();
                var parsedRows = Math.Max(1, rows);
                if (!string.IsNullOrWhiteSpace(rowsText) && !int.TryParse(rowsText, out parsedRows))
                    parsedRows = Math.Max(1, rows);
                var rowCount = Math.Max(1, parsedRows);
                var columnText = ReadWithDefault("Seat columns", string.IsNullOrWhiteSpace(columns) ? "ABCDEF" : columns);
                updated.InitializeSeats(rowCount, columnText.ToUpperInvariant());
            }
            else
            {
                Console.WriteLine("This flight has bookings; its seat layout will be preserved.");
            }

            try
            {
                _service.UpdateFlight(current.FlightNumber, updated);
                Console.WriteLine($"✅ Flight updated: {current.FlightNumber} -> {updated.FlightNumber}");
                Persist();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {ex.Message}");
            }

            Pause();
        }

        static void EditPassenger()
        {
            ClearScreen();
            Console.WriteLine("=== EDIT PASSENGER ===");
            Console.Write("Passenger ID: ");
            if (!int.TryParse(Console.ReadLine(), out var id))
            {
                Console.WriteLine("❌ Invalid ID.");
                Pause();
                return;
            }

            var current = _service.FindPassenger(id);
            if (current is null)
            {
                Console.WriteLine($"❌ Passenger #{id} not found.");
                Pause();
                return;
            }

            var updated = new Passenger
            {
                Id = id,
                FirstName = ReadWithDefault("First name", current.FirstName),
                LastName = ReadWithDefault("Last name", current.LastName),
                Email = ReadWithDefault("Email", current.Email),
                Phone = ReadWithDefault("Phone", current.Phone)
            };

            try
            {
                _service.UpdatePassenger(updated);
                Console.WriteLine($"✅ Passenger #{id} updated: {updated.FullName}");
                Persist();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {ex.Message}");
            }

            Pause();
        }

        static void EditBooking()
        {
            ClearScreen();
            Console.WriteLine("=== EDIT BOOKING ===");
            Console.Write("Booking ID: ");
            if (!int.TryParse(Console.ReadLine(), out var id))
            {
                Console.WriteLine("❌ Invalid ID.");
                Pause();
                return;
            }

            var current = _service.GetAllBookings().FirstOrDefault(b => b.BookingId == id);
            if (current is null)
            {
                Console.WriteLine($"❌ Booking #{id} not found.");
                Pause();
                return;
            }

            var passenger = _service.FindPassenger(current.PassengerId);
            Console.WriteLine($"Current: flight {current.FlightNumber}, passenger {passenger?.FullName}, seat {current.SeatNumber}");

            var flightNumber = ReadWithDefault("New flight number", current.FlightNumber).ToUpperInvariant();
            Console.Write($"New passenger ID [{current.PassengerId}]: ");
            var passengerText = Console.ReadLine();
            var parsedPassenger = current.PassengerId;
            if (!string.IsNullOrWhiteSpace(passengerText) && !int.TryParse(passengerText, out parsedPassenger))
            {
                Console.WriteLine("❌ Invalid passenger ID.");
                Pause();
                return;
            }
            var passengerId = parsedPassenger;
            var seat = ReadWithDefault("New seat", current.SeatNumber).ToUpperInvariant();

            var result = _service.UpdateBooking(id, flightNumber, passengerId, seat);
            Console.WriteLine(result.success ? $"✅ {result.message}" : $"❌ {result.message}");
            if (result.success)
                Persist();

            Pause();
        }

        static void DeleteFlight()
        {
            ClearScreen();
            Console.WriteLine("=== DELETE FLIGHT ===");
            Console.Write("Flight number: ");
            var number = Console.ReadLine()?.Trim();
            var flight = _service.GetAllFlights().FirstOrDefault(f => f.FlightNumber == number);
            if (flight is null)
            {
                Console.WriteLine($"❌ Flight {number} not found.");
                Pause();
                return;
            }

            var bookings = _service.GetAllBookings().Count(b => b.FlightNumber == flight.FlightNumber);
            var detail = bookings == 0
                ? $"Delete flight {number}?"
                : $"Delete flight {number}? Its {bookings} booking(s) will also be cancelled.";

            if (!ConfirmAction(detail))
            {
                Console.WriteLine("Deletion cancelled.");
                Pause();
                return;
            }

            _service.DeleteFlight(flight.FlightNumber, out var removed);
            Console.WriteLine($"✅ Flight {flight.FlightNumber} deleted. {removed} booking(s) cancelled.");
            Persist();
            Pause();
        }

        static void DeletePassenger()
        {
            ClearScreen();
            Console.WriteLine("=== DELETE PASSENGER ===");
            Console.Write("Passenger ID: ");
            if (!int.TryParse(Console.ReadLine(), out var id))
            {
                Console.WriteLine("❌ Invalid ID.");
                Pause();
                return;
            }

            var passenger = _service.FindPassenger(id);
            if (passenger is null)
            {
                Console.WriteLine($"❌ Passenger #{id} not found.");
                Pause();
                return;
            }

            var bookings = _service.GetBookingsForPassenger(id).Count;
            var detail = bookings == 0
                ? $"Delete passenger {passenger.FullName} (#{id})?"
                : $"Delete passenger {passenger.FullName} (#{id})? {bookings} booking(s) will be cancelled and seats freed.";

            if (!ConfirmAction(detail))
            {
                Console.WriteLine("Deletion cancelled.");
                Pause();
                return;
            }

            _service.DeletePassenger(id, out var removed);
            Console.WriteLine($"✅ Passenger #{id} deleted. {removed} booking(s) cancelled.");
            Persist();
            Pause();
        }

        static void DeleteBooking()
        {
            ClearScreen();
            Console.WriteLine("=== DELETE BOOKING ===");
            Console.Write("Booking ID: ");
            if (!int.TryParse(Console.ReadLine(), out var id))
            {
                Console.WriteLine("❌ Invalid ID.");
                Pause();
                return;
            }

            var booking = _service.GetAllBookings().FirstOrDefault(b => b.BookingId == id);
            if (booking is null)
            {
                Console.WriteLine($"❌ Booking #{id} not found.");
                Pause();
                return;
            }

            if (!ConfirmAction($"Delete booking #{id} (flight {booking.FlightNumber}, seat {booking.SeatNumber})?"))
            {
                Console.WriteLine("Deletion cancelled.");
                Pause();
                return;
            }

            if (_service.DeleteBooking(id))
            {
                Console.WriteLine($"✅ Booking #{id} deleted. Seat freed.");
                Persist();
            }
            else
            {
                Console.WriteLine($"❌ Booking #{id} not found.");
            }

            Pause();
        }
    }
}