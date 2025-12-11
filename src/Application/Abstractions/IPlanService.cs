using Application.DTOs;
    
namespace Application.Abstractions;
    
public interface IPlanService
{
    Task<PlanDto> CreateAsync(CreatePlanDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<PlanDto>> GetAllAsync(bool includeShared = false, CancellationToken ct = default);
    Task<PlanDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PlanDto?> UpdateAsync(Guid id, CreatePlanDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<PlanDto?> DuplicateAsync(Guid id, CancellationToken ct = default);
    Task ShareToUserAsync(Guid planId, Guid sharedWithUserId, CancellationToken ct);
    Task<IReadOnlyList<PlanDto>> GetSharedWithMeAsync(CancellationToken ct);
    Task RespondToSharedPlanAsync(Guid sharedPlanId, bool accept, CancellationToken ct);
    Task<IReadOnlyList<SharedPlanDto>> GetPendingSharedPlansAsync(CancellationToken ct);
    Task<IReadOnlyList<SharedPlanDto>> GetSharedHistoryAsync(string? scope, CancellationToken ct);
    Task DeleteSharedPlanAsync(Guid sharedPlanId, bool onlyIfPending, CancellationToken ct = default);
    Task DeleteSharedPlanByPlanIdAsync(Guid planId, CancellationToken ct = default);
    Task<IReadOnlyList<SharedPlanDto>> GetSentPendingSharedPlansAsync(CancellationToken ct);
}