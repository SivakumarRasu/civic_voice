using System.ComponentModel.DataAnnotations;

namespace CivicVoice.Web.Validation;

/// <summary>
/// Custom validation attribute demonstrating ASP.NET Core ValidationAttribute.
/// Ensures an occurrence or incident date is not set in the future.
/// </summary>
public class NotFutureDateAttribute : ValidationAttribute
{
    public NotFutureDateAttribute() : base("The date cannot be in the future.")
    {
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            // Null values are handled by [Required] if required
            return ValidationResult.Success;
        }

        if (value is DateTime dateTime)
        {
            if (dateTime > DateTime.UtcNow.AddMinutes(5)) // 5-minute clock drift allowance
            {
                return new ValidationResult(ErrorMessage ?? "The date cannot be in the future.");
            }
        }
        else if (value is DateOnly dateOnly)
        {
            if (dateOnly > DateOnly.FromDateTime(DateTime.UtcNow))
            {
                return new ValidationResult(ErrorMessage ?? "The date cannot be in the future.");
            }
        }

        return ValidationResult.Success;
    }
}
