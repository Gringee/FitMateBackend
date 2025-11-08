using System.Globalization;
using Application.Abstractions;
using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;              
using Application.Common.Security;          

namespace Infrastructure.Services;

public sealed class ScheduledService : IScheduledService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http; 

    public ScheduledService(AppDbContext db, IHttpContextAccessor http) 
    {
        _db = db;
        _http = http;
    }

    private Guid CurrentUserId()
    {
        var user = _http.HttpContext?.User ?? throw new UnauthorizedAccessException("No HttpContext/User.");
        return user.GetUserId();
    }

    public async Task<ScheduledDto> CreateAsync(CreateScheduledDto dto, CancellationToken ct = default)
    {
        var userId = CurrentUserId();

        var (d, t) = ParseDateTime(dto.Date, dto.Time);
        var status = ParseStatus(dto.Status);
        
        var plan = await _db.Plans
            .Include(p => p.Exercises).ThenInclude(e => e.Sets)
            .FirstOrDefaultAsync(p => p.Id == dto.PlanId && p.CreatedByUserId == userId, ct)
            ?? throw new KeyNotFoundException("Plan not found");

        var sw = new ScheduledWorkout
        {
            Id = Guid.NewGuid(),
            Date = d,
            Time = t,
            PlanId = plan.Id,
            PlanName = dto.PlanName ?? plan.PlanName,
            Notes = dto.Notes ?? plan.Notes,
            Status = status,
            UserId = userId 
        };

        var exercisesSrc = (dto.Exercises is { Count: > 0 })
            ? dto.Exercises.Select(e => (name: e.Name, rest: e.Rest, sets: e.Sets))
            : plan.Exercises.Select(e => (
                name: e.Name,
                rest: e.RestSeconds,
                sets: e.Sets
                    .Select(s => new SetDto { Reps = s.Reps, Weight = s.Weight })
                    .ToList()
              ));

        foreach (var ex in exercisesSrc)
        {
            var se = new ScheduledExercise
            {
                Id = Guid.NewGuid(),
                ScheduledWorkoutId = sw.Id,
                Name = ex.name,
                RestSeconds = ex.rest
            };
            int i = 1;
            foreach (var s in ex.sets)
            {
                se.Sets.Add(new ScheduledSet
                {
                    Id = Guid.NewGuid(),
                    ScheduledExerciseId = se.Id,
                    SetNumber = i++,
                    Reps = s.Reps,
                    Weight = s.Weight
                });
            }
            sw.Exercises.Add(se);
        }

        _db.Add(sw);
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(sw.Id, ct) ?? throw new InvalidOperationException();
    }

    public async Task<List<ScheduledDto>> GetAllAsync(CancellationToken ct = default)
    {
        var userId = CurrentUserId();

        var list = await _db.ScheduledWorkouts
            .Include(s => s.Exercises).ThenInclude(e => e.Sets)
            .Where(s => s.UserId == userId)
            .AsNoTracking()
            .OrderBy(s => s.Date).ThenBy(s => s.Time)
            .ToListAsync(ct);

        return list.Select(Map).ToList();
    }

    public async Task<ScheduledDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var userId = CurrentUserId();

        var s = await _db.ScheduledWorkouts
            .Include(x => x.Exercises).ThenInclude(e => e.Sets)
            .Where(x => x.UserId == userId)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return s is null ? null : Map(s);
    }

    public async Task<List<ScheduledDto>> GetByDateAsync(string yyyyMMdd, CancellationToken ct = default)
    {
        var userId = CurrentUserId();

        var date = DateOnly.ParseExact(yyyyMMdd, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var list = await _db.ScheduledWorkouts
            .Include(s => s.Exercises).ThenInclude(e => e.Sets)
            .Where(s => s.UserId == userId && s.Date == date)
            .AsNoTracking()
            .OrderBy(s => s.Time)
            .ToListAsync(ct);

        return list.Select(Map).ToList();
    }

    public async Task<ScheduledDto?> UpdateAsync(Guid id, CreateScheduledDto dto, CancellationToken ct = default)
    {
        var userId = CurrentUserId();

        var sw = await _db.ScheduledWorkouts
            .Include(s => s.Exercises).ThenInclude(e => e.Sets)
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId, ct);
        if (sw is null) return null;

        var (d, t) = ParseDateTime(dto.Date, dto.Time);
        sw.Date = d;
        sw.Time = t;
        sw.PlanId = dto.PlanId;
        sw.PlanName = dto.PlanName;
        sw.Notes = dto.Notes;
        sw.Status = ParseStatus(dto.Status);

        _db.RemoveRange(sw.Exercises.SelectMany(e => e.Sets));
        _db.RemoveRange(sw.Exercises);
        sw.Exercises.Clear();

        foreach (var ex in dto.Exercises)
        {
            var se = new ScheduledExercise
            {
                Id = Guid.NewGuid(),
                ScheduledWorkoutId = sw.Id,
                Name = ex.Name,
                RestSeconds = ex.Rest
            };
            var i = 1;
            foreach (var s in ex.Sets)
            {
                se.Sets.Add(new ScheduledSet
                {
                    Id = Guid.NewGuid(),
                    ScheduledExerciseId = se.Id,
                    SetNumber = i++,
                    Reps = s.Reps,
                    Weight = s.Weight
                });
            }
            sw.Exercises.Add(se);
        }

        await _db.SaveChangesAsync(ct);
        return Map(sw);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var userId = CurrentUserId();

        var sw = await _db.ScheduledWorkouts
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (sw is null) return false;

        _db.Remove(sw);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ScheduledDto?> DuplicateAsync(Guid id, CancellationToken ct = default)
    {
        var userId = CurrentUserId(); // ← NEW

        var s = await _db.ScheduledWorkouts
            .Include(x => x.Exercises).ThenInclude(e => e.Sets)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (s is null) return null;

        var copy = new ScheduledWorkout
        {
            Id = Guid.NewGuid(),
            Date = s.Date,
            Time = s.Time,
            PlanId = s.PlanId,
            PlanName = s.PlanName,
            Notes = s.Notes,
            Status = s.Status,
            UserId = userId 
        };
        foreach (var ex in s.Exercises)
        {
            var se = new ScheduledExercise
            {
                Id = Guid.NewGuid(),
                ScheduledWorkoutId = copy.Id,
                Name = ex.Name,
                RestSeconds = ex.RestSeconds
            };
            foreach (var set in ex.Sets.OrderBy(x => x.SetNumber))
            {
                se.Sets.Add(new ScheduledSet
                {
                    Id = Guid.NewGuid(),
                    ScheduledExerciseId = se.Id,
                    SetNumber = set.SetNumber,
                    Reps = set.Reps,
                    Weight = set.Weight
                });
            }
            copy.Exercises.Add(se);
        }

        _db.Add(copy);
        await _db.SaveChangesAsync(ct);
        return Map(copy);
    }
    
    private static (DateOnly, TimeOnly?) ParseDateTime(string date, string? time)
    {
        var d = DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        TimeOnly? t = string.IsNullOrWhiteSpace(time) ? null : TimeOnly.ParseExact(time!, "HH:mm", CultureInfo.InvariantCulture);
        return (d, t);
    }

    private static ScheduledStatus ParseStatus(string? s)
        => string.IsNullOrWhiteSpace(s) ? ScheduledStatus.Planned
           : s.Trim().ToLowerInvariant() == "completed" ? ScheduledStatus.Completed : ScheduledStatus.Planned;

    private static ScheduledDto Map(ScheduledWorkout s) => new()
    {
        Id = s.Id,
        Date = s.Date.ToString("yyyy-MM-dd"),
        Time = s.Time?.ToString("HH:mm"),
        PlanId = s.PlanId,
        PlanName = s.PlanName,
        Notes = s.Notes,
        Status = s.Status == ScheduledStatus.Completed ? "completed" : "planned",
        Exercises = s.Exercises
            .OrderBy(e => e.Id)
            .Select(e => new ExerciseDto
            {
                Name = e.Name,
                Rest = e.RestSeconds,
                Sets = e.Sets.OrderBy(x => x.SetNumber)
                             .Select(x => new SetDto { Reps = x.Reps, Weight = x.Weight }).ToList()
            }).ToList()
    };
}