using Pick_To_Ride.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Pick_To_Ride.Models.Entities
{
    public class User
    {
        [Key]
        public Guid UserId { get; set; }

        [Required, StringLength(200)]
        public string FullName { get; set; }

        [Required, StringLength(50)]
        public string UserName { get; set; }

        [DataType(DataType.Password)]
        public string PasswordHash { get; set; }

        [LicenceNumber]
        public string LicenceNumber { get; set; }

        [Required(ErrorMessage = "NIC number is required")]
        [SriLankaNic]
        public string NIC { get; set; }

        [StringLength(50)]
        public string Role { get; set; }

        [Required, StringLength(200)]
        public string Email { get; set; }

        [StringLength(20)]
        public string PhoneNumber { get; set; }

        public string? ProfileImage { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public string Address { get; set; }
        public string City { get; set; }
    }
}
