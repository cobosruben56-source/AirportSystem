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
        private static readonly ConsoleLocalization _loc = ConsoleLocalization.Instance;

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            ChooseLanguage();

            if (!Login())
                return;

            LoadSavedData();

            bool exit = false;
            while (!exit)
            {
                ClearScreen();
                ReloadIfChanged();

                Console.WriteLine(_loc.Get("Separator"));
                Console.WriteLine(_loc.Get("AppTitle"));
                Console.WriteLine(_loc.Get("Separator"));
                Console.WriteLine(_loc.Get("Menu1"));
                Console.WriteLine(_loc.Get("Menu2"));
                Console.WriteLine(_loc.Get("Menu3"));
                Console.WriteLine(_loc.Get("Menu4"));
                Console.WriteLine(_loc.Get("Menu5"));
                Console.WriteLine(_loc.Get("Menu6"));
                Console.WriteLine(_loc.Get("Menu7"));
                Console.WriteLine(_loc.Get("Menu8"));
                Console.WriteLine(_loc.Get("Menu9"));
                Console.WriteLine(_loc.Get("Menu10"));
                Console.WriteLine(_loc.Get("Menu11"));
                Console.WriteLine(_loc.Get("Menu12"));
                Console.WriteLine(_loc.Get("Menu13"));
                Console.WriteLine(_loc.Get("Menu14"));
                Console.WriteLine(_loc.Get("Menu15"));
                Console.WriteLine(_loc.Get("MenuExit"));
                Console.WriteLine(_loc.Get("DataFile", DataStorage.FilePath));
                Console.Write(_loc.Get("YourChoice"));

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
                            Console.WriteLine(_loc.Get("DataSavedGoodbye"));
                        else
                            Console.WriteLine(_loc.Get("DataNotSavedExit"));
                        exit = true;
                        break;
                    default:
                        Console.WriteLine(_loc.Get("InvalidOption"));
                        Pause();
                        break;
                }
            }
        }

        /// <summary>
        /// Pregunta el idioma antes del login. Si no se elige nada se conserva la
        /// preferencia guardada (la misma que usa la aplicación de escritorio).
        /// </summary>
        private static void ChooseLanguage()
        {
            ClearScreen();
            Console.WriteLine(_loc.Get("Separator"));
            Console.WriteLine(_loc.Get("SelectLanguageTitle"));
            Console.WriteLine(_loc.Get("Separator"));
            Console.Write(_loc.Get("LanguagePrompt", _loc.SelectedLanguage));

            var answer = Console.ReadLine()?.Trim();
            if (answer == "1")
                _loc.SetLanguage(ConsoleLocalization.English);
            else if (answer == "2")
                _loc.SetLanguage(ConsoleLocalization.Spanish);

            Console.WriteLine(_loc.Get("LanguageSelected", _loc.SelectedLanguage));
            Console.WriteLine();
        }

        private static bool Login()
        {
            const int maxAttempts = 3;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                ClearScreen();
                Console.WriteLine(_loc.Get("Separator"));
                Console.WriteLine(_loc.Get("LoginTitle"));
                Console.WriteLine(_loc.Get("Separator"));
                Console.WriteLine(_loc.Get("Attempt", attempt, maxAttempts));
                Console.Write(_loc.Get("UserPrompt"));
                var user = Console.ReadLine();
                Console.Write(_loc.Get("PasswordPrompt"));
                var password = ReadPassword();

                if (_auth.Authenticate(user, password))
                {
                    Console.WriteLine(_loc.Get("LoginOk"));
                    return true;
                }

                Console.WriteLine(_loc.Get("LoginError"));
                if (attempt < maxAttempts)
                {
                    Console.WriteLine(_loc.Get("LoginRetry"));
                    Pause();
                }
            }

            Console.WriteLine(_loc.Get("LoginTooMany"));
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
                        _loc.Get("LoadedSaved", flights.Count, passengers.Count, bookings.Count));
                    return;
                }
            }
            catch (Exception ex)
            {
                // Si el archivo esta corrupto no lo sobrescribimos automaticamente.
                Console.WriteLine(_loc.Get("LoadError", _loc.T(ex.Message)));
                Console.WriteLine(_loc.Get("SeedKeep"));
                _service.SeedData();
                return;
            }

            Console.WriteLine(_loc.Get("NoSavedData"));
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
                Console.WriteLine(_loc.Get("Synced"));
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(_loc.Get("ReloadError", _loc.T(ex.Message)));
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
                Console.WriteLine(_loc.Get("SaveError", _loc.T(ex.Message)));
                return false;
            }
        }

        static void ListAllFlights()
        {
            ClearScreen();
            Console.WriteLine(_loc.Get("AllFlights"));
            var flights = _service.GetAllFlights();
            if (!flights.Any())
            {
                Console.WriteLine(_loc.Get("NoFlights"));
                Pause();
                return;
            }

            foreach (var f in flights)
            {
                Console.WriteLine(_loc.Get("FlightRow",
                    f.FlightNumber, f.Airline, f.Origin, f.Destination,
                    f.DepartureTime, f.AvailableSeats, f.TotalSeats,
                    f.Status, f.Gate, f.Terminal));
            }

            Console.WriteLine(_loc.Get("RegisterFlightQuestion"));
            string? answer = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(answer) &&
                answer.Trim().Equals("y", StringComparison.OrdinalIgnoreCase))
                RegisterFlight();

            Console.WriteLine(_loc.Get("PressAnyKey"));
            Pause();
            return;
        }

        private static void RegisterFlight()
        {
            ClearScreen();
            Console.WriteLine(_loc.Get("RegisterNewFlight"));

            Console.Write(_loc.Get("FlightNumberPrompt"));
            string? flightNumber = Console.ReadLine();
            Console.Write(_loc.Get("AirlinePrompt"));
            string? airline = Console.ReadLine();
            Console.Write(_loc.Get("OriginPrompt"));
            string? origin = Console.ReadLine();
            Console.Write(_loc.Get("DestinationPrompt"));
            string? destination = Console.ReadLine();
            Console.Write(_loc.Get("DeparturePrompt"));
            if (!DateTime.TryParse(Console.ReadLine(), out DateTime departure))
            {
                Console.WriteLine(_loc.Get("InvalidDeparture"));
                return;
            }
            Console.Write(_loc.Get("ArrivalPrompt"));
            if (!DateTime.TryParse(Console.ReadLine(), out DateTime arrival))
            {
                Console.WriteLine(_loc.Get("InvalidArrival"));
                return;
            }
            Console.Write(_loc.Get("BasePricePrompt"));
            if (!decimal.TryParse(Console.ReadLine(), out decimal price))
            {
                Console.WriteLine(_loc.Get("InvalidPrice"));
                return;
            }
            Console.Write(_loc.Get("GatePrompt"));
            string? gate = Console.ReadLine();
            Console.Write(_loc.Get("TerminalPrompt"));
            if (!int.TryParse(Console.ReadLine(), out int terminal))
            {
                Console.WriteLine(_loc.Get("InvalidTerminal"));
                return;
            }
            Console.Write(_loc.Get("SeatRowsPrompt"));
            if (!int.TryParse(Console.ReadLine(), out int rows))
            {
                Console.WriteLine(_loc.Get("InvalidRowCount"));
                return;
            }
            Console.Write(_loc.Get("SeatColumnsPrompt"));
            string? columns = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(flightNumber) || string.IsNullOrWhiteSpace(airline) ||
                string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination) ||
                string.IsNullOrWhiteSpace(gate) || string.IsNullOrWhiteSpace(columns))
            {
                Console.WriteLine(_loc.Get("AllFieldsRequired"));
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
                Console.WriteLine(_loc.Get("FlightRegistered", flight.FlightNumber, flight.TotalSeats));
                Persist();
            }
            catch (Exception ex)
            {
                Console.WriteLine(_loc.Get("UpdatePrefix", _loc.T(ex.Message)));
            }
        }

        static void SearchFlights()
        {
            ClearScreen();
            Console.WriteLine(_loc.Get("SearchFlights"));
            Console.Write(_loc.Get("OriginEmptyPrompt"));
            string? origin = Console.ReadLine();
            Console.Write(_loc.Get("DestinationEmptyPrompt"));
            string? dest = Console.ReadLine();

            var results = _service.SearchFlights(origin, dest, null);
            if (!results.Any())
            {
                Console.WriteLine(_loc.Get("NoFlightsFound"));
                Pause();
                return;
            }

            foreach (var f in results)
            {
                Console.WriteLine(_loc.Get("SearchRow",
                    f.FlightNumber, f.Origin, f.Destination,
                    f.DepartureTime, f.AvailableSeats, f.TotalSeats, f.Status));
            }
            Console.WriteLine(_loc.Get("PressAnyKey"));
            Pause();
        }

        static void ManagePassengers()
        {
            ClearScreen();
            Console.WriteLine(_loc.Get("AllPassengers"));

            var passengers = _service.GetAllPassengers();
            if (!passengers.Any())
            {
                Console.WriteLine(_loc.Get("NoPassengers"));
            }
            else
            {
                foreach (var p in passengers)
                {
                    var bookingCount = _service.GetBookingsForPassenger(p.Id).Count;
                    Console.WriteLine(_loc.Get("PassengerRow",
                        p.Id, p.FullName, p.Email, p.Phone, bookingCount));
                }
            }

            Console.WriteLine(_loc.Get("RegisterPassengerQuestion"));
            string? answer = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(answer) &&
                answer.Trim().Equals("y", StringComparison.OrdinalIgnoreCase))
            {
                // RegisterPassenger muestra su propia pantalla y espera una tecla al terminar.
                RegisterPassenger();
                return;
            }

            Console.WriteLine(_loc.Get("PressAnyKey"));
            Pause();
        }

        static void RegisterPassenger()
        {
            ClearScreen();
            Console.WriteLine(_loc.Get("RegisterPassenger"));
            Console.Write(_loc.Get("FirstNamePrompt"));
            string? fn = Console.ReadLine();
            Console.Write(_loc.Get("LastNamePrompt"));
            string? ln = Console.ReadLine();
            Console.Write(_loc.Get("EmailPrompt"));
            string? email = Console.ReadLine();
            Console.Write(_loc.Get("PhonePrompt"));
            string? phone = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(fn) || string.IsNullOrWhiteSpace(ln))
            {
                Console.WriteLine(_loc.Get("NameLastNameRequired"));
                Pause();
                return;
            }

            var p = _service.RegisterPassenger(fn, ln, email ?? "", phone ?? "");
            Console.WriteLine(_loc.Get("PassengerRegistered", p.Id));
            Persist();
            Pause();
        }

        static void BookTicket()
        {
            ClearScreen();
            Console.WriteLine(_loc.Get("BookTicket"));
            Console.Write(_loc.Get("FlightNumberRequiredPrompt"));
            string? fn = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(fn))
            {
                Console.WriteLine(_loc.Get("FlightNumberRequired"));
                Pause();
                return;
            }

            Console.Write(_loc.Get("PassengerIdPrompt"));
            if (!int.TryParse(Console.ReadLine(), out int pid))
            {
                Console.WriteLine(_loc.Get("InvalidId"));
                Pause();
                return;
            }

            // Show available seats
            var flight = _service.GetAllFlights().FirstOrDefault(f => f.FlightNumber == fn);
            if (flight == null)
            {
                Console.WriteLine(_loc.Get("FlightNotFound"));
                Pause();
                return;
            }

            Console.WriteLine(_loc.Get("AvailableSeats", fn));
            var avail = flight.GetAvailableSeats();
            if (!avail.Any())
            {
                Console.WriteLine(_loc.Get("NoSeatsAvailable"));
                Pause();
                return;
            }
            Console.WriteLine(string.Join(", ", avail.Take(20)) + (avail.Count > 20 ? " ..." : ""));

            Console.Write(_loc.Get("SelectSeatPrompt"));
            string? seat = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(seat))
            {
                Console.WriteLine(_loc.Get("InvalidSeat"));
                Pause();
                return;
            }

            var result = _service.BookTicket(fn, pid, seat);
            if (result.success)
            {
                Console.WriteLine(_loc.Get("BookingConfirmed", _loc.T(result.message)));
                Persist();
            }
            else
            {
                Console.WriteLine(_loc.Get("UpdatePrefix", _loc.T(result.message)));
            }
            Pause();
        }

        static void CancelBooking()
        {
            ClearScreen();
            Console.WriteLine(_loc.Get("CancelBooking"));
            Console.Write(_loc.Get("BookingIdPrompt"));
            if (!int.TryParse(Console.ReadLine(), out int bid))
            {
                Console.WriteLine(_loc.Get("InvalidId"));
                Pause();
                return;
            }

            if (_service.CancelBooking(bid))
            {
                Console.WriteLine(_loc.Get("BookingCancelled"));
                Persist();
            }
            else
                Console.WriteLine(_loc.Get("BookingNotFound"));
            Pause();
        }

        static void ViewMyBookings()
        {
            ClearScreen();
            Console.WriteLine(_loc.Get("MyBookings"));
            Console.Write(_loc.Get("PassengerIdPrompt"));
            if (!int.TryParse(Console.ReadLine(), out int pid))
            {
                Console.WriteLine(_loc.Get("InvalidId"));
                Pause();
                return;
            }

            var bookings = _service.GetBookingsForPassenger(pid);
            if (!bookings.Any())
            {
                Console.WriteLine(_loc.Get("NoBookings"));
                Pause();
                return;
            }

            foreach (var b in bookings)
            {
                var flight = _service.GetAllFlights().FirstOrDefault(f => f.FlightNumber == b.FlightNumber);
                string status = _loc.Get(b.IsCheckedIn ? "CheckedIn" : "NotCheckedIn");
                Console.WriteLine(_loc.Get("BookingRow", b.BookingId, b.FlightNumber, b.SeatNumber, status));
                if (flight != null)
                    Console.WriteLine($"  {flight.Origin} -> {flight.Destination} | {flight.DepartureTime:g}");
            }
            Pause();
        }

        static void CheckIn()
        {
            ClearScreen();
            Console.WriteLine(_loc.Get("CheckIn"));
            Console.Write(_loc.Get("BookingIdPrompt"));
            if (!int.TryParse(Console.ReadLine(), out int bid))
            {
                Console.WriteLine(_loc.Get("InvalidId"));
                Pause();
                return;
            }

            if (_service.CheckIn(bid))
            {
                Persist();
                Console.WriteLine(_loc.Get("CheckInOk"));
                var booking = _service.GetAllBookings().FirstOrDefault(b => b.BookingId == bid);
                if (booking != null)
                {
                    var flight = _service.GetAllFlights().FirstOrDefault(f => f.FlightNumber == booking.FlightNumber);
                    var passenger = _service.FindPassenger(booking.PassengerId);
                    Console.WriteLine(_loc.Get("Separator"));
                    Console.WriteLine(_loc.Get("BoardingPass"));
                    Console.WriteLine(_loc.Get("PassengerLabel", passenger?.FullName ?? _loc.Get("Unknown")));
                    Console.WriteLine(_loc.Get("FlightLabel", booking.FlightNumber));
                    Console.WriteLine(_loc.Get("SeatLabel", booking.SeatNumber));
                    Console.WriteLine(_loc.Get("GateLabel", flight?.Gate ?? _loc.Get("NotAvailable")));
                    Console.WriteLine(_loc.Get("TerminalLabel", flight?.Terminal ?? 0));
                    Console.WriteLine(_loc.Get("DepartureLabel", flight?.DepartureTime ?? default));
                    Console.WriteLine(_loc.Get("Separator"));
                }
            }
            else
            {
                Console.WriteLine(_loc.Get("CheckInFailed"));
            }
            Pause();
        }

        static void AdvanceTime()
        {
            ClearScreen();
            Console.WriteLine(_loc.Get("AdvanceTime"));
            Console.Write(_loc.Get("HoursPrompt"));
            if (!double.TryParse(Console.ReadLine(), out double hours))
            {
                Console.WriteLine(_loc.Get("InvalidInput"));
                Pause();
                return;
            }

            _service.AdvanceTime(TimeSpan.FromHours(hours));
            Console.WriteLine(_loc.Get("TimeAdvanced", hours));
            Persist();
            Pause();
        }

        static void ShowSeatMap()
        {
            ClearScreen();
            Console.WriteLine(_loc.Get("SeatMap"));
            Console.Write(_loc.Get("FlightNumberRequiredPrompt"));
            string? fn = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(fn))
            {
                Console.WriteLine(_loc.Get("InvalidFlight"));
                Pause();
                return;
            }

            var flight = _service.GetAllFlights().FirstOrDefault(f => f.FlightNumber == fn);
            if (flight == null)
            {
                Console.WriteLine(_loc.Get("FlightNotFound"));
                Pause();
                return;
            }

            Console.WriteLine(_loc.Get("SeatMapTitle", fn));
            Console.WriteLine(_loc.Get("SeatMapColumns"));
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
            Console.WriteLine(_loc.Get("PressAnyKey"));
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
            Console.WriteLine(_loc.Get("EditFlight"));
            Console.Write(_loc.Get("FlightEditPrompt"));
            var original = Console.ReadLine()?.Trim();
            var current = _service.GetAllFlights()
                .FirstOrDefault(f => f.FlightNumber == original);

            if (current is null)
            {
                Console.WriteLine(_loc.Get("FlightNotFoundId", original ?? string.Empty));
                Pause();
                return;
            }

            var flightNumber = ReadWithDefault(_loc.Get("NewFlightNumber"), current.FlightNumber).ToUpperInvariant();
            var airline = ReadWithDefault(_loc.Get("Airline"), current.Airline);
            var origin = ReadWithDefault(_loc.Get("Origin"), current.Origin).ToUpperInvariant();
            var destination = ReadWithDefault(_loc.Get("Destination"), current.Destination).ToUpperInvariant();

            Console.Write($"{_loc.Get("DeparturePrompt").TrimEnd(' ', ':')} [{current.DepartureTime:yyyy-MM-dd HH:mm}]: ");
            var departureText = Console.ReadLine();
            var departure = current.DepartureTime;
            if (!string.IsNullOrWhiteSpace(departureText) &&
                !DateTime.TryParse(departureText, out departure))
            {
                Console.WriteLine(_loc.Get("InvalidDeparture"));
                Pause();
                return;
            }

            Console.Write($"{_loc.Get("ArrivalPrompt").TrimEnd(' ', ':')} [{current.ArrivalTime:yyyy-MM-dd HH:mm}]: ");
            var arrivalText = Console.ReadLine();
            var arrival = current.ArrivalTime;
            if (!string.IsNullOrWhiteSpace(arrivalText) &&
                !DateTime.TryParse(arrivalText, out arrival))
            {
                Console.WriteLine(_loc.Get("InvalidArrival"));
                Pause();
                return;
            }

            Console.Write($"{_loc.Get("BasePricePrompt").TrimEnd(' ', ':')} [{current.BasePrice}]: ");
            var priceText = Console.ReadLine();
            var price = current.BasePrice;
            if (!string.IsNullOrWhiteSpace(priceText) &&
                !decimal.TryParse(priceText, out price))
            {
                Console.WriteLine(_loc.Get("InvalidPrice"));
                Pause();
                return;
            }

            var gate = ReadWithDefault(_loc.Get("Gate"), current.Gate).ToUpperInvariant();

            Console.Write($"{_loc.Get("TerminalPrompt").TrimEnd(' ', ':')} [{current.Terminal}]: ");
            var terminalText = Console.ReadLine();
            var terminal = current.Terminal;
            if (!string.IsNullOrWhiteSpace(terminalText) &&
                !int.TryParse(terminalText, out terminal))
            {
                Console.WriteLine(_loc.Get("InvalidTerminal"));
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
                Console.Write($"{_loc.Get("NumberSeatRows")} [{Math.Max(1, rows)}]: ");
                var rowsText = Console.ReadLine();
                var parsedRows = Math.Max(1, rows);
                if (!string.IsNullOrWhiteSpace(rowsText) && !int.TryParse(rowsText, out parsedRows))
                    parsedRows = Math.Max(1, rows);
                var rowCount = Math.Max(1, parsedRows);
                var columnText = ReadWithDefault(_loc.Get("SeatColumns"), string.IsNullOrWhiteSpace(columns) ? "ABCDEF" : columns);
                updated.InitializeSeats(rowCount, columnText.ToUpperInvariant());
            }
            else
            {
                Console.WriteLine(_loc.Get("SeatLayoutPreserved"));
            }

            try
            {
                _service.UpdateFlight(current.FlightNumber, updated);
                Console.WriteLine(_loc.Get("FlightUpdated", current.FlightNumber, updated.FlightNumber));
                Persist();
            }
            catch (Exception ex)
            {
                Console.WriteLine(_loc.Get("UpdatePrefix", _loc.T(ex.Message)));
            }

            Pause();
        }

        static void EditPassenger()
        {
            ClearScreen();
            Console.WriteLine(_loc.Get("EditPassenger"));
            Console.Write(_loc.Get("PassengerIdPrompt"));
            if (!int.TryParse(Console.ReadLine(), out var id))
            {
                Console.WriteLine(_loc.Get("InvalidId"));
                Pause();
                return;
            }

            var current = _service.FindPassenger(id);
            if (current is null)
            {
                Console.WriteLine(_loc.Get("PassengerNotFound", id));
                Pause();
                return;
            }

            var updated = new Passenger
            {
                Id = id,
                FirstName = ReadWithDefault(_loc.Get("FirstNamePrompt").TrimEnd(' ', ':'), current.FirstName),
                LastName = ReadWithDefault(_loc.Get("LastNamePrompt").TrimEnd(' ', ':'), current.LastName),
                Email = ReadWithDefault(_loc.Get("EmailPrompt").TrimEnd(' ', ':'), current.Email),
                Phone = ReadWithDefault(_loc.Get("PhonePrompt").TrimEnd(' ', ':'), current.Phone)
            };

            try
            {
                _service.UpdatePassenger(updated);
                Console.WriteLine(_loc.Get("PassengerUpdated", id, updated.FullName));
                Persist();
            }
            catch (Exception ex)
            {
                Console.WriteLine(_loc.Get("UpdatePrefix", _loc.T(ex.Message)));
            }

            Pause();
        }

        static void EditBooking()
        {
            ClearScreen();
            Console.WriteLine(_loc.Get("EditBooking"));
            Console.Write(_loc.Get("BookingIdPrompt"));
            if (!int.TryParse(Console.ReadLine(), out var id))
            {
                Console.WriteLine(_loc.Get("InvalidId"));
                Pause();
                return;
            }

            var current = _service.GetAllBookings().FirstOrDefault(b => b.BookingId == id);
            if (current is null)
            {
                Console.WriteLine(_loc.Get("BookingNotFoundId", id));
                Pause();
                return;
            }

            var passenger = _service.FindPassenger(current.PassengerId);
            Console.WriteLine(_loc.Get("CurrentBooking",
                current.FlightNumber, passenger?.FullName ?? _loc.Get("Unknown"), current.SeatNumber));

            var flightNumber = ReadWithDefault(_loc.Get("NewFlightNumber"), current.FlightNumber).ToUpperInvariant();
            Console.Write($"{_loc.Get("NewPassengerId")} [{current.PassengerId}]: ");
            var passengerText = Console.ReadLine();
            var parsedPassenger = current.PassengerId;
            if (!string.IsNullOrWhiteSpace(passengerText) && !int.TryParse(passengerText, out parsedPassenger))
            {
                Console.WriteLine(_loc.Get("InvalidPassengerId"));
                Pause();
                return;
            }
            var passengerId = parsedPassenger;
            var seat = ReadWithDefault(_loc.Get("NewSeat"), current.SeatNumber).ToUpperInvariant();

            var result = _service.UpdateBooking(id, flightNumber, passengerId, seat);
            Console.WriteLine(result.success
                ? _loc.Get("BookingConfirmed", _loc.T(result.message))
                : _loc.Get("UpdatePrefix", _loc.T(result.message)));
            if (result.success)
                Persist();

            Pause();
        }

        static void DeleteFlight()
        {
            ClearScreen();
            Console.WriteLine(_loc.Get("DeleteFlight"));
            Console.Write(_loc.Get("FlightNumberRequiredPrompt"));
            var number = Console.ReadLine()?.Trim() ?? string.Empty;
            var flight = _service.GetAllFlights().FirstOrDefault(f => f.FlightNumber == number);
            if (flight is null)
            {
                Console.WriteLine(_loc.Get("FlightNotFoundId", number));
                Pause();
                return;
            }

            var bookings = _service.GetAllBookings().Count(b => b.FlightNumber == flight.FlightNumber);
            var detail = bookings == 0
                ? _loc.Get("ConfirmDeleteFlight", number)
                : _loc.Get("ConfirmDeleteFlightBookings", number, bookings);

            if (!ConfirmAction(detail))
            {
                Console.WriteLine(_loc.Get("DeletionCancelled"));
                Pause();
                return;
            }

            _service.DeleteFlight(flight.FlightNumber, out var removed);
            Console.WriteLine(_loc.Get("FlightDeleted", flight.FlightNumber, removed));
            Persist();
            Pause();
        }

        static void DeletePassenger()
        {
            ClearScreen();
            Console.WriteLine(_loc.Get("DeletePassenger"));
            Console.Write(_loc.Get("PassengerIdPrompt"));
            if (!int.TryParse(Console.ReadLine(), out var id))
            {
                Console.WriteLine(_loc.Get("InvalidId"));
                Pause();
                return;
            }

            var passenger = _service.FindPassenger(id);
            if (passenger is null)
            {
                Console.WriteLine(_loc.Get("PassengerNotFound", id));
                Pause();
                return;
            }

            var bookings = _service.GetBookingsForPassenger(id).Count;
            var detail = bookings == 0
                ? _loc.Get("ConfirmDeletePassenger", passenger.FullName, id)
                : _loc.Get("ConfirmDeletePassengerBookings", passenger.FullName, id, bookings);

            if (!ConfirmAction(detail))
            {
                Console.WriteLine(_loc.Get("DeletionCancelled"));
                Pause();
                return;
            }

            _service.DeletePassenger(id, out var removed);
            Console.WriteLine(_loc.Get("PassengerDeleted", id, removed));
            Persist();
            Pause();
        }

        static void DeleteBooking()
        {
            ClearScreen();
            Console.WriteLine(_loc.Get("DeleteBooking"));
            Console.Write(_loc.Get("BookingIdPrompt"));
            if (!int.TryParse(Console.ReadLine(), out var id))
            {
                Console.WriteLine(_loc.Get("InvalidId"));
                Pause();
                return;
            }

            var booking = _service.GetAllBookings().FirstOrDefault(b => b.BookingId == id);
            if (booking is null)
            {
                Console.WriteLine(_loc.Get("BookingNotFoundId", id));
                Pause();
                return;
            }

            if (!ConfirmAction(_loc.Get("ConfirmDeleteBooking", id, booking.FlightNumber, booking.SeatNumber)))
            {
                Console.WriteLine(_loc.Get("DeletionCancelled"));
                Pause();
                return;
            }

            if (_service.DeleteBooking(id))
            {
                Console.WriteLine(_loc.Get("BookingDeleted", id));
                Persist();
            }
            else
            {
                Console.WriteLine(_loc.Get("BookingNotFoundId", id));
            }

            Pause();
        }
    }
}
