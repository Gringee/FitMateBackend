using Application.Abstractions;
using Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public sealed class UserProfileService : IUserProfileService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPasswordHasher _hasher;

    public UserProfileService(IApplicationDbContext db, ICurrentUserService currentUser, IPasswordHasher hasher)
    {
        _db = db;
        _currentUser = currentUser;
        _hasher = hasher;
    }

    private Guid UserId => _currentUser.UserId;

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
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            TargetWeightKg = user.TargetWeightKg,
            ShareBiometricsWithFriends = user.ShareBiometricsWithFriends
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
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            TargetWeightKg = user.TargetWeightKg,
            ShareBiometricsWithFriends = user.ShareBiometricsWithFriends
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

    public async Task<TargetWeightDto> GetTargetWeightAsync(CancellationToken ct)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        return new TargetWeightDto
        {
            TargetWeightKg = user.TargetWeightKg
        };
    }

    public async Task UpdateTargetWeightAsync(UpdateTargetWeightRequest request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        // Clear target if null or 0
        if (request.TargetWeightKg.HasValue)
        {
            user.TargetWeightKg = request.TargetWeightKg.Value == 0 
                ? null 
                : request.TargetWeightKg.Value;
        }
        else
        {
            user.TargetWeightKg = null;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateBiometricsPrivacyAsync(bool shareWithFriends, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        user.ShareBiometricsWithFriends = shareWithFriends;
        await _db.SaveChangesAsync(ct);
    }
}