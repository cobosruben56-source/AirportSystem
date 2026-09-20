using System;
using System.Collections.Generic;
using System.Linq;
using AirportSystem.Enums;
using AirportSystem.Models;
using AirportSystem.Services;
using AirportSystem.Utilities;

namespace AirportSystem
{
    class Program
    {
        private static AirportService _service = new();

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var (flights, passengers, bookings) = DataStorage.LoadData();
            if (flights != null && passengers != null && bookings != null)
            {
              
                Console.WriteLine("Saved data found. Loading... (will seed fresh data for this demo)");
                _service.SeedData();
            }
            else
            {
                Console.WriteLine("No saved data found. Seeding initial data...");
                _service.SeedData();
            }

            bool exit = false;
            while (!exit)
            {
                Console.Clear();
                Console.WriteLine("========================================");
                Console.WriteLine("          AIRPORT MANAGEMENT SYSTEM    ");
                Console.WriteLine("========================================");
                Console.WriteLine("1. List all flights / register new flight");
                Console.WriteLine("2. Search flights");
                Console.WriteLine("3. Register passenger");
                Console.WriteLine("4. Book a ticket");
                Console.WriteLine("5. Cancel booking");
                Console.WriteLine("6. View my bookings");
                Console.WriteLine("7. Check-in");
                Console.WriteLine("8. Advance time (simulate)");
                Console.WriteLine("9. Show seat map for a flight");
                Console.WriteLine("0. Save & Exit");
                Console.Write("\nYour choice: ");

                string? choice = Console.ReadLine();
                switch (choice)
                {
                    case "1": ListAllFlights(); break;
                    case "2": SearchFlights(); break;
                    case "3": RegisterPassenger(); break;
                    case "4": BookTicket(); break;
                    case "5": CancelBooking(); break;
                    case "6": ViewMyBookings(); break;
                    case "7": CheckIn(); break;
                    case "8": AdvanceTime(); break;
                    case "9": ShowSeatMap(); break;
                    case "0":
                        DataStorage.SaveData(_service);
                        Console.WriteLine("Data saved. Goodbye!");
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("Invalid option. Press any key to continue.");
                        Console.ReadKey();
                        break;
                }
            }
        }


        static void ListAllFlights()
        {
            Console.Clear();
            Console.WriteLine("=== ALL FLIGHTS ===");
            var flights = _service.GetAllFlights();
            if (!flights.Any())
            {
                Console.WriteLine("No flights available.");
                Console.ReadKey();
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
            Console.ReadKey();
            return;
        }

        private static void RegisterFlight()
        {
            Console.Clear();
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {ex.Message}");
            }
        }

        static void SearchFlights()
        {
            Console.Clear();
            Console.WriteLine("=== SEARCH FLIGHTS ===");
            Console.Write("Origin (or leave empty): ");
            string? origin = Console.ReadLine();
            Console.Write("Destination (or leave empty): ");
            string? dest = Console.ReadLine();

            var results = _service.SearchFlights(origin, dest, null);
            if (!results.Any())
            {
                Console.WriteLine("No flights found.");
                Console.ReadKey();
                return;
            }

            foreach (var f in results)
            {
                Console.WriteLine($"{f.FlightNumber} | {f.Origin}->{f.Destination} | Dep: {f.DepartureTime:g} | " +
                                  $"Seats: {f.AvailableSeats}/{f.TotalSeats} | {f.Status}");
            }
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        static void RegisterPassenger()
        {
            Console.Clear();
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
                Console.ReadKey();
                return;
            }

            var p = _service.RegisterPassenger(fn, ln, email ?? "", phone ?? "");
            Console.WriteLine($"Passenger registered with ID: {p.Id}");
            Console.ReadKey();
        }

        static void BookTicket()
        {
            Console.Clear();
            Console.WriteLine("=== BOOK A TICKET ===");
            Console.Write("Flight number: ");
            string? fn = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(fn))
            {
                Console.WriteLine("Flight number required.");
                Console.ReadKey();
                return;
            }

            Console.Write("Passenger ID: ");
            if (!int.TryParse(Console.ReadLine(), out int pid))
            {
                Console.WriteLine("Invalid ID.");
                Console.ReadKey();
                return;
            }

            // Show available seats
            var flight = _service.GetAllFlights().FirstOrDefault(f => f.FlightNumber == fn);
            if (flight == null)
            {
                Console.WriteLine("Flight not found.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"\nAvailable seats for {fn}:");
            var avail = flight.GetAvailableSeats();
            if (!avail.Any())
            {
                Console.WriteLine("No seats available.");
                Console.ReadKey();
                return;
            }
            Console.WriteLine(string.Join(", ", avail.Take(20)) + (avail.Count > 20 ? " ..." : ""));

            Console.Write("Select seat number: ");
            string? seat = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(seat))
            {
                Console.WriteLine("Invalid seat.");
                Console.ReadKey();
                return;
            }

            var result = _service.BookTicket(fn, pid, seat);
            if (result.success)
            {
                Console.WriteLine($"✅ {result.message}");
            }
            else
            {
                Console.WriteLine($"❌ {result.message}");
            }
            Console.ReadKey();
        }

        static void CancelBooking()
        {
            Console.Clear();
            Console.WriteLine("=== CANCEL BOOKING ===");
            Console.Write("Booking ID: ");
            if (!int.TryParse(Console.ReadLine(), out int bid))
            {
                Console.WriteLine("Invalid ID.");
                Console.ReadKey();
                return;
            }

            if (_service.CancelBooking(bid))
                Console.WriteLine("✅ Booking cancelled.");
            else
                Console.WriteLine("❌ Booking not found.");
            Console.ReadKey();
        }

        static void ViewMyBookings()
        {
            Console.Clear();
            Console.WriteLine("=== MY BOOKINGS ===");
            Console.Write("Passenger ID: ");
            if (!int.TryParse(Console.ReadLine(), out int pid))
            {
                Console.WriteLine("Invalid ID.");
                Console.ReadKey();
                return;
            }

            var bookings = _service.GetBookingsForPassenger(pid);
            if (!bookings.Any())
            {
                Console.WriteLine("No bookings found.");
                Console.ReadKey();
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
            Console.ReadKey();
        }

        static void CheckIn()
        {
            Console.Clear();
            Console.WriteLine("=== CHECK-IN ===");
            Console.Write("Booking ID: ");
            if (!int.TryParse(Console.ReadLine(), out int bid))
            {
                Console.WriteLine("Invalid ID.");
                Console.ReadKey();
                return;
            }

            if (_service.CheckIn(bid))
            {
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
            Console.ReadKey();
        }

        static void AdvanceTime()
        {
            Console.Clear();
            Console.WriteLine("=== ADVANCE TIME (SIMULATION) ===");
            Console.Write("Enter hours to advance: ");
            if (!double.TryParse(Console.ReadLine(), out double hours))
            {
                Console.WriteLine("Invalid input.");
                Console.ReadKey();
                return;
            }

            _service.AdvanceTime(TimeSpan.FromHours(hours));
            Console.WriteLine($"Time advanced by {hours} hour(s). Flight statuses updated.");
            Console.ReadKey();
        }

        static void ShowSeatMap()
        {
            Console.Clear();
            Console.WriteLine("=== SEAT MAP ===");
            Console.Write("Flight number: ");
            string? fn = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(fn))
            {
                Console.WriteLine("Invalid flight.");
                Console.ReadKey();
                return;
            }

            var flight = _service.GetAllFlights().FirstOrDefault(f => f.FlightNumber == fn);
            if (flight == null)
            {
                Console.WriteLine("Flight not found.");
                Console.ReadKey();
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
            Console.ReadKey();
        }
    }
}