using Pick_To_Ride.Attributes;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pick_To_Ride.Models.Entities
{
    public class Booking
    {
        [Key]
        public Guid BookingId { get; set; } = Guid.NewGuid();

        [Required(ErrorMessage = "Please select a car.")]
        public Guid CarId { get; set; }
        [ForeignKey(nameof(CarId))]
        public Car Car { get; set; }

        [Required(ErrorMessage = "Customer is required.")]
        public Guid CustomerId { get; set; }   // must match User.UserId
        [ForeignKey(nameof(CustomerId))]
        public User Customer { get; set; }

        public Guid? DriverId { get; set; }   // Optional StaffId
        [ForeignKey(nameof(DriverId))]
        public Staff Driver { get; set; }

        [Required(ErrorMessage = "Start date is required.")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required.")]
        [DataType(DataType.Date)]
        [DateGreaterThan("StartDate", ErrorMessage = "End date must be after start date.")]
        public DateTime EndDate { get; set; }

        [StringLength(6)]
        public string? BookingCode { get; set; } // generated server-side

        [Required]
        public BookingStatus Status { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        // ✅ Make this optional in DB (nullable) - only required when DriverRequired = true
        public string? PickupLocation { get; set; }

        public bool DriverRequired { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    public enum BookingStatus
    {
        Pending,
        Booked,
        Cancelled,
        Completed
    }
}
