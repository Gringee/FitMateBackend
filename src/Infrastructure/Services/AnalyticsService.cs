using Application.Abstractions;
using Application.DTOs.Analytics;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Infrastructure.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _db;
    public AnalyticsService(AppDbContext db) => _db = db;

    // KPI: volume / avg intensity / sessions / adherence / new PRs (0 na razie)
    public async Task<OverviewDto> GetOverviewAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        var sessions = await _db.WorkoutSessions
            .AsNoTracking()
            .Where(ws => ws.CompletedAtUtc != null && ws.StartedAtUtc >= fromUtc && ws.StartedAtUtc < toUtc)
            .Select(ws => new {
                ws.Id,
                Sets = ws.Exercises.SelectMany(se => se.Sets)
            })
            .ToListAsync(ct);

        var allSets = sessions.SelectMany(s => s.Sets);

        decimal totalVolume = allSets.Sum(s =>
            (decimal)((s.RepsDone ?? s.RepsPlanned) * (double)(s.WeightDone ?? s.WeightPlanned)));

        var intensities = allSets
            .Where(s => s.WeightDone != null)
            .Select(s => (double)s.WeightDone!)
            .ToList();

        double avgIntensity = intensities.Count == 0 ? 0 : intensities.Average();
        int sessionsCount = sessions.Count;

        // adherence z scheduled_workouts (po datach dziennych)
        DateOnly fromDate = DateOnly.FromDateTime(fromUtc);
        DateOnly toDate = DateOnly.FromDateTime(toUtc.AddDays(-0)); // to exclusive -> interpretuj ostrożnie

        int planned = await _db.ScheduledWorkouts
            .AsNoTracking()
            .CountAsync(sw => sw.Date >= fromDate && sw.Date < toDate, ct);

        int completed = await _db.ScheduledWorkouts
            .AsNoTracking()
            .CountAsync(sw => sw.Status.ToString() == "Completed" && sw.Date >= fromDate && sw.Date < toDate, ct);

        return new OverviewDto
        {
            TotalVolume = decimal.Round(totalVolume, 2),
            AvgIntensity = Math.Round(avgIntensity, 2),
            SessionsCount = sessionsCount,
            AdherencePct = planned == 0 ? 0 : Math.Round(completed * 100.0 / planned, 1),
            NewPrs = 0
        };
    }

    // Volume agregowane wg groupBy: day | week | exercise
    public async Task<IReadOnlyList<TimePointDto>> GetVolumeAsync(DateTime fromUtc, DateTime toUtc, string groupBy, string? exerciseName, CancellationToken ct)
    {
        groupBy = (groupBy ?? "day").ToLowerInvariant();

        var q = _db.WorkoutSessions
            .AsNoTracking()
            .Where(ws => ws.CompletedAtUtc != null && ws.StartedAtUtc >= fromUtc && ws.StartedAtUtc < toUtc)
            .SelectMany(ws => ws.Exercises.SelectMany(se => se.Sets.Select(ss => new
            {
                ws.StartedAtUtc,
                se.Name,
                Volume = (decimal)((ss.RepsDone ?? ss.RepsPlanned) * (double)(ss.WeightDone ?? ss.WeightPlanned))
            })));

        if (!string.IsNullOrWhiteSpace(exerciseName))
            q = q.Where(x => x.Name == exerciseName);

        if (groupBy == "exercise")
        {
            var list = await q
                .GroupBy(x => x.Name)
                .Select(g => new TimePointDto
                {
                    Period = g.Key,
                    Value = g.Sum(x => x.Volume),
                    ExerciseName = g.Key
                })
                .OrderBy(x => x.Period)
                .ToListAsync(ct);
            return list;
        }

        // day/week
        var data = await q.ToListAsync(ct);
        var result = data
            .GroupBy(x => groupBy == "week"
                ? $"{ISOWeek.GetYear(x.StartedAtUtc):D4}-W{ISOWeek.GetWeekOfYear(x.StartedAtUtc):D2}"
                : x.StartedAtUtc.ToString("yyyy-MM-dd"))
            .Select(g => new TimePointDto { Period = g.Key, Value = g.Sum(x => x.Volume) })
            .OrderBy(x => x.Period)
            .ToList();

        return result;
    }

    // Epley e1RM: weight * (1 + reps/30)
    public async Task<IReadOnlyList<E1rmPointDto>> GetE1RmAsync(string exerciseName, DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        var rows = await _db.WorkoutSessions
            .AsNoTracking()
            .Where(ws => ws.CompletedAtUtc != null && ws.StartedAtUtc >= fromUtc && ws.StartedAtUtc < toUtc)
            .SelectMany(ws => ws.Exercises
                .Where(se => se.Name == exerciseName)
                .SelectMany(se => se.Sets
                    .Where(ss => ss.RepsDone != null && ss.WeightDone != null)
                    .Select(ss => new { ws.Id, ws.StartedAtUtc, ss.RepsDone, ss.WeightDone })))
            .ToListAsync(ct);

        var grouped = rows
            .GroupBy(r => DateOnly.FromDateTime(r.StartedAtUtc.Date))
            .Select(g => new E1rmPointDto
            {
                Day = g.Key,
                E1Rm = g.Max(r => r.WeightDone!.Value * (1 + (decimal)r.RepsDone!.Value / 30m)),
                SessionId = rows.FirstOrDefault(r => DateOnly.FromDateTime(r.StartedAtUtc.Date) == g.Key)?.Id
            })
            .OrderBy(x => x.Day)
            .ToList();

        return grouped;
    }

    public async Task<AdherenceDto> GetAdherenceAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct)
    {
        int planned = await _db.ScheduledWorkouts
            .AsNoTracking()
            .CountAsync(sw => sw.Date >= fromDate && sw.Date < toDate, ct);

        int completed = await _db.ScheduledWorkouts
            .AsNoTracking()
            .CountAsync(sw => sw.Status.ToString() == "Completed" && sw.Date >= fromDate && sw.Date < toDate, ct);

        return new AdherenceDto { Planned = planned, Completed = completed };
    }

    public async Task<IReadOnlyList<PlanVsActualItemDto>> GetPlanVsActualAsync(Guid sessionId, CancellationToken ct)
    {
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
                    IsFailure = ss.IsFailure
                }))
            .ToListAsync(ct);

        return items;
    }
}