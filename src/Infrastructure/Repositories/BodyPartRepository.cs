using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class BodyPartRepository : IBodyPartRepository
{
    private readonly AppDbContext _ctx;
    public BodyPartRepository(AppDbContext ctx) => _ctx = ctx;

    public async Task<BodyPart> AddAsync(BodyPart entity)
    {
        _ctx.BodyParts.Add(entity);
        await _ctx.SaveChangesAsync();
        return entity;
    }

    public Task<List<BodyPart>> GetAllAsync()
        => _ctx.BodyParts.AsNoTracking().OrderBy(b => b.Name).ToListAsync();

    public Task<BodyPart?> GetByIdAsync(Guid id)
        => _ctx.BodyParts.FindAsync(id).AsTask();

    public async Task UpdateAsync(BodyPart entity)
    {
        _ctx.Update(entity);
        await _ctx.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _ctx.BodyParts.FindAsync(id);
        if (entity is null) return false;
        _ctx.Remove(entity);
        await _ctx.SaveChangesAsync();
        return true;
    }
}
