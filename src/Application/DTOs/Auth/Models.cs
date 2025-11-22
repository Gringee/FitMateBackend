using System.ComponentModel.DataAnnotations;
using Application.Common;

namespace Application.DTOs.Auth;

/// <summary>
/// Request to register a new user.
/// </summary>
public sealed class RegisterRequest
{
    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8,
        ErrorMessage = "Password must be between 8 and 100 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [StringLength(100,
        ErrorMessage = "FullName cannot be longer than 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(ValidationConstants.UserNameMaxLength, MinimumLength = ValidationConstants.UserNameMinLength,
        ErrorMessage = "UserName must be between 3 and 50 characters.")]
    [RegularExpression(ValidationConstants.UserNamePattern,
        ErrorMessage = ValidationConstants.UserNameErrorMessage)]
    public string UserName { get; set; } = string.Empty;
}

/// <summary>
/// Request to log in a user.
/// </summary>
public sealed class LoginRequest
{
    [Required]
    [StringLength(200)]
    public string UserNameOrEmail { get; set; } = string.Empty;

    [Required]
    [StringLength(100,
        ErrorMessage = "Password is too long.")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Response containing authentication tokens.
/// </summary>
public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Request to log out a user.
/// </summary>
public sealed class LogoutRequestDto
{
    [Required]
    [StringLength(512)]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Request to refresh an authentication token.
/// </summary>
public sealed class RefreshRequestDto
{
    [Required]
    [StringLength(512)]
    public string RefreshToken { get; set; } = string.Empty;
}