using Application.DTOs;
    
namespace Application.Abstractions;
    
public interface IPlanService
{
    Task<PlanDto> CreateAsync(CreatePlanDto dto, CancellationToken ct = default);
    Task<List<PlanDto>> GetAllAsync(CancellationToken ct = default);
    Task<PlanDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PlanDto?> UpdateAsync(Guid id, CreatePlanDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<PlanDto?> DuplicateAsync(Guid id, CancellationToken ct = default);
    Task ShareToUserAsync(Guid planId, Guid sharedWithUserId, CancellationToken ct);
    Task<List<PlanDto>> GetSharedWithMeAsync(CancellationToken ct);
    Task RespondToSharedPlanAsync(Guid sharedPlanId, bool accept, CancellationToken ct);
    Task<List<SharedPlanDto>> GetPendingSharedPlansAsync(CancellationToken ct);
    Task<List<SharedPlanDto>> GetSharedHistoryAsync(CancellationToken ct);
    Task<bool> UnshareAsync(Guid sharedPlanId, CancellationToken ct);
}