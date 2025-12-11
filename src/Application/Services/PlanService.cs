using Application.Abstractions;
using System.Linq;
using Application.DTOs;
using Application.Common.Security; 
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public sealed class PlanService : IPlanService
{
    private readonly IApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;
    
    public PlanService(IApplicationDbContext db, IHttpContextAccessor http) 
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

        if (dto.Exercises is { Count: > 0 })
        {
            plan.Exercises = CreateExercisesFromDto(dto.Exercises, plan.Id);
        }

        _db.Add(plan);
        await _db.SaveChangesAsync(ct);
        
        return await GetByIdAsync(plan.Id, ct) ?? throw new InvalidOperationException("Failed to retrieve created plan.");
    }

    public async Task<IReadOnlyList<PlanDto>> GetAllAsync(bool includeShared = false, CancellationToken ct = default)
    {
        var userId = UserId;
        
        var ownedPlans = await _db.Plans
            .Include(p => p.Exercises).ThenInclude(e => e.Sets)
            .Where(p => p.CreatedByUserId == userId)
            .AsNoTracking()
            .ToListAsync(ct);
        
        if (!includeShared)
            return ownedPlans.Select(p => Map(p)).ToList();
        
        var sharedPlans = await _db.SharedPlans
            .Include(sp => sp.Plan).ThenInclude(p => p.Exercises).ThenInclude(e => e.Sets)
            .Where(sp => sp.SharedWithUserId == userId && sp.Status == RequestStatus.Accepted)
            .AsNoTracking()
            .ToListAsync(ct);

        var ownedDtos = ownedPlans.Select(p => Map(p));
        var sharedDtos = sharedPlans.Select(sp => Map(sp.Plan, sp.Id));
        
        var allPlans = ownedDtos
            .Concat(sharedDtos)
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .ToList();

        return allPlans;
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

        var plan = await _db.Plans
            .FirstOrDefaultAsync(p => p.Id == id && p.CreatedByUserId == userId, ct);

        if (plan is null) return null;

        plan.PlanName = dto.PlanName;
        plan.Type = dto.Type;
        plan.Notes = dto.Notes;

        var oldExercises = await _db.PlanExercises
            .Where(e => e.PlanId == id)
            .ToListAsync(ct);
        
        _db.PlanExercises.RemoveRange(oldExercises);

        if (dto.Exercises is { Count: > 0 })
        {
            var newExercises = CreateExercisesFromDto(dto.Exercises, id);
            await _db.PlanExercises.AddRangeAsync(newExercises, ct);
        }

        await _db.SaveChangesAsync(ct);
        
        return await GetByIdAsync(id, ct);
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

        copy.Exercises = CreateExercisesFromPlan(p, copy.Id);

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

    public async Task<IReadOnlyList<PlanDto>> GetSharedWithMeAsync(CancellationToken ct)
    {
        var myId = UserId;

        var sharedPlans = await _db.SharedPlans
            .Include(sp => sp.Plan).ThenInclude(p => p.Exercises).ThenInclude(e => e.Sets)
            .Where(sp => sp.SharedWithUserId == myId && sp.Status == RequestStatus.Accepted) 
            .AsNoTracking()
            .ToListAsync(ct);

        return sharedPlans.Select(sp => Map(sp.Plan, sp.Id)).ToList();
    }
    
    public async Task RespondToSharedPlanAsync(Guid sharedPlanId, bool accept, CancellationToken ct)
    {
        var userId = UserId;

        var shared = await _db.SharedPlans
                         .FirstOrDefaultAsync(sp => sp.Id == sharedPlanId && sp.SharedWithUserId == userId, ct)
                     ?? throw new KeyNotFoundException("Shared plan not found.");

        if (shared.Status != RequestStatus.Pending)
            throw new InvalidOperationException("This plan has already been accepted or rejected previously.");

        shared.Status = accept ? RequestStatus.Accepted : RequestStatus.Rejected;
        shared.RespondedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<SharedPlanDto>> GetPendingSharedPlansAsync(CancellationToken ct)
    {
        var userId = UserId;

        var shared = await _db.SharedPlans
            .Include(sp => sp.Plan)
            .Include(sp => sp.SharedByUser)
            .Include(sp => sp.SharedWithUser)
            .Where(sp => sp.SharedWithUserId == userId && sp.Status == RequestStatus.Pending)
            .AsNoTracking()
            .ToListAsync(ct);

        return shared.Select(MapShared).ToList();
    }
    
    public async Task<IReadOnlyList<SharedPlanDto>> GetSharedHistoryAsync(string? scope, CancellationToken ct)
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
                query = query.Where(sp => sp.SharedByUserId == myId && sp.Status != RequestStatus.Pending);
                break;

            case "all":
                query = query.Where(sp => (sp.SharedByUserId == myId || sp.SharedWithUserId == myId) && sp.Status != RequestStatus.Pending);
                break;
            
            default:
                query = query.Where(sp => sp.SharedWithUserId == myId && sp.Status != RequestStatus.Pending);
                break;
        }

        var items = await query
            .OrderByDescending(sp => sp.RespondedAtUtc ?? sp.SharedAtUtc)
            .AsNoTracking()
            .ToListAsync(ct);

        return items.Select(MapShared).ToList();
    }
    
    public async Task<IReadOnlyList<SharedPlanDto>> GetSentPendingSharedPlansAsync(CancellationToken ct)
    {
        var myId = UserId;

        var shared = await _db.SharedPlans
            .Include(sp => sp.Plan)
            .Include(sp => sp.SharedWithUser)
            .Include(sp => sp.SharedByUser)
            .Where(sp => sp.SharedByUserId == myId && sp.Status == RequestStatus.Pending)
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
            if (sp.Status != RequestStatus.Pending)
                throw new InvalidOperationException("Only pending shared plans can be cancelled.");
        }

        _db.SharedPlans.Remove(sp);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteSharedPlanByPlanIdAsync(Guid planId, CancellationToken ct = default)
    {
        var userId = UserId;
        var sp = await _db.SharedPlans
            .FirstOrDefaultAsync(sp => sp.PlanId == planId && sp.SharedWithUserId == userId, ct);

        if (sp is null)
            throw new KeyNotFoundException("Shared plan not found or you don't have permission.");

        _db.SharedPlans.Remove(sp);
        await _db.SaveChangesAsync(ct);
    }

    private static List<PlanExercise> CreateExercisesFromDto(IEnumerable<ExerciseDto> dtos, Guid planId)
    {
        var list = new List<PlanExercise>();
        foreach (var ex in dtos)
        {
            var pe = new PlanExercise
            {
                Id = Guid.NewGuid(),
                PlanId = planId,
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
            list.Add(pe);
        }
        return list;
    }

    private static List<PlanExercise> CreateExercisesFromPlan(Plan sourcePlan, Guid targetPlanId)
    {
        var list = new List<PlanExercise>();
        
        foreach (var ex in sourcePlan.Exercises)
        {
            var pe = new PlanExercise
            {
                Id = Guid.NewGuid(),
                PlanId = targetPlanId,
                Name = ex.Name,
                RestSeconds = ex.RestSeconds
            };
            
            var sortedSets = ex.Sets.OrderBy(s => s.SetNumber).ToList();
            foreach (var s in sortedSets)
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
            list.Add(pe);
        }
        return list;
    }

    private static PlanDto Map(Plan p, Guid? sharedPlanId = null) => new()
    {
        Id = p.Id,
        SharedPlanId = sharedPlanId,
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
        Status         = sp.Status.ToString(),
        RespondedAtUtc = sp.RespondedAtUtc
    };
}