using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Pick_To_Ride.Attributes
{
    public class LicenceNumberAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var lic = (value as string)?.Trim();
            if (string.IsNullOrEmpty(lic))
                return ValidationResult.Success; // or return required if you want to force it

            if (lic.Length < 5 || lic.Length > 20)
                return new ValidationResult("Licence number must be between 5 and 20 characters.");

            if (!Regex.IsMatch(lic, @"^[A-Za-z0-9\/\-]+$"))
                return new ValidationResult("Licence number may contain only letters, digits, '/' and '-'.");

            return ValidationResult.Success;
        }
    }
}
