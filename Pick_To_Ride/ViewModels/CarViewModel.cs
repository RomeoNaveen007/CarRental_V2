using Pick_To_Ride.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Pick_To_Ride.ViewModels
{
    public class CarViewModel
    {
        [Key]
        public Guid CarId { get; set; }

        [Required]
        public string CarName { get; set; }

        [Required]
        public string Brand { get; set; }

        [RegistrationNumber]
        public string RegistrationNumber { get; set; }

        [Required]
        public string FuelType { get; set; }

        [Required]
        public string Transmission { get; set; }

        [Required]
        public string Seats { get; set; }

        [Required]
        public decimal DailyRate { get; set; }

        public string? ImageURL { get; set; }
        public string? ImageLogo { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string Status { get; set; }

        [Required]
        public string Category { get; set; }

        // 🔹 For AddCar: Required, For UpdateCar: Optional
        public IFormFile? ImageFile { get; set; }
        public IFormFile? ImageLogoFile { get; set; }
    }
}
