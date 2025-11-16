using Application.DTOs;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Application.Common.Security;
using Microsoft.AspNetCore.Http;  
using Application.Abstractions;

namespace Infrastructure.Services;

public sealed class PlanService : IPlanService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;  
    public PlanService(AppDbContext db, IHttpContextAccessor http) 
    {
        _db = db;
        _http = http;
    }

    private Guid CurrentUserId()
    {
        var user = _http.HttpContext?.User 
                   ?? throw new UnauthorizedAccessException("No HttpContext/User.");
        return user.GetUserId();
    }

    public async Task<PlanDto> CreateAsync(CreatePlanDto dto, CancellationToken ct = default)
    {
        var userId = CurrentUserId();

        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanName = dto.PlanName,
            Type = dto.Type,
            Notes = dto.Notes,
            CreatedByUserId = userId 
        };

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
        var userId = CurrentUserId();

        var list = await _db.Plans
            .Include(p => p.Exercises).ThenInclude(e => e.Sets)
            .Where(p => p.CreatedByUserId == userId)
            .AsNoTracking()
            .ToListAsync(ct);

        return list.Select(Map).ToList();
    }

    public async Task<PlanDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var userId = CurrentUserId();

        var p = await _db.Plans
            .Include(x => x.Exercises).ThenInclude(e => e.Sets)
            .Where(x => x.CreatedByUserId == userId)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return p is null ? null : Map(p);
    }

    public async Task<PlanDto?> UpdateAsync(Guid id, CreatePlanDto dto, CancellationToken ct = default)
    {
        var userId = CurrentUserId();
        
        var plan = await _db.Plans
            .FirstOrDefaultAsync(p => p.Id == id && p.CreatedByUserId == userId, ct);

        if (plan is null)
            return null;
        
        plan.PlanName = dto.PlanName;
        plan.Type     = dto.Type;
        plan.Notes    = dto.Notes;
        
        await _db.PlanSets
            .Where(s => s.PlanExercise.PlanId == id)
            .ExecuteDeleteAsync(ct);   

        await _db.PlanExercises
            .Where(e => e.PlanId == id)
            .ExecuteDeleteAsync(ct);
        
        foreach (var ex in dto.Exercises)
        {
            var pe = new PlanExercise
            {
                Id          = Guid.NewGuid(),
                PlanId      = plan.Id,
                Name        = ex.Name,
                RestSeconds = ex.Rest
            };

            var i = 1;
            foreach (var s in ex.Sets)
            {
                pe.Sets.Add(new PlanSet
                {
                    Id             = Guid.NewGuid(),
                    PlanExerciseId = pe.Id,
                    SetNumber      = i++,
                    Reps           = s.Reps,
                    Weight         = s.Weight
                });
            }

            _db.PlanExercises.Add(pe);
        }
        
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var userId = CurrentUserId();

        var p = await _db.Plans
            .FirstOrDefaultAsync(x => x.Id == id && x.CreatedByUserId == userId, ct);
        if (p is null) return false;

        _db.Remove(p);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<PlanDto?> DuplicateAsync(Guid id, CancellationToken ct = default)
    {
        var userId = CurrentUserId();

        var p = await _db.Plans
            .Include(x => x.Exercises).ThenInclude(e => e.Sets)
            .FirstOrDefaultAsync(x => x.Id == id && x.CreatedByUserId == userId, ct);
        if (p is null) return null;

        var copy = new Plan
        {
            Id = Guid.NewGuid(),
            PlanName = p.PlanName + " (Copy)",
            Type = p.Type,
            Notes = p.Notes,
            CreatedByUserId = userId
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
    
    public async Task ShareToUserAsync(Guid planId, Guid sharedWithUserId, CancellationToken ct)
    {
        var sharedById = CurrentUserId();
        if (sharedWithUserId == sharedById)
            throw new InvalidOperationException("You cannot share a plan with yourself.");

        var plan = await _db.Plans
                       .FirstOrDefaultAsync(p => p.Id == planId && p.CreatedByUserId == sharedById, ct)
                   ?? throw new UnauthorizedAccessException("You do not have permission to access this plan.");

        var targetExists = await _db.Users.AnyAsync(u => u.Id == sharedWithUserId, ct);
        if (!targetExists)
            throw new KeyNotFoundException("Target user does not exist.");

        var already = await _db.SharedPlans
            .AnyAsync(x => x.PlanId == planId && x.SharedWithUserId == sharedWithUserId, ct);
        if (already)
            throw new InvalidOperationException("This plan has already been shared with this user.");

        _db.SharedPlans.Add(new SharedPlan
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            SharedByUserId = sharedById,
            SharedWithUserId = sharedWithUserId
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<PlanDto>> GetSharedWithMeAsync(CancellationToken ct)
    {
        var myId = CurrentUserId();

        var plans = await _db.SharedPlans
            .Include(sp => sp.Plan).ThenInclude(p => p.Exercises).ThenInclude(e => e.Sets)
            .Where(sp => sp.SharedWithUserId == myId && sp.Status == "Accepted") 
            .AsNoTracking()
            .Select(sp => sp.Plan)
            .ToListAsync(ct);

        return plans.Select(Map).ToList();
    }
    
    public async Task RespondToSharedPlanAsync(Guid sharedPlanId, bool accept, CancellationToken ct)
    {
        var userId = CurrentUserId();

        var shared = await _db.SharedPlans
                         .FirstOrDefaultAsync(sp => sp.Id == sharedPlanId && sp.SharedWithUserId == userId, ct)
                     ?? throw new KeyNotFoundException("Shared plan not found.");

        if (shared.Status != "Pending")
            throw new InvalidOperationException("This plan has already been accepted or rejected previously.");

        shared.Status = accept ? "Accepted" : "Rejected";
        shared.RespondedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<SharedPlanDto>> GetPendingSharedPlansAsync(CancellationToken ct)
    {
        var userId = CurrentUserId();

        var shared = await _db.SharedPlans
            .Include(sp => sp.Plan)
            .Include(sp => sp.SharedByUser)
            .Where(sp => sp.SharedWithUserId == userId && sp.Status == "Pending")
            .AsNoTracking()
            .ToListAsync(ct);

        return shared.Select(sp => new SharedPlanDto
        {
            Id = sp.Id,
            PlanId = sp.PlanId,
            PlanName = sp.Plan.PlanName,
            SharedByName = sp.SharedByUser.FullName ?? string.Empty,
            SharedAtUtc = sp.SharedAtUtc,
            Status = sp.Status
        }).ToList();
    }
    
    public async Task<List<SharedPlanDto>> GetSharedHistoryAsync(CancellationToken ct)
    {
        var myId = CurrentUserId();

        var items = await _db.SharedPlans
            .Include(sp => sp.Plan)
            .Include(sp => sp.SharedByUser)
            .Where(sp => sp.SharedWithUserId == myId && sp.Status != "Pending")
            .OrderByDescending(sp => sp.RespondedAtUtc)
            .AsNoTracking()
            .ToListAsync(ct);

        return items.Select(MapShared).ToList();
    }
    
    public async Task<bool> UnshareAsync(Guid sharedPlanId, CancellationToken ct)
    {
        var ownerId = CurrentUserId();

        var sp = await _db.SharedPlans
            .FirstOrDefaultAsync(x => x.Id == sharedPlanId && x.SharedByUserId == ownerId, ct);

        if (sp is null) return false;

        _db.SharedPlans.Remove(sp);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static PlanDto Map(Plan p) => new()
    {
        Id = p.Id,
        PlanName = p.PlanName,
        Type = p.Type,
        Notes = p.Notes,
        Exercises = p.Exercises
            .OrderBy(e => e.Id)
            .Select(e => new ExerciseDto
            {
                Name = e.Name,
                Rest = e.RestSeconds,
                Sets = e.Sets
                    .OrderBy(s => s.SetNumber)
                    .Select(s => new SetDto { Reps = s.Reps, Weight = s.Weight })
                    .ToList()
            }).ToList()
    };
    
    private static SharedPlanDto MapShared(SharedPlan sp) => new()
    {
        Id = sp.Id,
        PlanId = sp.PlanId,
        PlanName = sp.Plan.PlanName,
        SharedByName = sp.SharedByUser.FullName ?? string.Empty,
        SharedAtUtc = sp.SharedAtUtc,
        Status = sp.Status,
        RespondedAtUtc = sp.RespondedAtUtc
    };
}