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




//using System.ComponentModel.DataAnnotations;

//namespace Pick_To_Ride.ViewModels
//{

//    public class PaymentViewModel
//    {
//        public Guid PaymentId { get; set; }

//        [Required]
//        public Guid BookingId { get; set; }

//        [Required]
//        [Range(0, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
//        public decimal Amount { get; set; }

//        [Display(Name = "Payment Date")]
//        public DateTime? PaymentDate { get; set; }

//        [Display(Name = "Payment Method")]
//        public string Method { get; set; } = "Card";

//        [Display(Name = "Payment Status")]
//        public string Status { get; set; } = "Pending";

//        [Display(Name = "Created At")]
//        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

//        // Extra UI field: Customer Name (for display in views)
//        public string CustomerName { get; set; }
//    }
//}
