using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Pick_To_Ride.Attributes
{
    public class CustomEmailAttributes : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var email = value as string;

            if (string.IsNullOrWhiteSpace(email))
                return new ValidationResult("Email is required.");

            if (email.Length < 3)
                return new ValidationResult("Email is too short.");

            if (email.Contains(" "))
                return new ValidationResult("Email cannot contain spaces.");

            var pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";

            if (!Regex.IsMatch(email, pattern))
                return new ValidationResult("Invalid email format. Example: name@example.com");

            if (email.Contains(".."))
                return new ValidationResult("Email cannot contain consecutive dots.");

            var localPart = email.Split('@')[0];
            if (localPart.StartsWith(".") || localPart.EndsWith("."))
                return new ValidationResult("Local part cannot start or end with a dot.");

            return ValidationResult.Success;
        }
    }
}
