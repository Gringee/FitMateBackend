using Application.Abstractions;
using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

/// <summary>
/// Serwis odpowiedzialny za udostępnianie planów treningowych między użytkownikami.
/// </summary>
public sealed class PlanSharingService : IPlanSharingService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPlanService _planService;
    
    public PlanSharingService(
        IApplicationDbContext db, 
        ICurrentUserService currentUser,
        IPlanService planService)
    {
        _db = db;
        _currentUser = currentUser;
        _planService = planService;
    }

    private Guid UserId => _currentUser.UserId;

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

        var planIds = await _db.SharedPlans
            .Where(sp => sp.SharedWithUserId == myId && sp.Status == RequestStatus.Accepted)
            .Select(sp => sp.PlanId)
            .ToListAsync(ct);

        var plans = new List<PlanDto>();
        foreach (var planId in planIds)
        {
            var plan = await _planService.GetByIdAsync(planId, ct);
            if (plan is not null)
                plans.Add(plan);
        }

        return plans;
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
