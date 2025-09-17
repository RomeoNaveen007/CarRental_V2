using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Pick_To_Ride.Models.Entities
{
    public class Staff
    {
        [Key]
        public Guid StaffId { get; set; }

        [Required, StringLength(50)]
        public string Availability { get; set; } // e.g., Available / On Duty

        [Range(0, double.MaxValue)]
        public decimal Salary { get; set; }

        // Foreign key to User
        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } // navigation property
    }
}
