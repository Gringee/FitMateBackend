using System;
using Domain;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<User>> GetAllAsync()
        => _context.Users.ToListAsync();

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        await _context.Users.ToListAsync();
    }
}
