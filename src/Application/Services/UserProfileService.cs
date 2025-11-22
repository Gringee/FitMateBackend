using Application.Abstractions;
using Application.DTOs;
using Application.Common.Security; // Extension
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public sealed class UserProfileService : IUserProfileService
{
    private readonly IApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly IPasswordHasher _hasher;

    public UserProfileService(IApplicationDbContext db, IHttpContextAccessor http, IPasswordHasher hasher)
    {
        _db = db;
        _http = http;
        _hasher = hasher;
    }

    private Guid UserId => _http.HttpContext?.User.GetUserId() 
                           ?? throw new UnauthorizedAccessException();

    public async Task<UserProfileDto> GetCurrentAsync(CancellationToken ct)
    {
        var user = await _db.Users
                       .AsNoTracking()
                       .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                       .FirstOrDefaultAsync(u => u.Id == UserId, ct)
                   ?? throw new KeyNotFoundException("User not found.");

        return new UserProfileDto
        {
            Id = user.Id,
            UserName = user.UserName,
            FullName = user.FullName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
        };
    }

    public async Task<UserProfileDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct)
    {
        var id = UserId;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
                   ?? throw new KeyNotFoundException("User not found.");
        
        var normUserName = request.UserName.Trim();
        if (await _db.Users.AnyAsync(u => u.Id != id && u.UserName == normUserName, ct))
            throw new InvalidOperationException("This username is already taken.");
        user.UserName = normUserName;
        
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var normEmail = request.Email.Trim().ToLowerInvariant();
            if (await _db.Users.AnyAsync(u => u.Id != id && u.Email.ToLower() == normEmail, ct))
                throw new InvalidOperationException("This email is already in use.");
            user.Email = normEmail;
        }

        if (!string.IsNullOrWhiteSpace(request.FullName))
            user.FullName = request.FullName.Trim();

        await _db.SaveChangesAsync(ct);
        
        await _db.Entry(user).Collection(u => u.UserRoles).Query().Include(ur => ur.Role).LoadAsync(ct);

        return new UserProfileDto
        {
            Id = user.Id,
            UserName = user.UserName,
            FullName = user.FullName ?? "",
            Email = user.Email ?? "",
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
        };
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");
        
        if (!_hasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            throw new InvalidOperationException("Current password is incorrect.");

        user.PasswordHash = _hasher.HashPassword(request.NewPassword);
        await _db.SaveChangesAsync(ct);
    }
}