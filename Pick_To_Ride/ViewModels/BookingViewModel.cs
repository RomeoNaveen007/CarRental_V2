using Pick_To_Ride.Attributes;
using Pick_To_Ride.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace Pick_To_Ride.ViewModels
{
    public class BookingViewModel : IValidatableObject
    {
        public Guid BookingId { get; set; }

        [Required(ErrorMessage = "Please select a car.")]
        public Guid CarId { get; set; }

        // Will be set on server from authenticated user if not provided
        public Guid CustomerId { get; set; }

        // Optional driver
        public Guid? DriverId { get; set; }

        [Required(ErrorMessage = "Start date is required.")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required.")]
        [DataType(DataType.Date)]
        [DateGreaterThan("StartDate", ErrorMessage = "End date must be after start date.")]
        public DateTime EndDate { get; set; }

        [Required]
        [StringLength(6)]
        public string BookingCode { get; set; }

        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        [Required(ErrorMessage = "Total amount is required.")]
        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        [StringLength(200)]
        public string PickupLocation { get; set; } // conditionally required

        public bool DriverRequired { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // For drop-downs in views
        public IEnumerable<Models.Entities.Car> AvailableCars { get; set; }
        public IEnumerable<Models.Entities.Staff> AvailableDrivers { get; set; }

        // Server-side conditional validation
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DriverRequired)
            {
                if (string.IsNullOrWhiteSpace(PickupLocation))
                {
                    yield return new ValidationResult(
                        "Pickup location is required when a driver is requested.",
                        new[] { nameof(PickupLocation) });
                }
            }

            if (StartDate.Date < DateTime.UtcNow.Date)
            {
                yield return new ValidationResult(
                    "Start date cannot be in the past.",
                    new[] { nameof(StartDate) });
            }

            if (EndDate < StartDate)
            {
                yield return new ValidationResult(
                    "End date must be after start date.",
                    new[] { nameof(EndDate) });
            }

            // If DriverId selected, ensure DriverRequired == true (help protect against inconsistent UI)
            if (DriverId.HasValue && !DriverRequired)
            {
                yield return new ValidationResult(
                    "You selected a driver but did not mark 'Driver required'.",
                    new[] { nameof(DriverId), nameof(DriverRequired) });
            }
        }
    }
}