using System.ComponentModel.DataAnnotations;

namespace Pick_To_Ride.ViewModels
{
    public class BookingExtensionViewModel
    {
        public Guid BookingId { get; set; }

        [Required(ErrorMessage = "New end date is required.")]
        [DataType(DataType.Date)]
        public DateTime NewEndDate { get; set; }

        [Required(ErrorMessage = "Reason is required.")]
        [StringLength(500)]
        public string Reason { get; set; }

        public string Status { get; set; } = "Pending";
    }
}
