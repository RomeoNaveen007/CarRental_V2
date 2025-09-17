using Pick_To_Ride.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Pick_To_Ride.ViewModels
{
    public class UserViewModel
    {
        public Guid? UserId { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [StringLength(50)]
        public string UserName { get; set; }

        // password is plain here only for input. Do not store it as plain.
        // When editing, leaving this empty => keep existing password.
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string? Password { get; set; }  // nullable

        [LicenceNumber]
        public string LicenceNumber { get; set; }

        [Required(ErrorMessage = "NIC number is required")]
        [SriLankaNic]
        public string NIC { get; set; }

        [Required]
        [CustomEmailAttributes] // your custom validator
        public string Email { get; set; }

        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must be 10 digits and start with 0.")]
        public string? PhoneNumber { get; set; } // nullable

        // For file upload
        public IFormFile? ProfileImageFile { get; set; }

        // URL of existing image (for showing current image in edit)
        public string? ProfileImage { get; set; }

        public string Role { get; set; }

        public bool IsActive { get; set; } = true;

        public string Address { get; set; }

        public string City { get; set; }
    }
}
