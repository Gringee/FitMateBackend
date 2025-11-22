using System.Globalization;
using Application.Abstractions;
using Application.DTOs;
using Application.Common.Security; // Extension GetUserId()
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public sealed class ScheduledService : IScheduledService
{
    private readonly IApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;

    public ScheduledService(IApplicationDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }
    private Guid UserId => _http.HttpContext?.User.GetUserId() 
                           ?? throw new UnauthorizedAccessException("No HttpContext/User.");

    public async Task<ScheduledDto> CreateAsync(CreateScheduledDto dto, CancellationToken ct = default)
    {
        var userId = UserId;
        var status = ParseStatus(dto.Status);

        var plan = await _db.Plans
                       .Include(p => p.Exercises).ThenInclude(e => e.Sets)
                       .FirstOrDefaultAsync(p => p.Id == dto.PlanId && p.CreatedByUserId == userId, ct)
                   ?? throw new KeyNotFoundException("Plan not found");

        var sw = new ScheduledWorkout
        {
            Id = Guid.NewGuid(),
            Date = dto.Date, 
            Time = dto.Time, 
            PlanId = plan.Id,
            PlanName = string.IsNullOrWhiteSpace(dto.PlanName) ? plan.PlanName : dto.PlanName,
            Notes = dto.Notes ?? plan.Notes,
            Status = status,
            UserId = userId,
            IsVisibleToFriends = dto.VisibleToFriends
        };

        var exercisesSrc = (dto.Exercises is { Count: > 0 })
            ? dto.Exercises.Select(e => (name: e.Name, rest: e.Rest, sets: e.Sets))
            : plan.Exercises.Select(e => (
                name: e.Name,
                rest: e.RestSeconds,
                sets: (IReadOnlyList<SetDto>)e.Sets.Select(s => new SetDto { Reps = s.Reps, Weight = s.Weight }).ToList()
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

        var created = await _db.ScheduledWorkouts
            .Include(x => x.Exercises).ThenInclude(e => e.Sets)
            .FirstAsync(x => x.Id == sw.Id, ct);
            
        return Map(created);
    }

    public async Task<IReadOnlyList<ScheduledDto>> GetAllAsync(CancellationToken ct = default)
    {
        var userId = UserId;

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
        var userId = UserId;

        var s = await _db.ScheduledWorkouts
            .Include(x => x.Exercises).ThenInclude(e => e.Sets)
            .Where(x => x.UserId == userId)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return s is null ? null : Map(s);
    }

    public async Task<IReadOnlyList<ScheduledDto>> GetByDateAsync(string yyyyMMdd, CancellationToken ct = default)
    {
        var userId = UserId;

        if (!DateOnly.TryParseExact(yyyyMMdd, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            throw new FormatException("Invalid date format. Use yyyy-MM-dd.");
        }
        
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
        var userId = UserId;

        var existing = await _db.ScheduledWorkouts
            .Include(s => s.Exercises).ThenInclude(e => e.Sets)
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId, ct);

        if (existing is null) return null;

        var plan = await _db.Plans
                       .Include(p => p.Exercises).ThenInclude(e => e.Sets)
                       .FirstOrDefaultAsync(p => p.Id == dto.PlanId && p.CreatedByUserId == userId, ct)
                   ?? throw new KeyNotFoundException("Plan not found");

        bool planChanged = existing.PlanId != dto.PlanId;

        existing.Date = dto.Date;
        existing.Time = dto.Time;
        existing.PlanId = plan.Id;

        existing.PlanName = string.IsNullOrWhiteSpace(dto.PlanName) ? plan.PlanName : dto.PlanName;
        
        existing.Notes = dto.Notes ?? plan.Notes;
        existing.Status = ParseStatus(dto.Status);
        existing.IsVisibleToFriends = dto.VisibleToFriends;

        bool shouldUpdateExercises = false;

        IEnumerable<(string name, int rest, IReadOnlyList<SetDto> sets)>? exercisesSrc = null;

        if (dto.Exercises is { Count: > 0 })
        {
            shouldUpdateExercises = true;
            exercisesSrc = dto.Exercises.Select(e => (e.Name, e.Rest, e.Sets));
        }
        else if (planChanged)
        {
            shouldUpdateExercises = true;
            exercisesSrc = plan.Exercises.Select(e => (
                e.Name, 
                e.RestSeconds, 
                e.Sets.Select(s => new SetDto { Reps = s.Reps, Weight = s.Weight }).ToList() as IReadOnlyList<SetDto>
            ));
        }

        if (shouldUpdateExercises && exercisesSrc != null)
        {
            
            existing.Exercises.Clear();

            foreach (var exData in exercisesSrc)
            {
                var newExercise = new ScheduledExercise
                {
                    Id = Guid.NewGuid(),
                    Name = exData.name,
                    RestSeconds = exData.rest
                };

                int i = 1;
                foreach (var setData in exData.sets)
                {
                    newExercise.Sets.Add(new ScheduledSet
                    {
                        Id = Guid.NewGuid(),
                        SetNumber = i++,
                        Reps = setData.Reps,
                        Weight = setData.Weight
                    });
                }
                existing.Exercises.Add(newExercise);
            }
        }

        await _db.SaveChangesAsync(ct);
        return Map(existing);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var userId = UserId;
        var sw = await _db.ScheduledWorkouts
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (sw is null) return false;

        _db.Remove(sw);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ScheduledDto?> DuplicateAsync(Guid id, CancellationToken ct = default)
    {
        var userId = UserId;

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
            UserId = userId,
            IsVisibleToFriends = s.IsVisibleToFriends
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

    private static ScheduledStatus ParseStatus(string? s)
        => string.IsNullOrWhiteSpace(s) ? ScheduledStatus.Planned
            : s.Trim().ToLowerInvariant() == "completed" ? ScheduledStatus.Completed : ScheduledStatus.Planned;

    private static ScheduledDto Map(ScheduledWorkout s) => new()
    {
        Id = s.Id,
        Date = s.Date,
        Time = s.Time,
        PlanId = s.PlanId,
        PlanName = s.PlanName,
        Notes = s.Notes,
        Status = s.Status == ScheduledStatus.Completed ? "completed" : "planned",
        VisibleToFriends = s.IsVisibleToFriends,
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