using System.ComponentModel.DataAnnotations;

namespace Pick_To_Ride.Models.Entities
{
    public class HandOverRecord
    {
        [Key]
        public Guid HandOverId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid BookingId { get; set; }

        [Required]
        public Guid UserId { get; set; } // User who receives the car (customer) or driver handing over

        [Required]
        public DateTime HandOverDate { get; set; } = DateTime.UtcNow;


    }
}
