using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public sealed class UserProfileDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = null!;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

public sealed class UpdateProfileRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)] 
    [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "UserName cannot contain spaces.")]
    public string UserName { get; set; } = null!;

    [StringLength(100)] 
    public string? FullName { get; set; }

    [EmailAddress, StringLength(200)]
    public string? Email { get; set; }  
}

public sealed class ChangePasswordRequest : IValidatableObject
{
    [Required]
    public string CurrentPassword { get; set; } = null!;

    [Required]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "NewPassword must have at least 8 characters.")]
    public string NewPassword { get; set; } = null!;

    [Required]
    [Compare(nameof(NewPassword), ErrorMessage = "ConfirmPassword must match NewPassword.")]
    public string ConfirmPassword { get; set; } = null!;

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (!string.IsNullOrEmpty(NewPassword))
        {
            if (!NewPassword.Any(char.IsLetter) || !NewPassword.Any(char.IsDigit))
            {
                yield return new ValidationResult(
                    "NewPassword must contain at least one letter and one digit.",
                    new[] { nameof(NewPassword) });
            }
        }
    }
}