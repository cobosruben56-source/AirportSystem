using Microsoft.EntityFrameworkCore;
using AirportSystem.Models;

namespace AirportSystem.Utilities
{
    public class AppDbContext : DbContext
    {
        public DbSet<Flight> Flights { get; set; }
        public DbSet<Passenger> Passengers { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Database=AirportDB;Username=postgres;Password=Ruben789lobo");
        }
    }
}