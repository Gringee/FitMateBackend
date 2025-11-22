using Application.Abstractions;
using Application.DTOs;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class UserService : IUserService
{
    private readonly IApplicationDbContext _db;

    public UserService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<UserDto>> GetAllAsync(string? search, CancellationToken ct)
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
        
        var userNameExists = await _db.Users.AnyAsync(u => u.UserName == normalizedUserName, ct);
        if (userNameExists)
            throw new InvalidOperationException($"UserName '{normalizedUserName}' is already taken.");
        
        var emailExists = await _db.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail, ct);
        if (emailExists)
            throw new InvalidOperationException($"Email '{dto.Email}' is already in use.");

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = normalizedEmail,
            UserName = normalizedUserName
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
            var taken = await _db.Users.AnyAsync(u => u.Id != id && u.UserName == normalized, ct);
            if (taken) throw new InvalidOperationException($"UserName '{normalized}' is already taken.");
            user.UserName = normalized;
        }

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var normalized = dto.Email.Trim().ToLowerInvariant();
            var taken = await _db.Users.AnyAsync(u => u.Id != id && u.Email.ToLower() == normalized, ct);
            if (taken) throw new InvalidOperationException($"Email '{dto.Email}' is already in use.");
            user.Email = normalized;
        }

        if (!string.IsNullOrWhiteSpace(dto.FullName))
        {
            user.FullName = dto.FullName.Trim();
        }

        await _db.SaveChangesAsync(ct);
    }
}