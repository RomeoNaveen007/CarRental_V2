using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Pick_To_Ride.Attributes
{
    public class SriLankaNicAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var nic = value as string;
            if (string.IsNullOrWhiteSpace(nic))
                return new ValidationResult("NIC is required.");

            nic = nic.Trim();

            // Accept old (9 digits + V/X) OR new (12 digits)
            var pattern = @"^(\d{9}[vVxX]|\d{12})$";
            if (!Regex.IsMatch(nic, pattern))
                return new ValidationResult("NIC must be 9 digits + V/X (e.g. 123456789V) or 12 digits.");

            return ValidationResult.Success;
        }
    }
}
