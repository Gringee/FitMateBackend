using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _ctx;
    public CategoryRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task<Category> AddAsync(Category e)
    {
        _ctx.Categories.Add(e);
        await _ctx.SaveChangesAsync();
        return e;
    }

    public Task<List<Category>> GetAllAsync()
        => _ctx.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();

    public Task<Category?> GetByIdAsync(Guid id)
        => _ctx.Categories.FindAsync(id).AsTask();

    public async Task UpdateAsync(Category e)
    {
        _ctx.Update(e);
        await _ctx.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var e = await _ctx.Categories.FindAsync(id);
        if (e is null) return false;
        _ctx.Remove(e);
        await _ctx.SaveChangesAsync();
        return true;
    }
}
