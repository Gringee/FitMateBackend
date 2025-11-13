namespace Application.DTOs.Auth;

public class RegisterRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = null!;
}

public class LoginRequest
{
    public string UserNameOrEmail { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class AuthResponse
{
    public string AccessToken { get; set; } = null!;
    public DateTime ExpiresAtUtc { get; set; }
    public string RefreshToken { get; set; } = null!; 
}