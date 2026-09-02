namespace WarningSystems.Models.Validation;

using System.ComponentModel.DataAnnotations;

public class FutureWeekdayAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not DateOnly date)
            return new ValidationResult("Invalid date.");

        var today = DateOnly.FromDateTime(DateTime.Now);

        if (date <= today)
            return new ValidationResult("Due date must be in the future.");

        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return new ValidationResult("Due date cannot be on a weekend.");

        return ValidationResult.Success;
    }
}