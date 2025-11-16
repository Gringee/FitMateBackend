using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth;

public class RegisterRequest
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
    [StringLength(50, MinimumLength = 3,
        ErrorMessage = "UserName must be between 3 and 50 characters.")]
    [RegularExpression("^[a-zA-Z0-9_.-]+$",
        ErrorMessage = "UserName may contain only letters, digits, '.', '_' and '-'.")]
    public string UserName { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required]
    [StringLength(200)]
    public string UserNameOrEmail { get; set; } = string.Empty;

    [Required]
    [StringLength(100,
        ErrorMessage = "Password is too long.")]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutRequestDto
{
    [Required]
    [StringLength(512)]
    public string RefreshToken { get; set; } = string.Empty;
}

public class RefreshRequestDto
{
    [Required]
    [StringLength(512)]
    public string RefreshToken { get; set; } = string.Empty;
}