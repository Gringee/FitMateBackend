using Application.DTOs;

namespace Application.Abstractions;

public interface IScheduledService
{
    Task<ScheduledDto> CreateAsync(CreateScheduledDto dto, CancellationToken ct = default);
    Task<List<ScheduledDto>> GetAllAsync(CancellationToken ct = default);
    Task<ScheduledDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<ScheduledDto>> GetByDateAsync(string yyyyMMdd, CancellationToken ct = default);
    Task<ScheduledDto?> UpdateAsync(Guid id, CreateScheduledDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<ScheduledDto?> DuplicateAsync(Guid id, CancellationToken ct = default);
}
