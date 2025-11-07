using Application.Abstractions;
using Application.DTOs.Auth;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using BCryptNet = BCrypt.Net.BCrypt;

namespace Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokens;

    public AuthService(AppDbContext db, ITokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var exists = await _db.Users.AnyAsync(u => u.Email == request.Email, ct);
        if (exists) throw new InvalidOperationException("Email already registered.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = BCryptNet.HashPassword(request.Password),
            FullName = request.FullName
        };
        _db.Users.Add(user);
        
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "User", ct);
        if (role is null)
        {
            role = new Role { Id = Guid.NewGuid(), Name = "User" };
            _db.Roles.Add(role);
            await _db.SaveChangesAsync(ct); // zapisz, aby mieć Role.Id w DB
        }

        _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });

        await _db.SaveChangesAsync(ct);

        var roles = new[] { "User" };
        var (access, exp) = _tokens.CreateAccessToken(user, roles);

        return new AuthResponse { AccessToken = access, ExpiresAtUtc = exp };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email, ct);

        if (user == null || !BCryptNet.Verify(request.Password, user.PasswordHash))
            throw new InvalidOperationException("Invalid credentials.");

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToArray();
        var (access, exp) = _tokens.CreateAccessToken(user, roles);

        return new AuthResponse { AccessToken = access, ExpiresAtUtc = exp };
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var rt = await _db.RefreshTokens
            .Include(x => x.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(x => x.Token == refreshToken && x.IsActive, ct);

        if (rt == null) throw new InvalidOperationException("Invalid refresh token.");

        var roles = rt.User.UserRoles.Select(r => r.Role.Name);
        var (access, exp) = _tokens.CreateAccessToken(rt.User, roles);

        return new AuthResponse { AccessToken = access, ExpiresAtUtc = exp };
    }
}