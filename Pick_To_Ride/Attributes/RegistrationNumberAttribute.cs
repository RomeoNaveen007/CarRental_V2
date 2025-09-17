using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Pick_To_Ride.Attributes
{
    public class RegistrationNumberAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var regNumber = value as string;

            if (string.IsNullOrWhiteSpace(regNumber))
                return new ValidationResult("Registration number is required.");

            // Example length check (Sri Lankan plates are usually 6–8 chars)
            if (regNumber.Length < 6 || regNumber.Length > 10)
                return new ValidationResult("Registration number must be between 6 and 10 characters.");

            // Prevent spaces
            if (regNumber.Contains(" "))
                return new ValidationResult("Registration number cannot contain spaces.");

            // Example pattern: UP-1234, WP-1234, CAR-1234 etc.
            var pattern = @"^[A-Z]{1,3}-\d{3,4}$";

            if (!Regex.IsMatch(regNumber, pattern))
                return new ValidationResult("Invalid registration format. Example: WP-1234 or CAR-1234");

            return ValidationResult.Success;
        }
    }
}
