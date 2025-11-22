using Application.Abstractions;
using Application.DTOs.Analytics;
using Domain.Enums; 
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Application.Common.Security;

namespace Application.Services;

public sealed class AnalyticsService : IAnalyticsService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public AnalyticsService(IApplicationDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    private Guid CurrentUserId() => _currentUserService.UserId;

    public async Task<OverviewDto> GetOverviewAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        if (fromUtc > toUtc) (fromUtc, toUtc) = (toUtc, fromUtc);
        var userId = CurrentUserId();

        var sessionsQuery = _db.WorkoutSessions
            .AsNoTracking()
            .Where(ws => ws.UserId == userId && ws.CompletedAtUtc != null && 
                         ws.StartedAtUtc >= fromUtc && ws.StartedAtUtc <= toUtc);

        var setsQuery = sessionsQuery
            .SelectMany(ws => ws.Exercises.SelectMany(se => se.Sets));

        var totalVolume = await setsQuery
            .SumAsync(s => (decimal)((s.RepsDone ?? 0) * (double)(s.WeightDone ?? 0)), ct);
        
        var avgIntensity = await setsQuery
            .Where(s => s.WeightDone != null && s.WeightDone > 0 && s.RepsDone > 0) 
            .AverageAsync(s => (double?)s.WeightDone, ct) ?? 0; 

        var sessionsCount = await sessionsQuery.CountAsync(ct);

        DateOnly fromDate = DateOnly.FromDateTime(fromUtc);
        DateOnly toDate = DateOnly.FromDateTime(toUtc);

        var scheduledBase = _db.ScheduledWorkouts
            .AsNoTracking()
            .Where(sw => sw.UserId == userId && sw.Date >= fromDate && sw.Date <= toDate);

        int planned = await scheduledBase.CountAsync(ct);
        int completed = await scheduledBase.CountAsync(sw => sw.Status == ScheduledStatus.Completed, ct);

        return new OverviewDto
        {
            TotalVolume = decimal.Round(totalVolume, 2),
            AvgIntensity = Math.Round(avgIntensity, 2),
            SessionsCount = sessionsCount,
            AdherencePct = planned == 0 ? 0 : Math.Round((double)completed * 100 / planned, 1),
            NewPrs = 0
        };
    }

    public async Task<IReadOnlyList<TimePointDto>> GetVolumeAsync(DateTime fromUtc, DateTime toUtc, string groupBy, string? exerciseName, CancellationToken ct)
    {
        if (fromUtc > toUtc) (fromUtc, toUtc) = (toUtc, fromUtc);
        var userId = CurrentUserId();
        groupBy = (groupBy ?? "day").ToLowerInvariant();

        var baseQuery = _db.WorkoutSessions
            .AsNoTracking()
            .Where(ws => ws.UserId == userId && ws.CompletedAtUtc != null && 
                         ws.StartedAtUtc >= fromUtc && ws.StartedAtUtc < toUtc); 

        var flatQuery = baseQuery
            .SelectMany(ws => ws.Exercises.SelectMany(se => se.Sets.Select(ss => new
            {
                Date = ws.StartedAtUtc.Date,
                ExerciseName = se.Name,
                Volume = (decimal)((ss.RepsDone ?? 0) * (double)(ss.WeightDone ?? 0))
            })));

        if (!string.IsNullOrWhiteSpace(exerciseName))
        {
            flatQuery = flatQuery.Where(x => x.ExerciseName == exerciseName);
        }

        if (groupBy == "exercise")
        {
            return await flatQuery
                .GroupBy(x => x.ExerciseName)
                .Select(g => new TimePointDto
                {
                    Period = g.Key,
                    ExerciseName = g.Key,
                    Value = g.Sum(x => x.Volume)
                })
                .OrderByDescending(x => x.Value)
                .ToListAsync(ct);
        }

        var dailyData = await flatQuery
            .GroupBy(x => x.Date)
            .Select(g => new
            {
                Date = g.Key,
                TotalVolume = g.Sum(x => x.Volume)
            })
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

        if (groupBy == "week")
        {
            return dailyData
                .GroupBy(x => $"{ISOWeek.GetYear(x.Date):D4}-W{ISOWeek.GetWeekOfYear(x.Date):D2}")
                .Select(g => new TimePointDto
                {
                    Period = g.Key,
                    Value = g.Sum(x => x.TotalVolume)
                })
                .ToList();
        }

        return dailyData
            .Select(x => new TimePointDto
            {
                Period = x.Date.ToString("yyyy-MM-dd"),
                Value = x.TotalVolume
            })
            .ToList();
    }

    public async Task<IReadOnlyList<E1rmPointDto>> GetE1RmAsync(string exerciseName, DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        if (fromUtc > toUtc) (fromUtc, toUtc) = (toUtc, fromUtc);
        var userId = CurrentUserId();
        
        var query = _db.WorkoutSessions
            .AsNoTracking()
            .Where(ws => ws.UserId == userId && ws.CompletedAtUtc != null && 
                         ws.StartedAtUtc >= fromUtc && ws.StartedAtUtc < toUtc)
            .SelectMany(ws => ws.Exercises
                .Where(se => se.Name == exerciseName)
                .SelectMany(se => se.Sets
                    .Where(ss => ss.RepsDone != null && ss.WeightDone != null && ss.RepsDone > 0)
                    .Select(ss => new 
                    { 
                        Date = ws.StartedAtUtc.Date,
                        SessionId = ws.Id,
                        CalculatedE1RM = (decimal)ss.WeightDone! * (1 + (decimal)ss.RepsDone! / 30m)
                    })));

        var groupedData = await query
            .GroupBy(x => x.Date)
            .Select(g => new 
            {
                Day = g.Key,
                MaxE1RM = g.Max(x => x.CalculatedE1RM)
            })
            .OrderBy(x => x.Day)
            .ToListAsync(ct);

        return groupedData.Select(x => new E1rmPointDto
        {
            Day = DateOnly.FromDateTime(x.Day),
            E1Rm = Math.Round(x.MaxE1RM, 2),
            SessionId = null 
        }).ToList();
    }

    public async Task<AdherenceDto> GetAdherenceAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct)
    {
        if (fromDate > toDate) (fromDate, toDate) = (toDate, fromDate);
        var userId = CurrentUserId();

        int planned = await _db.ScheduledWorkouts
            .AsNoTracking()
            .CountAsync(sw => sw.UserId == userId && sw.Date >= fromDate && sw.Date <= toDate, ct);

        int completed = await _db.ScheduledWorkouts
            .AsNoTracking()
            .CountAsync(sw => sw.UserId == userId && sw.Status == ScheduledStatus.Completed && 
                              sw.Date >= fromDate && sw.Date <= toDate, ct);

        return new AdherenceDto { Planned = planned, Completed = completed };
    }

    public async Task<IReadOnlyList<PlanVsActualItemDto>> GetPlanVsActualAsync(Guid sessionId, CancellationToken ct)
    {
        var userId = CurrentUserId();
        
        var sessionExists = await _db.WorkoutSessions
            .AnyAsync(s => s.Id == sessionId && s.UserId == userId, ct);
        
        if (!sessionExists) return Array.Empty<PlanVsActualItemDto>();

        var items = await _db.SessionExercises
            .AsNoTracking()
            .Where(se => se.WorkoutSessionId == sessionId)
            .OrderBy(se => se.Order)
            .SelectMany(se => se.Sets
                .OrderBy(ss => ss.SetNumber)
                .Select(ss => new PlanVsActualItemDto
                {
                    ExerciseName = se.Name,
                    SetNumber = ss.SetNumber,
                    RepsPlanned = ss.RepsPlanned,
                    WeightPlanned = ss.WeightPlanned,
                    RepsDone = ss.RepsDone,
                    WeightDone = ss.WeightDone,
                    Rpe = ss.Rpe,
                    IsFailure = ss.IsFailure,
                    IsExtra = se.IsAdHoc 
                }))
            .ToListAsync(ct);

        return items;
    }
}