using System.ComponentModel.DataAnnotations;
using Application.Common;
using Application.Common.Validation;

namespace Application.DTOs;

/// <summary>
/// Represents the profile of a user.
/// </summary>
public sealed class UserProfileDto
{
    /// <summary>
    /// Unique identifier of the user.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Unique username.
    /// </summary>
    public string UserName { get; set; } = null!;

    /// <summary>
    /// Full name of the user.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Email address of the user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// List of roles assigned to the user.
    /// </summary>
    public IReadOnlyList<string> Roles { get; set; } = new List<string>();
}

/// <summary>
/// Request to update user profile information.
/// </summary>
public sealed class UpdateProfileRequest
{
    /// <summary>
    /// New username.
    /// </summary>
    [Required]
    [StringLength(ValidationConstants.UserNameMaxLength, MinimumLength = ValidationConstants.UserNameMinLength)] 
    [RegularExpression(ValidationConstants.UserNamePattern, ErrorMessage = ValidationConstants.UserNameErrorMessage)]
    public string UserName { get; set; } = null!;

    /// <summary>
    /// New full name.
    /// </summary>
    [StringLength(100)] 
    public string? FullName { get; set; }

    /// <summary>
    /// New email address.
    /// </summary>
    [EmailAddress, StringLength(200)]
    public string? Email { get; set; }  
}

/// <summary>
/// Request to change user password.
/// </summary>
public sealed class ChangePasswordRequest
{
    /// <summary>
    /// Current password for verification.
    /// </summary>
    [Required]
    public string CurrentPassword { get; set; } = null!;

    /// <summary>
    /// New password.
    /// </summary>
    [Required]
    [StrongPassword]
    public string NewPassword { get; set; } = null!;

    /// <summary>
    /// Confirmation of the new password.
    /// </summary>
    [Required]
    [Compare(nameof(NewPassword), ErrorMessage = "ConfirmPassword must match NewPassword.")]
    public string ConfirmPassword { get; set; } = null!;
}