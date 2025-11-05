using Application.DTOs.Analytics;

namespace Application.Abstractions;

public interface IAnalyticsService
{
    Task<OverviewDto> GetOverviewAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct);
    Task<IReadOnlyList<TimePointDto>> GetVolumeAsync(DateTime fromUtc, DateTime toUtc, string groupBy, string? exerciseName, CancellationToken ct);
    Task<IReadOnlyList<E1rmPointDto>> GetE1RmAsync(string exerciseName, DateTime fromUtc, DateTime toUtc, CancellationToken ct);
    Task<AdherenceDto> GetAdherenceAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct);
    Task<IReadOnlyList<PlanVsActualItemDto>> GetPlanVsActualAsync(Guid sessionId, CancellationToken ct);
}