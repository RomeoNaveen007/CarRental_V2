using Pick_To_Ride.Attributes;
using Pick_To_Ride.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace Pick_To_Ride.ViewModels
{
    public class StaffViewModel
    {
        // Driver/Staff-specific
        public Guid? StaffId { get; set; }

        [Required(ErrorMessage = "Availability is required")]
        public StaffAvailability Availability { get; set; } = StaffAvailability.Available;

        [Range(0, double.MaxValue, ErrorMessage = "Salary must be non-negative")]
        public decimal Salary { get; set; }

        // User-related
        public Guid? UserId { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required")]
        [StringLength(50)]
        public string UserName { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string? Password { get; set; }

        [Required]
        [CustomEmailAttributes]
        public string Email { get; set; } = string.Empty;

        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must be 10 digits and start with 0.")]
        public string? PhoneNumber { get; set; }

        [SriLankaNic(ErrorMessage = "Invalid NIC number.")]
        [Required(ErrorMessage = "NIC is required")]
        public string NIC { get; set; } = string.Empty;

        public IFormFile? ProfileImageFile { get; set; }
        public string? ProfileImage { get; set; } // final DB path

        public string? ProfileImagePath { get; set; } // temp path for session

        public string? LicenceNumber { get; set; } // Driver-only field

        public string Role { get; set; } = "Driver"; // default role
        public bool IsActive { get; set; } = true;

        [StringLength(250)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }
    }
}
