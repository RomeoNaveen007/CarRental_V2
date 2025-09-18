using System.ComponentModel.DataAnnotations;

namespace Pick_To_Ride.Models.Entities
{
    public class ReturnRecord
    {
        [Key]
        public Guid ReturnId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid BookingId { get; set; }

        [Required]
        public DateTime ReturnDate { get; set; } = DateTime.UtcNow;

        [Required]
        public string CarCondition { get; set; }

        [Range(0, double.MaxValue)]
        public decimal ExtraCharge { get; set; } // For damage or late return
    }
}
