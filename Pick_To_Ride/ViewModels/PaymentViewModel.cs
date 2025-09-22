using System;
using System.ComponentModel.DataAnnotations;

namespace Pick_To_Ride.ViewModels
{
    public class PaymentViewModel
    {
        public Guid PaymentId { get; set; }

        [Required]
        public Guid BookingId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        public DateTime? PaymentDate { get; set; }

        public string Method { get; set; } = "Card";

        public string Status { get; set; }
        public string BookingDetails { get; set; }
    }
}