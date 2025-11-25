using Application.Abstractions;
using Application.DTOs;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Application.Common.Security;

namespace Application.Services;

public sealed class FriendWorkoutService : IFriendWorkoutService
{
    private readonly IApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly IFriendshipService _friends; 

    public FriendWorkoutService(
        IApplicationDbContext db, 
        IHttpContextAccessor http, 
        IFriendshipService friends)
    {
        _db = db;
        _http = http;
        _friends = friends;
    }

    private Guid UserId => _http.HttpContext?.User.GetUserId() 
                           ?? throw new UnauthorizedAccessException();

    public async Task<IReadOnlyList<FriendScheduledWorkoutDto>> GetFriendsScheduledAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken ct)
    {
        var friendIds = await _friends.GetFriendIdsAsync(ct);

        if (friendIds.Count == 0)
            return Array.Empty<FriendScheduledWorkoutDto>();

        var scheduled = await _db.ScheduledWorkouts
            .Include(sw => sw.User)
            .Where(sw =>
                friendIds.Contains(sw.UserId) &&
                sw.IsVisibleToFriends &&
                sw.Date >= fromDate &&
                sw.Date <= toDate)
            .OrderBy(sw => sw.Date)
            .ThenBy(sw => sw.Time)
            .AsNoTracking()
            .ToListAsync(ct);

        return scheduled.Select(sw => new FriendScheduledWorkoutDto
        {
            ScheduledId = sw.Id,
            UserId      = sw.UserId,
            UserName    = sw.User.UserName,
            FullName    = sw.User.FullName ?? string.Empty,
            Date        = sw.Date, 
            Time        = sw.Time,
            PlanName    = sw.PlanName,
            Status      = sw.Status == ScheduledStatus.Completed ? "completed" : "planned"
        }).ToList();
    }

    public async Task<IReadOnlyList<FriendWorkoutSessionDto>> GetFriendsSessionsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct)
    {
        var friendIds = await _friends.GetFriendIdsAsync(ct);

        if (friendIds.Count == 0)
            return Array.Empty<FriendWorkoutSessionDto>();

        var sessions = await _db.WorkoutSessions
            .Include(ws => ws.Scheduled)
            .ThenInclude(sw => sw.User)
            .Where(ws =>
                friendIds.Contains(ws.UserId) &&
                ws.Scheduled.IsVisibleToFriends &&
                ws.StartedAtUtc >= fromUtc &&
                ws.StartedAtUtc <  toUtc)
            .OrderByDescending(ws => ws.StartedAtUtc)
            .AsNoTracking()
            .ToListAsync(ct);

        return sessions.Select(ws => new FriendWorkoutSessionDto
        {
            SessionId      = ws.Id,
            ScheduledId    = ws.ScheduledId,
            UserId         = ws.UserId,
            UserName       = ws.Scheduled.User.UserName,
            FullName       = ws.Scheduled.User.FullName ?? string.Empty,
            PlanName       = ws.Scheduled.PlanName,
            StartedAtUtc   = ws.StartedAtUtc,
            CompletedAtUtc = ws.CompletedAtUtc,
            DurationSec    = ws.DurationSec,
            Status         = ws.Status.ToString().ToLower()
        }).ToList();
    }
}