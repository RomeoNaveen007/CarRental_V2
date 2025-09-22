using Microsoft.EntityFrameworkCore;
using Pick_To_Ride.Models.Entities;

namespace Pick_To_Ride.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        public DbSet<Car> Cars { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<HandOverRecord> HandOverRecords { get; set; }
        public DbSet<ReturnRecord> ReturnRecords { get; set; }
        public DbSet<BookingExtentionRequest> BookingExtentionRequests { get; set; }
        public DbSet<Maintenence> Maintenences { get; set; }
        public DbSet<DriverSchedule> DriverSchedules { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Optional: configure relationships
            builder.Entity<Staff>()
                   .HasOne(s => s.User)
                   .WithMany()
                   .HasForeignKey(s => s.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Booking>()
                   .HasOne(b => b.Customer)
                   .WithMany()
                   .HasForeignKey(b => b.CustomerId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Booking>()
                   .HasOne(b => b.Driver)
                   .WithMany()
                   .HasForeignKey(b => b.DriverId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
