using Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public sealed class UserValidationHelpers : IUserValidationHelpers
{
    private readonly IApplicationDbContext _db;

    public UserValidationHelpers(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task EnsureEmailIsUniqueAsync(string email, CancellationToken ct, Guid? excludeUserId = null)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var query = _db.Users.Where(u => u.Email.ToLower() == normalized);
        
        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        if (await query.AnyAsync(ct))
        {
            throw new InvalidOperationException($"Email '{email}' is already in use.");
        }
    }

    public async Task EnsureUserNameIsUniqueAsync(string userName, CancellationToken ct, Guid? excludeUserId = null)
    {
        var normalized = userName.Trim();
        var query = _db.Users.Where(u => u.UserName == normalized);
        
        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        if (await query.AnyAsync(ct))
        {
            throw new InvalidOperationException($"UserName '{userName}' is already taken.");
        }
    }
}
