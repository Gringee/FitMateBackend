using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions;
using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class WorkoutSessionService : IWorkoutSessionService
{
    private readonly AppDbContext _db;

    public WorkoutSessionService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Rozpoczyna sesję realizacji na podstawie zaplanowanego treningu (ScheduledWorkout).
    /// Tworzy snapshot ćwiczeń/serii i ustawia status sesji na "in_progress".
    /// </summary>
    public async Task<WorkoutSessionDto> StartAsync(StartSessionRequest req, CancellationToken ct)
    {
        var sch = await _db.ScheduledWorkouts
            .Include(x => x.Exercises)
            .ThenInclude(e => e.Sets)
            .FirstOrDefaultAsync(x => x.Id == req.ScheduledId, ct)
            ?? throw new KeyNotFoundException("Scheduled workout not found");

        var session = new WorkoutSession
        {
            Id = Guid.NewGuid(),
            ScheduledId = sch.Id,
            StartedAtUtc = DateTime.UtcNow,
            Status = "in_progress",
            // zachowujemy kolejność taką, jak w ScheduledWorkout (indeksy)
            Exercises = sch.Exercises
                .Select((e, idx) => new SessionExercise
                {
                    Id = Guid.NewGuid(),
                    Order = idx + 1, // stabilny identyfikator w trakcie sesji
                    Name = e.Name,
                    RestSecPlanned = e.RestSeconds,
                    Sets = e.Sets
                        .OrderBy(s => s.SetNumber)
                        .Select(s => new SessionSet
                        {
                            Id = Guid.NewGuid(),
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

    /// <summary>
    /// Aktualizuje wyniki pojedynczej serii w trakcie sesji (optymistyczny model UI).
    /// </summary>
    public async Task<WorkoutSessionDto> PatchSetAsync(Guid sessionId, PatchSetRequest req, CancellationToken ct)
    {
        var sess = await _db.WorkoutSessions
            .Include(s => s.Exercises)
            .ThenInclude(e => e.Sets)
            .FirstOrDefaultAsync(x => x.Id == sessionId, ct)
            ?? throw new KeyNotFoundException("Session not found");

        if (!string.Equals(sess.Status, "in_progress", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Session not in progress");

        var exercise = sess.Exercises.FirstOrDefault(x => x.Order == req.ExerciseOrder)
            ?? throw new KeyNotFoundException("Exercise not found");

        var set = exercise.Sets.FirstOrDefault(x => x.SetNumber == req.SetNumber)
            ?? throw new KeyNotFoundException("Set not found");

        set.RepsDone = req.RepsDone;
        set.WeightDone = req.WeightDone;
        set.Rpe = req.Rpe;
        set.IsFailure = req.IsFailure;

        await _db.SaveChangesAsync(ct);
        return await MapDtoAsync(sessionId, ct);
    }

    /// <summary>
    /// Kończy sesję, uzupełnia czasy i (opcjonalnie) oznacza ScheduledWorkout jako Completed.
    /// </summary>
    public async Task<WorkoutSessionDto> CompleteAsync(Guid sessionId, CompleteSessionRequest req, CancellationToken ct)
    {
        var sess = await _db.WorkoutSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId, ct)
            ?? throw new KeyNotFoundException("Session not found");

        if (!string.Equals(sess.Status, "in_progress", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Session not in progress");

        sess.Status = "completed";
        sess.CompletedAtUtc = req.CompletedAtUtc ?? DateTime.UtcNow;
        sess.DurationSec = (int)Math.Max(0, (sess.CompletedAtUtc.Value - sess.StartedAtUtc).TotalSeconds);

        if (!string.IsNullOrWhiteSpace(req.SessionNotes))
            sess.SessionNotes = req.SessionNotes;
        
        var sch = await _db.ScheduledWorkouts.FirstOrDefaultAsync(x => x.Id == sess.ScheduledId, ct);
        if (sch is not null && sch.Status == ScheduledStatus.Planned)
            sch.Status = ScheduledStatus.Completed;

        await _db.SaveChangesAsync(ct);
        return await MapDtoAsync(sessionId, ct);
    }

    /// <summary>
    /// Przerywa sesję i zamyka ją z odpowiednim statusem.
    /// </summary>
    public async Task<WorkoutSessionDto> AbortAsync(Guid sessionId, AbortSessionRequest req, CancellationToken ct)
    {
        var sess = await _db.WorkoutSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId, ct)
            ?? throw new KeyNotFoundException("Session not found");

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
        var exists = await _db.WorkoutSessions.AnyAsync(x => x.Id == id, ct);
        if (!exists) return null;

        return await MapDtoAsync(id, ct);
    }

    public async Task<IReadOnlyList<WorkoutSessionDto>> GetByRangeAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        var ids = await _db.WorkoutSessions
            .Where(x => x.StartedAtUtc >= fromUtc && x.StartedAtUtc < toUtc)
            .OrderByDescending(x => x.StartedAtUtc)
            .Select(x => x.Id)
            .ToListAsync(ct);

        var result = new List<WorkoutSessionDto>(ids.Count);
        foreach (var id in ids)
            result.Add(await MapDtoAsync(id, ct));

        return result;
    }

    // --------------------- prywatne mapowanie ---------------------

    private async Task<WorkoutSessionDto> MapDtoAsync(Guid id, CancellationToken ct)
    {
        var s = await _db.WorkoutSessions
            .AsNoTracking()
            .Include(x => x.Exercises)
            .ThenInclude(e => e.Sets)
            .FirstAsync(x => x.Id == id, ct);

        return new WorkoutSessionDto
        {
            Id = s.Id,
            ScheduledId = s.ScheduledId,
            StartedAtUtc = s.StartedAtUtc,
            CompletedAtUtc = s.CompletedAtUtc,
            DurationSec = s.DurationSec,
            Status = s.Status,
            SessionNotes = s.SessionNotes,
            Exercises = s.Exercises
                .OrderBy(e => e.Order)
                .Select(e => new SessionExerciseDto
                {
                    Order = e.Order,
                    Name = e.Name,
                    RestSecPlanned = e.RestSecPlanned,
                    RestSecActual = e.RestSecActual,
                    Sets = e.Sets
                        .OrderBy(z => z.SetNumber)
                        .Select(z => new SessionSetDto
                        {
                            SetNumber   = z.SetNumber,
                            RepsPlanned = z.RepsPlanned,
                            WeightPlanned = z.WeightPlanned,
                            RepsDone    = z.RepsDone,
                            WeightDone  = z.WeightDone,
                            Rpe         = z.Rpe,
                            IsFailure   = z.IsFailure
                        })
                        .ToList()
                })
                .ToList()
        };
    }
}