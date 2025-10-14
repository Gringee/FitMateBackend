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
}
