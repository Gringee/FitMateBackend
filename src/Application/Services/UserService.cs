using Application.Abstractions;
using Application.DTOs;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public sealed class UserService : IUserService
{
    private readonly IApplicationDbContext _db;
    private readonly IUserValidationHelpers _validationHelpers;

    public UserService(IApplicationDbContext db, IUserValidationHelpers validationHelpers)
    {
        _db = db;
        _validationHelpers = validationHelpers;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(string? search, CancellationToken ct)
    {
        var query = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                (u.FullName != null && u.FullName.ToLower().Contains(term)) ||
                u.Email.ToLower().Contains(term) ||
                u.UserName.ToLower().Contains(term));
        }

        return await query
            .OrderBy(u => u.FullName)
            .Select(u => new UserDto
            {
                Id = u.Id,
                FullName = u.FullName ?? string.Empty,
                Email = u.Email,
                UserName = u.UserName
            })
            .ToListAsync(ct);
    }

    public async Task<UserDto> CreateAsync(CreateUserDto dto, CancellationToken ct)
    {
        var normalizedUserName = dto.UserName.Trim();
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        
        await _validationHelpers.EnsureUserNameIsUniqueAsync(normalizedUserName, ct);
        await _validationHelpers.EnsureEmailIsUniqueAsync(normalizedEmail, ct);

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = normalizedEmail,
            UserName = normalizedUserName,
            PasswordHash = string.Empty
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            UserName = user.UserName
        };
    }

    public async Task UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
            throw new KeyNotFoundException($"User with Id={id} does not exist.");

        if (!string.IsNullOrWhiteSpace(dto.UserName))
        {
            var normalized = dto.UserName.Trim();
            await _validationHelpers.EnsureUserNameIsUniqueAsync(normalized, ct, id);
            user.UserName = normalized;
        }

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var normalized = dto.Email.Trim().ToLowerInvariant();
            await _validationHelpers.EnsureEmailIsUniqueAsync(normalized, ct, id);
            user.Email = normalized;
        }

        if (!string.IsNullOrWhiteSpace(dto.FullName))
        {
            user.FullName = dto.FullName.Trim();
        }

        await _db.SaveChangesAsync(ct);
    }
}