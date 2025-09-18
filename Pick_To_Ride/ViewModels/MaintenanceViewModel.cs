using System.ComponentModel.DataAnnotations;

namespace Pick_To_Ride.ViewModels
{
    public class MaintenanceViewModel
    {
        public Guid? MaintenenceId { get; set; }

        [Required]
        public Guid CarId { get; set; }

        [Required]
        public Guid ReportedBy { get; set; }

        [Required, StringLength(100)]
        public string MaintainenceType { get; set; }

        [Required, StringLength(1000)]
        public string Description { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Required, StringLength(50)]
        public string Status { get; set; } = "In Progress";

        [Range(0, double.MaxValue)]
        public decimal Cost { get; set; } = 0;
    }
}
