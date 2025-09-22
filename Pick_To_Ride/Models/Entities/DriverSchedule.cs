using System.ComponentModel.DataAnnotations;

namespace Pick_To_Ride.Models.Entities
{
    public class DriverSchedule
    {
        [Key]
        public Guid DriverScheduleId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid StaffId { get; set; }

        // Navigation (optional)
        public Staff Staff { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        // Link to booking (optional)
        public Guid? BookingId { get; set; }

    }
}
