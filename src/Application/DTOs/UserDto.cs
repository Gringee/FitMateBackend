using System.ComponentModel.DataAnnotations;
using Application.Common;

namespace Application.DTOs;

/// <summary>
/// Represents a user in the system.
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}

/// <summary>
/// Data transfer object for creating a new user.
/// </summary>
public sealed class CreateUserDto
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;
    
    [Required, StringLength(ValidationConstants.UserNameMaxLength, MinimumLength = ValidationConstants.UserNameMinLength)]
    [RegularExpression(ValidationConstants.UserNamePattern, ErrorMessage = ValidationConstants.UserNameErrorMessage)]
    public string UserName { get; set; } = string.Empty;
}

/// <summary>
/// Data transfer object for updating an existing user.
/// </summary>
public sealed class UpdateUserDto
{
    [StringLength(100, MinimumLength = 1)]
    public string? FullName { get; set; }

    [EmailAddress, StringLength(200)]
    public string? Email { get; set; }

    [StringLength(ValidationConstants.UserNameMaxLength, MinimumLength = ValidationConstants.UserNameMinLength)]
    [RegularExpression(ValidationConstants.UserNamePattern, ErrorMessage = ValidationConstants.UserNameErrorMessage)]
    public string? UserName { get; set; }
}

/// <summary>
/// Data transfer object for resetting a user's password.
/// </summary>
public sealed class ResetPasswordDto
{
    [Required]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
    public string NewPassword { get; set; } = string.Empty;
}