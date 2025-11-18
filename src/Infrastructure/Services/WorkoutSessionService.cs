using Application.Abstractions;
using Application.DTOs;
using Application.Common.Security; // Extension GetUserId()
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class WorkoutSessionService : IWorkoutSessionService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;

    public WorkoutSessionService(AppDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    private Guid UserId => _http.HttpContext?.User.GetUserId() 
                           ?? throw new UnauthorizedAccessException("No HttpContext/User.");

    public async Task<WorkoutSessionDto> StartAsync(StartSessionRequest req, CancellationToken ct)
    {
        var userId = UserId;

        var sch = await _db.ScheduledWorkouts.Include(x => x.Exercises)
                      .ThenInclude(e => e.Sets)
                      .FirstOrDefaultAsync(x => x.Id == req.ScheduledId && x.UserId == userId, ct) ??
                  throw new KeyNotFoundException("Scheduled workout not found");

        var session = new WorkoutSession
        {
            Id = Guid.NewGuid(),
            ScheduledId = sch.Id,
            StartedAtUtc = DateTime.UtcNow,
            Status = "in_progress",
            UserId = userId,
            Exercises = sch.Exercises
                .Select((e, idx) => new SessionExercise
                {
                    Id = Guid.NewGuid(),
                    WorkoutSessionId = Guid.Empty, 
                    Order = idx + 1,
                    Name = e.Name,
                    RestSecPlanned = e.RestSeconds,
                    RestSecActual = null,
                    IsAdHoc = false,                    
                    ScheduledExerciseId = e.Id,         
                    Sets = e.Sets
                        .OrderBy(s => s.SetNumber)
                        .Select(s => new SessionSet
                        {
                            Id = Guid.NewGuid(),
                            SessionExerciseId = Guid.Empty, 
                            SetNumber = s.SetNumber,
                            RepsPlanned = s.Reps,
                            WeightPlanned = s.Weight
                        })
                        .ToList()
                })
                .ToList()
        };

        _db.WorkoutSessions.Add(session);
        await _db.SaveChangesAsync(ct);

        return await MapDtoAsync(session.Id, ct);
    }

    public async Task<WorkoutSessionDto> PatchSetAsync(Guid sessionId, Guid setId, PatchSetRequest req, CancellationToken ct)
    {
        var userId = UserId;

        var sess = await _db.WorkoutSessions
                       .FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, ct) ??
                   throw new KeyNotFoundException("Session not found");

        if (!string.Equals(sess.Status, "in_progress", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Session not in progress");

        var set = await _db.SessionExercises
            .Where(se => se.WorkoutSessionId == sessionId)
            .SelectMany(se => se.Sets)
            .FirstOrDefaultAsync(s => s.Id == setId, ct);

        if (set == null)
            throw new KeyNotFoundException("Set not found in this session");

        if (req.RepsDone.HasValue) set.RepsDone = req.RepsDone;
        if (req.WeightDone.HasValue) set.WeightDone = req.WeightDone;
        if (req.Rpe.HasValue) set.Rpe = req.Rpe;
        if (req.IsFailure.HasValue) set.IsFailure = req.IsFailure;

        await _db.SaveChangesAsync(ct);
    
        return await MapDtoAsync(sessionId, ct);
    }

    public async Task<WorkoutSessionDto> CompleteAsync(Guid sessionId, CompleteSessionRequest req, CancellationToken ct)
    {
        var userId = UserId;

        var sess = await _db.WorkoutSessions.FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, ct) ??
                   throw new KeyNotFoundException("Session not found");

        if (!string.Equals(sess.Status, "in_progress", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Session not in progress");

        DateTime completedAtUtc;
        if (req.CompletedAtUtc is null)
        {
            completedAtUtc = DateTime.UtcNow;
        }
        else if (req.CompletedAtUtc.Value.Kind == DateTimeKind.Unspecified)
        {
            completedAtUtc = DateTime.SpecifyKind(req.CompletedAtUtc.Value, DateTimeKind.Utc);
        }
        else if (req.CompletedAtUtc.Value.Kind == DateTimeKind.Local)
        {
            completedAtUtc = req.CompletedAtUtc.Value.ToUniversalTime();
        }
        else
        {
            completedAtUtc = req.CompletedAtUtc.Value;
        }

        sess.Status = "completed";
        sess.CompletedAtUtc = completedAtUtc;
        sess.DurationSec = (int)Math.Max(0, (sess.CompletedAtUtc.Value - sess.StartedAtUtc).TotalSeconds);

        if (!string.IsNullOrWhiteSpace(req.SessionNotes)) sess.SessionNotes = req.SessionNotes;

        var sch = await _db.ScheduledWorkouts.FirstOrDefaultAsync(x => x.Id == sess.ScheduledId && x.UserId == userId, ct);
        if (sch is not null && sch.Status == ScheduledStatus.Planned) 
            sch.Status = ScheduledStatus.Completed;

        await _db.SaveChangesAsync(ct);
        return await MapDtoAsync(sessionId, ct);
    }

    public async Task<WorkoutSessionDto> AbortAsync(Guid sessionId, AbortSessionRequest req, CancellationToken ct)
    {
        var userId = UserId;

        var sess = await _db.WorkoutSessions.FirstOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, ct) ??
                   throw new KeyNotFoundException("Session not found");

        if (!string.Equals(sess.Status, "in_progress", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Session not in progress");

        sess.Status = "aborted";
        sess.CompletedAtUtc = DateTime.UtcNow;
        sess.DurationSec = (int)Math.Max(0, (sess.CompletedAtUtc.Value - sess.StartedAtUtc).TotalSeconds);

        if (!string.IsNullOrWhiteSpace(req.Reason))
            sess.SessionNotes = string.IsNullOrWhiteSpace(sess.SessionNotes)
                ? $"Aborted: {req.Reason}"
                : $"{sess.SessionNotes}\nAborted: {req.Reason}";

        await _db.SaveChangesAsync(ct);
        return await MapDtoAsync(sessionId, ct);
    }

    public async Task<WorkoutSessionDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var userId = UserId;
        var exists = await _db.WorkoutSessions.AnyAsync(x => x.Id == id && x.UserId == userId, ct);
        if (!exists) return null;

        return await MapDtoAsync(id, ct);
    }

    public async Task<IReadOnlyList<WorkoutSessionDto>> GetByRangeAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        var userId = UserId;

        var sessions = await _db.WorkoutSessions.AsNoTracking()
            .Include(x => x.Exercises).ThenInclude(e => e.Sets)
            .Where(x => x.UserId == userId && x.StartedAtUtc >= fromUtc && x.StartedAtUtc < toUtc)
            .OrderByDescending(x => x.StartedAtUtc)
            .ToListAsync(ct);

        return sessions.Select(s => MapToDto(s)).ToList();
    }

    public async Task<WorkoutSessionDto> AddExerciseAsync(Guid sessionId, AddSessionExerciseRequest req, CancellationToken ct)
{
    var userId = UserId;
    
    var sess = await _db.WorkoutSessions
                   .AsNoTracking() 
                   .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId, ct) ??
               throw new KeyNotFoundException("Session not found");

    if (!string.Equals(sess.Status, "in_progress", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Cannot add exercises to a non in-progress session.");
    
    var maxOrder = await _db.SessionExercises
        .Where(e => e.WorkoutSessionId == sessionId)
        .Select(e => (int?)e.Order)
        .MaxAsync(ct) ?? 0;

    var targetOrder = maxOrder + 1;
    
    var exercise = new SessionExercise
    {
        Id = Guid.NewGuid(),
        WorkoutSessionId = sessionId, 
        Order = targetOrder,
        Name = req.Name,
        RestSecPlanned = req.RestSecPlanned ?? 0,
        RestSecActual = null,
        IsAdHoc = true,
        ScheduledExerciseId = null
    };

    var nextSetNumber = 1;
    foreach (var s in req.Sets)
    {
        exercise.Sets.Add(new SessionSet
        {
            Id = Guid.NewGuid(),
            SessionExerciseId = exercise.Id,
            SetNumber = nextSetNumber++,
            RepsPlanned = s.RepsPlanned,
            WeightPlanned = s.WeightPlanned,
            RepsDone = null,
            WeightDone = null,
            Rpe = null,
            IsFailure = null
        });
    }
    
    _db.SessionExercises.Add(exercise);
    
    await _db.SaveChangesAsync(ct);
    
    return await MapDtoAsync(sessionId, ct);
}
    private async Task<WorkoutSessionDto> MapDtoAsync(Guid id, CancellationToken ct)
    {
        var userId = UserId;
        var s = await _db.WorkoutSessions.AsNoTracking()
            .Include(x => x.Exercises).ThenInclude(e => e.Sets)
            .FirstAsync(x => x.Id == id && x.UserId == userId, ct);

        return MapToDto(s);
    }
    
    private static WorkoutSessionDto MapToDto(WorkoutSession s) => new()
    {
        Id = s.Id,
        ScheduledId = s.ScheduledId,
        StartedAtUtc = s.StartedAtUtc,
        CompletedAtUtc = s.CompletedAtUtc,
        DurationSec = s.DurationSec,
        Status = s.Status,
        SessionNotes = s.SessionNotes,
        Exercises = s.Exercises.OrderBy(e => e.Order)
            .Select(e => new SessionExerciseDto
            {
                Id = e.Id,
                Order = e.Order,
                Name = e.Name,
                RestSecPlanned = e.RestSecPlanned,
                RestSecActual = e.RestSecActual,
                Sets = e.Sets.OrderBy(z => z.SetNumber)
                    .Select(z => new SessionSetDto
                    {
                        Id = z.Id,
                        SetNumber = z.SetNumber,
                        RepsPlanned = z.RepsPlanned,
                        WeightPlanned = z.WeightPlanned,
                        RepsDone = z.RepsDone,
                        WeightDone = z.WeightDone,
                        Rpe = z.Rpe,
                        IsFailure = z.IsFailure
                    })
                    .ToList()
            })
            .ToList()
    };
}