using Pick_To_Ride.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Pick_To_Ride.ViewModels
{
    public class HandOverViewModel
    {
        [Required]
        public Guid BookingId { get; set; }

        [Required]
        public string CustomerName { get; set; }

        [Required]
        [SriLankaNic]
        public string NIC { get; set; }

        [Required]
        public string LicenceNumber { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string BookingCode { get; set; }
    }
}
