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

    }
}
