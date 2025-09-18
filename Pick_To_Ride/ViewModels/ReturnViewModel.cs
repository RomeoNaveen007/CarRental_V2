using System.ComponentModel.DataAnnotations;

namespace Pick_To_Ride.ViewModels
{
    public class ReturnViewModel
    {
        [Required]
        public Guid BookingId { get; set; }

        [Required]
        public string CarCondition { get; set; }

        [Range(0, double.MaxValue)]
        public decimal ExtraCharge { get; set; }
    }
}
