using System.ComponentModel.DataAnnotations;
using Application.Common;
using Application.Common.Validation;

namespace Application.DTOs;

/// <summary>
/// Represents a user in the system.
/// </summary>
public class UserDto
{
    /// <summary>
    /// Unique identifier of the user.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Full name of the user.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Email address of the user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Unique username.
    /// </summary>
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
    [StrongPassword]
    public string NewPassword { get; set; } = string.Empty;
}