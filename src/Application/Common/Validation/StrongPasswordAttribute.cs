using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Application.Common.Validation;

/// <summary>
/// Validates password complexity requirements.
/// Password must contain:
/// - At least 8 characters
/// - At least one uppercase letter (A-Z)
/// - At least one lowercase letter (a-z)
/// - At least one digit (0-9)
/// - At least one special character (@$!%*?&amp;#^()_+=-[]{}|:;"'&lt;&gt;,./)
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class StrongPasswordAttribute : ValidationAttribute
{
    private const int MinLength = 8;
    private const int MaxLength = 100;
    
    // Regex pattern for strong password
    // (?=.*[a-z])       - at least one lowercase letter
    // (?=.*[A-Z])       - at least one uppercase letter
    // (?=.*\d)          - at least one digit
    // (?=.*[@$!%*?&])   - at least one special character
    // .{8,100}          - between 8 and 100 characters total
    private const string PasswordPattern = 
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#^()_+\-=\[\]{}|:;""'<>,.\/]).{8,100}$";

    public StrongPasswordAttribute()
    {
        ErrorMessage = "Password must be 8-100 characters and contain at least one uppercase letter, " +
                      "one lowercase letter, one digit, and one special character (@$!%*?&#^()_+-=[]{}|:;\"'<>,.\\/). ";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success; // [Required] handles null check
        }

        var password = value.ToString();
        
        if (string.IsNullOrWhiteSpace(password))
        {
            return ValidationResult.Success; // [Required] handles empty check
        }

        // Check length
        if (password.Length < MinLength)
        {
            return new ValidationResult(
                $"Password must be at least {MinLength} characters long.",
                new[] { validationContext.MemberName ?? "Password" });
        }

        if (password.Length > MaxLength)
        {
            return new ValidationResult(
                $"Password cannot exceed {MaxLength} characters.",
                new[] { validationContext.MemberName ?? "Password" });
        }

        // Check for lowercase letter
        if (!password.Any(char.IsLower))
        {
            return new ValidationResult(
                "Password must contain at least one lowercase letter (a-z).",
                new[] { validationContext.MemberName ?? "Password" });
        }

        // Check for uppercase letter
        if (!password.Any(char.IsUpper))
        {
            return new ValidationResult(
                "Password must contain at least one uppercase letter (A-Z).",
                new[] { validationContext.MemberName ?? "Password" });
        }

        // Check for digit
        if (!password.Any(char.IsDigit))
        {
            return new ValidationResult(
                "Password must contain at least one digit (0-9).",
                new[] { validationContext.MemberName ?? "Password" });
        }

        // Check for special character
        var specialChars = "@$!%*?&#^()_+-=[]{}|:;\"'<>,./\\";
        if (!password.Any(c => specialChars.Contains(c)))
        {
            return new ValidationResult(
                "Password must contain at least one special character (@$!%*?&#^()_+-=[]{}|:;\"'<>,.\\/). ",
                new[] { validationContext.MemberName ?? "Password" });
        }

        // Additional check: no common weak passwords (only after all requirements are met)
        var weakPasswords = new[]
        {
            "Password1!", "Qwerty123!", "Admin123!", "Welcome1!",
            "Passw0rd!", "Abc12345!", "Test1234!"
        };

        if (weakPasswords.Any(weak => password.Equals(weak, StringComparison.OrdinalIgnoreCase)))
        {
            return new ValidationResult(
                "This password is too common. Please choose a stronger password.",
                new[] { validationContext.MemberName ?? "Password" });
        }

        return ValidationResult.Success;
    }

    public override bool IsValid(object? value)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return true; // [Required] handles this
        }

        var password = value.ToString()!;
        return Regex.IsMatch(password, PasswordPattern);
    }
}
