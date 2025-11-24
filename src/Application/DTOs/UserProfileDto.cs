using System.ComponentModel.DataAnnotations;
using Application.Common;
using Application.Common.Validation;

namespace Application.DTOs;

public sealed class UserProfileDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = null!;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = new List<string>();
}

public sealed class UpdateProfileRequest
{
    [Required]
    [StringLength(ValidationConstants.UserNameMaxLength, MinimumLength = ValidationConstants.UserNameMinLength)] 
    [RegularExpression(ValidationConstants.UserNamePattern, ErrorMessage = ValidationConstants.UserNameErrorMessage)]
    public string UserName { get; set; } = null!;

    [StringLength(100)] 
    public string? FullName { get; set; }

    [EmailAddress, StringLength(200)]
    public string? Email { get; set; }  
}

public sealed class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = null!;

    [Required]
    [StrongPassword]
    public string NewPassword { get; set; } = null!;

    [Required]
    [Compare(nameof(NewPassword), ErrorMessage = "ConfirmPassword must match NewPassword.")]
    public string ConfirmPassword { get; set; } = null!;
}