using Application.Abstractions;
using Application.DTOs;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;

namespace Infrastructure.Services;

public sealed class PlanService(AppDbContext db) : IPlanService
{
    private readonly AppDbContext _db = db;

    public async Task<PlanDto> CreateAsync(CreatePlanDto dto, CancellationToken ct = default)
    {
        var plan = new Plan { Id = Guid.NewGuid(), PlanName = dto.PlanName, Notes = dto.Notes };
        foreach (var ex in dto.Exercises)
        {
            var pe = new PlanExercise
            {
                Id = Guid.NewGuid(),
                PlanId = plan.Id,
                Name = ex.Name,
                RestSeconds = ex.Rest
            };
            var i = 1;
            foreach (var s in ex.Sets)
            {
                pe.Sets.Add(new PlanSet
                {
                    Id = Guid.NewGuid(),
                    PlanExerciseId = pe.Id,
                    SetNumber = i++,
                    Reps = s.Reps,
                    Weight = s.Weight
                });
            }
            plan.Exercises.Add(pe);
        }
        _db.Add(plan);
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(plan.Id, ct) ?? throw new InvalidOperationException();
    }

    public async Task<List<PlanDto>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await _db.Plans
            .Include(p => p.Exercises).ThenInclude(e => e.Sets)
            .AsNoTracking().ToListAsync(ct);

        return list.Select(Map).ToList();
    }

    public async Task<PlanDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var p = await _db.Plans
            .Include(x => x.Exercises).ThenInclude(e => e.Sets)
            .AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

        return p is null ? null : Map(p);
    }

    public async Task<PlanDto?> UpdateAsync(Guid id, CreatePlanDto dto, CancellationToken ct = default)
    {
        var plan = await _db.Plans
            .Include(p => p.Exercises).ThenInclude(e => e.Sets)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (plan is null) return null;

        plan.PlanName = dto.PlanName;
        plan.Notes = dto.Notes;

        _db.RemoveRange(plan.Exercises.SelectMany(e => e.Sets));
        _db.RemoveRange(plan.Exercises);
        plan.Exercises.Clear();

        foreach (var ex in dto.Exercises)
        {
            var pe = new PlanExercise
            {
                Id = Guid.NewGuid(),
                PlanId = plan.Id,
                Name = ex.Name,
                RestSeconds = ex.Rest
            };
            var i = 1;
            foreach (var s in ex.Sets)
            {
                pe.Sets.Add(new PlanSet
                {
                    Id = Guid.NewGuid(),
                    PlanExerciseId = pe.Id,
                    SetNumber = i++,
                    Reps = s.Reps,
                    Weight = s.Weight
                });
            }
            plan.Exercises.Add(pe);
        }

        await _db.SaveChangesAsync(ct);
        return Map(plan);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var p = await _db.Plans.FindAsync([id], ct);
        if (p is null) return false;
        _db.Remove(p);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<PlanDto?> DuplicateAsync(Guid id, CancellationToken ct = default)
    {
        var p = await _db.Plans
            .Include(x => x.Exercises).ThenInclude(e => e.Sets)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return null;

        var copy = new Plan
        {
            Id = Guid.NewGuid(),
            PlanName = p.PlanName + " (Copy)",
            Notes = p.Notes
        };
        foreach (var ex in p.Exercises)
        {
            var pe = new PlanExercise
            {
                Id = Guid.NewGuid(),
                PlanId = copy.Id,
                Name = ex.Name,
                RestSeconds = ex.RestSeconds
            };
            foreach (var s in ex.Sets.OrderBy(s => s.SetNumber))
            {
                pe.Sets.Add(new PlanSet
                {
                    Id = Guid.NewGuid(),
                    PlanExerciseId = pe.Id,
                    SetNumber = s.SetNumber,
                    Reps = s.Reps,
                    Weight = s.Weight
                });
            }
            copy.Exercises.Add(pe);
        }

        _db.Add(copy);
        await _db.SaveChangesAsync(ct);
        return Map(copy);
    }

    private static PlanDto Map(Plan p) => new()
    {
        Id = p.Id,
        PlanName = p.PlanName,
        Notes = p.Notes,
        Exercises = p.Exercises
            .OrderBy(e => e.Id)
            .Select(e => new ExerciseDto
            {
                Name = e.Name,
                Rest = e.RestSeconds,
                Sets = e.Sets.OrderBy(s => s.SetNumber)
                             .Select(s => new SetDto { Reps = s.Reps, Weight = s.Weight }).ToList()
            }).ToList()
    };
}
