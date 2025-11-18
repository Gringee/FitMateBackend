using Application.DTOs;
    
namespace Application.Abstractions;
    
public interface IPlanService
{
    Task<PlanDto> CreateAsync(CreatePlanDto dto, CancellationToken ct = default);
    Task<List<PlanDto>> GetAllAsync(bool includeShared = false, CancellationToken ct = default);
    Task<PlanDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PlanDto?> UpdateAsync(Guid id, CreatePlanDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<PlanDto?> DuplicateAsync(Guid id, CancellationToken ct = default);
    Task ShareToUserAsync(Guid planId, Guid sharedWithUserId, CancellationToken ct);
    Task<List<PlanDto>> GetSharedWithMeAsync(CancellationToken ct);
    Task RespondToSharedPlanAsync(Guid sharedPlanId, bool accept, CancellationToken ct);
    Task<List<SharedPlanDto>> GetPendingSharedPlansAsync(CancellationToken ct);
    Task<List<SharedPlanDto>> GetSharedHistoryAsync(string? scope, CancellationToken ct);
    Task DeleteSharedPlanAsync(Guid sharedPlanId, bool onlyIfPending, CancellationToken ct = default);
    Task<List<SharedPlanDto>> GetSentPendingSharedPlansAsync(CancellationToken ct);
}