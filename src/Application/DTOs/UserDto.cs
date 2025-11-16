using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}

public class CreateUserDto : IValidatableObject
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;
    
    [Required, StringLength(50, MinimumLength = 3)]
    [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "UserName may contain only letters, digits, dot, underscore, and hyphen.")]
    public string UserName { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (UserName.Contains(' '))
            yield return new ValidationResult("UserName cannot contain spaces.", new[] { nameof(UserName) });

        if (FullName.Trim().Length == 0)
            yield return new ValidationResult("FullName cannot be empty after trimming.", new[] { nameof(FullName) });
    }
}

public class UpdateUserDto : IValidatableObject
{
    [StringLength(100, MinimumLength = 1)]
    public string? FullName { get; set; }

    [EmailAddress, StringLength(200)]
    public string? Email { get; set; }

    [StringLength(50, MinimumLength = 3)]
    [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "UserName may contain only letters, digits, dot, underscore, and hyphen.")]
    public string? UserName { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (UserName is { } u && u.Contains(' '))
            yield return new ValidationResult("UserName cannot contain spaces.", new[] { nameof(UserName) });

        if (FullName is { } f && f.Trim().Length == 0)
            yield return new ValidationResult("FullName cannot be empty after trimming.", new[] { nameof(FullName) });
    }
}

public class ResetPasswordDto
{
    [Required]
    [StringLength(100, MinimumLength = 8,
        ErrorMessage = "Password must be at least 8 characters long.")]
    public string NewPassword { get; set; } = string.Empty;
}