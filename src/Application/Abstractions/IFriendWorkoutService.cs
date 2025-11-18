using Application.DTOs;

namespace Application.Abstractions;

public interface IFriendWorkoutService
{
    Task<IReadOnlyList<FriendScheduledWorkoutDto>> GetFriendsScheduledAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken ct);
    
    Task<IReadOnlyList<FriendWorkoutSessionDto>> GetFriendsSessionsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct);
}