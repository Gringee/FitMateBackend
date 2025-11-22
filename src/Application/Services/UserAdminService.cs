using Application.Abstractions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public sealed class UserAdminService : IUserAdminService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;

    public UserAdminService(IApplicationDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<AssignRoleResult> AssignRoleAsync(Guid userId, string roleName, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            return AssignRoleResult.UserNotFound;

        var role = await _db.Roles
            .FirstOrDefaultAsync(r => r.Name == roleName, ct);

        if (role is null)
            return AssignRoleResult.RoleNotFound;

        if (user.UserRoles.Any(ur => ur.RoleId == role.Id))
            return AssignRoleResult.AlreadyHasRole;

        _db.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        });

        await _db.SaveChangesAsync(ct);
        return AssignRoleResult.Ok;
    }

    public async Task<DeleteUserResult> DeleteUserAsync(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            return DeleteUserResult.NotFound;

        var isAdmin = user.UserRoles.Any(ur => ur.Role.Name == "Admin");
        if (isAdmin)
            return DeleteUserResult.IsAdmin;

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);

        return DeleteUserResult.Ok;
    }

    public async Task<ResetPasswordResult> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return ResetPasswordResult.NotFound;

        user.PasswordHash = _hasher.HashPassword(newPassword);
        await _db.SaveChangesAsync(ct);

        return ResetPasswordResult.Ok;
    }
}