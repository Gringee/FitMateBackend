using System.Globalization;
using System.Linq;
using Application.Abstractions;
using Application.DTOs;
using Application.Common.Security; 
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

        if (dto.Exercises is { Count: > 0 })
        {
            sw.Exercises = CreateExercisesFromDto(dto.Exercises, sw.Id);
        }
        else
        {
            sw.Exercises = CreateExercisesFromPlan(plan, sw.Id);
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

    public async Task<IReadOnlyList<ScheduledDto>> GetByDateAsync(DateOnly date, CancellationToken ct = default)
    {
        var userId = UserId;

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
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId, ct);  // NO INCLUDE - load without exercises

        if (existing is null) return null;

        var plan = await _db.Plans
                       .Include(p => p.Exercises).ThenInclude(e => e.Sets)
                       .FirstOrDefaultAsync(p => p.Id == dto.PlanId && p.CreatedByUserId == userId, ct)
                   ?? throw new KeyNotFoundException("Plan not found");

        bool planChanged = existing.PlanId != dto.PlanId;
        bool exercisesProvided = dto.Exercises is { Count: > 0 };

        existing.Date = dto.Date;
        existing.Time = dto.Time;
        existing.PlanId = plan.Id;

        existing.PlanName = string.IsNullOrWhiteSpace(dto.PlanName) ? plan.PlanName : dto.PlanName;
        
        existing.Notes = dto.Notes ?? plan.Notes;
        existing.Status = ParseStatus(dto.Status);
        existing.IsVisibleToFriends = dto.VisibleToFriends;

        if (exercisesProvided || planChanged)
        {
            // Delete old exercises via direct query (without loading them into memory)
            var oldExercises = await _db.ScheduledExercises
                .Where(e => e.ScheduledWorkoutId == id)
                .ToListAsync(ct);
            
            _db.ScheduledExercises.RemoveRange(oldExercises);
            
            // Add new exercises
            List<ScheduledExercise> newExercises;
            if (exercisesProvided)
            {
                newExercises = CreateExercisesFromDto(dto.Exercises!, existing.Id);
            }
            else
            {
                newExercises = CreateExercisesFromPlan(plan, existing.Id);
            }
            
            await _db.ScheduledExercises.AddRangeAsync(newExercises, ct);
        }

        await _db.SaveChangesAsync(ct);
        
        // Reload with exercises for return
        var updated = await _db.ScheduledWorkouts
            .Include(s => s.Exercises).ThenInclude(e => e.Sets)
            .FirstAsync(s => s.Id == existing.Id, ct);
            
        return Map(updated);
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
            
            var sortedSets = ex.Sets.OrderBy(x => x.SetNumber).ToList();
            foreach (var set in sortedSets)
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
    {
        if (string.IsNullOrWhiteSpace(s)) return ScheduledStatus.Planned;
        
        return Enum.TryParse<ScheduledStatus>(s, true, out var status) 
            ? status 
            : ScheduledStatus.Planned;
    }

    private static List<ScheduledExercise> CreateExercisesFromPlan(Plan plan, Guid scheduledWorkoutId)
    {
        var list = new List<ScheduledExercise>();

        foreach (var planEx in plan.Exercises)
        {
            var se = new ScheduledExercise
            {
                Id = Guid.NewGuid(),
                ScheduledWorkoutId = scheduledWorkoutId,
                Name = planEx.Name,
                RestSeconds = planEx.RestSeconds
            };

            var sortedSets = planEx.Sets.OrderBy(s => s.SetNumber).ToList();
            foreach (var planSet in sortedSets)
            {
                se.Sets.Add(new ScheduledSet
                {
                    Id = Guid.NewGuid(),
                    ScheduledExerciseId = se.Id,
                    SetNumber = planSet.SetNumber,
                    Reps = planSet.Reps,
                    Weight = planSet.Weight
                });
            }
            list.Add(se);
        }
        return list;
    }

    private static List<ScheduledExercise> CreateExercisesFromDto(IEnumerable<ExerciseDto> dtos, Guid scheduledWorkoutId)
    {
        var list = new List<ScheduledExercise>();
        foreach (var dto in dtos)
        {
            var se = new ScheduledExercise
            {
                Id = Guid.NewGuid(),
                ScheduledWorkoutId = scheduledWorkoutId,
                Name = dto.Name,
                RestSeconds = dto.Rest
            };

            int i = 1;
            foreach (var setDto in dto.Sets)
            {
                se.Sets.Add(new ScheduledSet
                {
                    Id = Guid.NewGuid(),
                    ScheduledExerciseId = se.Id,
                    SetNumber = i++,
                    Reps = setDto.Reps,
                    Weight = setDto.Weight
                });
            }
            list.Add(se);
        }
        return list;
    }

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