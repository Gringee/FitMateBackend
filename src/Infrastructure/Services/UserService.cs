using Application.Abstractions;
using Application.DTOs;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<UserDto>> GetAllAsync(string? search, CancellationToken ct)
    {
        var query = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            
            query = query.Where(u =>
                EF.Functions.ILike(u.FullName ?? String.Empty, pattern) ||
                EF.Functions.ILike(u.Email, pattern) ||
                EF.Functions.ILike(u.UserName, pattern));
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
        if (dto is null)
            throw new ArgumentNullException(nameof(dto));
        
        if (string.IsNullOrWhiteSpace(dto.UserName))
            throw new ArgumentException("UserName is required.", nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new ArgumentException("Email is required.", nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.FullName))
            throw new ArgumentException("FullName is required.", nameof(dto));

        var normalizedUserName = dto.UserName.Trim();
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        
        var userNameExists = await _db.Users
            .AnyAsync(u => u.UserName == normalizedUserName, ct);

        if (userNameExists)
            throw new InvalidOperationException(
                $"UserName '{normalizedUserName}' is already taken.");
        
        var emailExists = await _db.Users
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail, ct);

        if (emailExists)
            throw new InvalidOperationException(
                $"Email '{dto.Email}' is already in use.");

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

        if (string.IsNullOrWhiteSpace(dto.UserName))
            throw new ArgumentException("UserName is required.", nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new ArgumentException("Email is required.", nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.FullName))
            throw new ArgumentException("FullName is required.", nameof(dto));

        var normalizedUserName = dto.UserName.Trim().ToLowerInvariant();
        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
        
        var userNameExists = await _db.Users
            .AnyAsync(u => u.Id != id && u.UserName == normalizedUserName, ct);

        if (userNameExists)
            throw new InvalidOperationException(
                $"UserName '{normalizedUserName}' is already taken.");
        
        var emailExists = await _db.Users
            .AnyAsync(u => u.Id != id && u.Email == normalizedEmail, ct);

        if (emailExists)
            throw new InvalidOperationException(
                $"Email '{dto.Email}' is already in use.");

        user.FullName = dto.FullName.Trim();
        user.Email = normalizedEmail;
        user.UserName = normalizedUserName;

        await _db.SaveChangesAsync(ct);
    }
    
    
}