using Application.DTOs;

namespace Application.Abstractions;

public interface IWorkoutSessionService
{
    Task<WorkoutSessionDto> StartAsync(StartSessionRequest req, CancellationToken ct);
    Task<WorkoutSessionDto> PatchSetAsync(Guid sessionId, PatchSetRequest req, CancellationToken ct);
    Task<WorkoutSessionDto> CompleteAsync(Guid sessionId, CompleteSessionRequest req, CancellationToken ct);
    Task<WorkoutSessionDto> AbortAsync(Guid sessionId, AbortSessionRequest req, CancellationToken ct);
    Task<WorkoutSessionDto?> GetByIdAsync(Guid sessionId, CancellationToken ct);
    Task<IReadOnlyList<WorkoutSessionDto>> GetByRangeAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct);
}