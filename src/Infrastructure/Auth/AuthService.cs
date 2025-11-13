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
        
        var uname = request.UserName.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.UserName == uname, ct))
            throw new InvalidOperationException("UserName already taken.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = BCryptNet.HashPassword(request.Password),
            FullName = request.FullName?.Trim() ?? string.Empty,
            UserName = uname
        };
        _db.Users.Add(user);

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "User", ct);
        if (role == null)
        {
            role = new Role { Id = Guid.NewGuid(), Name = "User" };
            _db.Roles.Add(role);
        }

        _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });

        // access
        await _db.SaveChangesAsync(ct);
        var roles = new[] { "User" };
        var (access, exp) = _tokens.CreateAccessToken(user, roles);

        // refresh 
        var (rt, rtExp) = _tokens.CreateRefreshToken();
        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = rt,
            ExpiresAtUtc = rtExp
        });
        await _db.SaveChangesAsync(ct);

        return new AuthResponse { AccessToken = access, ExpiresAtUtc = exp, RefreshToken = rt };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var id = request.UserNameOrEmail.Trim();
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u =>
                u.Email == id || u.UserName == id.ToLower(), ct);

        if (user == null || !BCryptNet.Verify(request.Password, user.PasswordHash))
            throw new InvalidOperationException("Invalid credentials.");

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToArray();
        var (access, exp) = _tokens.CreateAccessToken(user, roles);
        var (rt, rtExp) = _tokens.CreateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken { Id = Guid.NewGuid(), UserId = user.Id, Token = rt, ExpiresAtUtc = rtExp });
        await _db.SaveChangesAsync(ct);

        return new AuthResponse { AccessToken = access, ExpiresAtUtc = exp, RefreshToken = rt };
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var rt = await _db.RefreshTokens
            .Include(x => x.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(x =>
                    x.Token == refreshToken &&
                    x.ExpiresAtUtc > now, // zamiast x.IsActive
                ct);

        if (rt == null)
            throw new InvalidOperationException("Invalid refresh token.");

        var roles = rt.User.UserRoles.Select(r => r.Role.Name);
        var (access, exp) = _tokens.CreateAccessToken(rt.User, roles);

        // Możesz też wygenerować nowy refresh token, ale na razie zostawię jak masz:
        return new AuthResponse
        {
            AccessToken = access,
            ExpiresAtUtc = exp,
            RefreshToken = rt.Token // warto coś zwrócić, żeby schema DTO była spójna
        };
    }
    
    public async Task LogoutAsync(LogoutRequestDto request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return; 
        var rt = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, ct);

        if (rt is null)
        {
            return;
        }

        _db.RefreshTokens.Remove(rt);

        await _db.SaveChangesAsync(ct);
    }
}