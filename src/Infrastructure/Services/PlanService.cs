using Application.Abstractions;
using Application.DTOs;
using Application.Common.Security; // Extension GetUserId()
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

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

    private Guid UserId => _http.HttpContext?.User.GetUserId() 
                           ?? throw new UnauthorizedAccessException("No HttpContext/User.");

    public async Task<PlanDto> CreateAsync(CreatePlanDto dto, CancellationToken ct = default)
    {
        var userId = UserId;

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
        
        return await GetByIdAsync(plan.Id, ct) ?? throw new InvalidOperationException("Failed to retrieve created plan.");
    }

    public async Task<List<PlanDto>> GetAllAsync(bool includeShared = false, CancellationToken ct = default)
    {
        var userId = UserId;
        
        var ownedPlans = await _db.Plans
            .Include(p => p.Exercises).ThenInclude(e => e.Sets)
            .Where(p => p.CreatedByUserId == userId)
            .AsNoTracking()
            .ToListAsync(ct);
        
        if (!includeShared)
            return ownedPlans.Select(Map).ToList();
        
        var sharedPlans = await _db.SharedPlans
            .Include(sp => sp.Plan).ThenInclude(p => p.Exercises).ThenInclude(e => e.Sets)
            .Where(sp => sp.SharedWithUserId == userId && sp.Status == "Accepted")
            .Select(sp => sp.Plan)
            .AsNoTracking()
            .ToListAsync(ct);
        
        var allPlans = ownedPlans
            .Concat(sharedPlans)
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .ToList();

        return allPlans.Select(Map).ToList();
    }

    public async Task<PlanDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var userId = UserId;

        var p = await _db.Plans
            .Include(x => x.Exercises).ThenInclude(e => e.Sets)
            .Where(x => x.CreatedByUserId == userId)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return p is null ? null : Map(p);
    }

public async Task<PlanDto?> UpdateAsync(Guid id, CreatePlanDto dto, CancellationToken ct = default)
{
    var userId = UserId;

    using var transaction = await _db.Database.BeginTransactionAsync(ct);

    try
    {
        var plan = await _db.Plans
            .FirstOrDefaultAsync(p => p.Id == id && p.CreatedByUserId == userId, ct);

        if (plan is null) return null;
        
        await _db.Database.ExecuteSqlRawAsync(
            "DELETE FROM plan_sets WHERE \"PlanExerciseId\" IN (SELECT \"Id\" FROM plan_exercises WHERE \"PlanId\" = {0})",
            new object[] { id }, ct);
            
        await _db.Database.ExecuteSqlRawAsync(
            "DELETE FROM plan_exercises WHERE \"PlanId\" = {0}",
            new object[] { id }, ct);
        
        _db.ChangeTracker.Clear();

        _db.Plans.Attach(plan);
        plan.PlanName = dto.PlanName;
        plan.Type = dto.Type;
        plan.Notes = dto.Notes;

        var entry = _db.Entry(plan);
        entry.Property(p => p.PlanName).IsModified = true;
        entry.Property(p => p.Type).IsModified = true;
        entry.Property(p => p.Notes).IsModified = true;

        foreach (var ex in dto.Exercises)
        {
            var pe = new PlanExercise
            {
                Id = Guid.NewGuid(),
                PlanId = id, 
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
            _db.PlanExercises.Add(pe);
        }
        
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        
        return await GetByIdAsync(id, ct);
    }
    catch
    {
        await transaction.RollbackAsync(ct);
        throw;
    }
}

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var userId = UserId;
        var p = await _db.Plans.FirstOrDefaultAsync(x => x.Id == id && x.CreatedByUserId == userId, ct);
        
        if (p is null) return false;

        _db.Remove(p);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<PlanDto?> DuplicateAsync(Guid id, CancellationToken ct = default)
    {
        var userId = UserId;

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
        var sharedById = UserId;
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
        var myId = UserId;

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
        var userId = UserId;

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
        var userId = UserId;

        var shared = await _db.SharedPlans
            .Include(sp => sp.Plan)
            .Include(sp => sp.SharedByUser)
            .Include(sp => sp.SharedWithUser)
            .Where(sp => sp.SharedWithUserId == userId && sp.Status == "Pending")
            .AsNoTracking()
            .ToListAsync(ct);

        return shared.Select(MapShared).ToList();
    }
    
    public async Task<List<SharedPlanDto>> GetSharedHistoryAsync(string? scope, CancellationToken ct)
    {
        var myId = UserId;
        
        var normalizedScope = string.IsNullOrWhiteSpace(scope) ? "received" : scope.Trim().ToLowerInvariant();

        var query = _db.SharedPlans
            .Include(sp => sp.Plan)
            .Include(sp => sp.SharedByUser)
            .Include(sp => sp.SharedWithUser)
            .AsQueryable();
        
        switch (normalizedScope)
        {
            case "sent":
                query = query.Where(sp => sp.SharedByUserId == myId && sp.Status != "Pending");
                break;

            case "all":
                query = query.Where(sp => (sp.SharedByUserId == myId || sp.SharedWithUserId == myId) && sp.Status != "Pending");
                break;
            
            default:
                query = query.Where(sp => sp.SharedWithUserId == myId && sp.Status != "Pending");
                break;
        }

        var items = await query
            .OrderByDescending(sp => sp.RespondedAtUtc ?? sp.SharedAtUtc)
            .AsNoTracking()
            .ToListAsync(ct);

        return items.Select(MapShared).ToList();
    }
    
    public async Task<List<SharedPlanDto>> GetSentPendingSharedPlansAsync(CancellationToken ct)
    {
        var myId = UserId;

        var shared = await _db.SharedPlans
            .Include(sp => sp.Plan)
            .Include(sp => sp.SharedWithUser)
            .Include(sp => sp.SharedByUser)
            .Where(sp => sp.SharedByUserId == myId && sp.Status == "Pending")
            .AsNoTracking()
            .ToListAsync(ct);

        return shared.Select(MapShared).ToList();
    }
    
    public async Task DeleteSharedPlanAsync(Guid sharedPlanId, bool onlyIfPending, CancellationToken ct = default)
    {
        var ownerId = UserId;
        
        var sp = await _db.SharedPlans
            .FirstOrDefaultAsync(x => x.Id == sharedPlanId && (x.SharedByUserId == ownerId || x.SharedWithUserId == ownerId), ct);

        if (sp is null)
            throw new KeyNotFoundException("Shared plan not found or you don't have permission.");

        if (onlyIfPending)
        {
            if (!string.Equals(sp.Status, "Pending", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only pending shared plans can be cancelled.");
        }

        _db.SharedPlans.Remove(sp);
        await _db.SaveChangesAsync(ct);
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
        Id             = sp.Id,
        PlanId         = sp.PlanId,
        PlanName       = sp.Plan.PlanName,
        SharedByName   = sp.SharedByUser.FullName ?? string.Empty,
        SharedWithName = sp.SharedWithUser.FullName ?? string.Empty,
        SharedAtUtc    = sp.SharedAtUtc,
        Status         = sp.Status,
        RespondedAtUtc = sp.RespondedAtUtc
    };
}