using Application.DTOs.BodyMetrics;

namespace Application.Abstractions;

public interface IBodyMetricsService
{
    Task<BodyMeasurementDto> AddMeasurementAsync(
        CreateBodyMeasurementDto dto, 
        CancellationToken ct = default);
    
    Task<IReadOnlyList<BodyMeasurementDto>> GetMeasurementsAsync(
        DateTime? from = null, 
        DateTime? to = null, 
        CancellationToken ct = default);
    
    Task<BodyMeasurementDto?> GetLatestMeasurementAsync(CancellationToken ct = default);
    
    Task<BodyMetricsStatsDto> GetStatsAsync(CancellationToken ct = default);
    
    Task<IReadOnlyList<BodyMetricsProgressDto>> GetProgressAsync(
        DateTime from, 
        DateTime to, 
        CancellationToken ct = default);
    
    Task DeleteMeasurementAsync(Guid id, CancellationToken ct = default);
    Task<BodyMeasurementDto?> GetFriendMetricsAsync(Guid friendId, CancellationToken ct = default);
}
