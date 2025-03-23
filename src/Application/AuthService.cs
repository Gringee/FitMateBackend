using Domain;
using BCrypt.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.RegularExpressions;

namespace Application;

public class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _config;

    public AuthService(IUserRepository userRepository, IConfiguration config)
    {
        _userRepository = userRepository;
        _config = config;
    }

    public async Task<(bool Success, string ErrorMessage)> RegisterAsync(string username, string email, string password)
    {
        var users = await _userRepository.GetAllAsync();

        if (!IsValidEmail(email))
            return (false, "Invalid email address format.");

        if (users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            return (false, "The email address you entered already exists.");

        if (users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            return (false, "The username you entered already exists.");

        if (!IsPasswordValid(password, out var passwordError))
            return (false, passwordError);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
        };

        await _userRepository.AddAsync(user);

        return (true, string.Empty);
    }

    public async Task<string?> LoginAsync(string email, string password)
    {
        var users = await _userRepository.GetAllAsync();
        var user = users.FirstOrDefault(u => u.Email == email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        return GenerateJwtToken(user);
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim("role", user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["ExpireMinutes"]!)),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private bool IsPasswordValid(string password, out string error)
    {
        if (password.Length < 8)
        {
            error = "The password must be at least 8 characters long.";
            return false;
        }
        if (!password.Any(char.IsUpper))
        {
            error = "The password must contain at least one uppercase letter.";
            return false;
        }
        if (!password.Any(char.IsLower))
        {
            error = "The password must contain at least one lowercase letter.";
            return false;
        }
        if (!password.Any(char.IsDigit))
        {
            error = "The password must contain at least one digit.";
            return false;
        }
        error = string.Empty;
        return true;
    }

    private bool IsValidEmail(string email)
    {
        var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
        return emailRegex.IsMatch(email);
    }
}
