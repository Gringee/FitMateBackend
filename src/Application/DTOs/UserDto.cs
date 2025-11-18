using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}

public class CreateUserDto
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;
    
    [Required, StringLength(50, MinimumLength = 3)]
    [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "UserName may contain only letters, digits, dot, underscore, and hyphen.")]
    public string UserName { get; set; } = string.Empty;
}

public class UpdateUserDto
{
    [StringLength(100, MinimumLength = 1)]
    public string? FullName { get; set; }

    [EmailAddress, StringLength(200)]
    public string? Email { get; set; }

    [StringLength(50, MinimumLength = 3)]
    [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "UserName may contain only letters, digits, dot, underscore, and hyphen.")]
    public string? UserName { get; set; }
}

public class ResetPasswordDto
{
    [Required]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
    public string NewPassword { get; set; } = string.Empty;
}