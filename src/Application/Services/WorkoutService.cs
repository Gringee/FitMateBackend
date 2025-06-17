using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Mapster;

namespace Application.Services
{
    public class WorkoutService : IWorkoutService
    {
        private readonly IWorkoutRepository _repo;
        private readonly IExerciseRepository _exerciseRepo;

        public WorkoutService(IWorkoutRepository repo, IExerciseRepository exerciseRepo)
        {
            _repo = repo;
            _exerciseRepo = exerciseRepo;
        }

        #region Create
        public async Task CreateWorkoutAsync(Guid userId, CreateWorkoutDto dto)
        {
            var ids = dto.Exercises.Select(e => e.ExerciseId).ToList();
            var existing = await _exerciseRepo.GetExistingExerciseIdsAsync(ids);
            if (existing.Count != ids.Count)
                throw new ArgumentException("One or more exercises do not exist");

            var workout = dto.Adapt<Workout>();
            workout.WorkoutId = Guid.NewGuid();
            workout.UserId = userId;

            foreach (var set in workout.Exercises)
                set.WorkoutExerciseId = Guid.NewGuid();

            await _repo.AddWorkoutAsync(workout);
        }
        #endregion

        #region Read
        public async Task<List<WorkoutDto>> GetAllForUserAsync(Guid userId)
        {
            var workouts = await _repo.GetAllByUserAsync(userId);
            return workouts.Adapt<List<WorkoutDto>>();
        }
        #endregion

        #region Update
        public async Task UpdateWorkoutAsync(Guid userId, Guid workoutId, CreateWorkoutDto dto)
        {
            var workout = await _repo.GetByIdAsync(workoutId);
            if (workout is null || workout.UserId != userId)
                throw new KeyNotFoundException("Workout not found");

            dto.Adapt(workout);

            workout.Exercises = dto.Exercises.Adapt<List<WorkoutExercise>>();
            foreach (var set in workout.Exercises)
                set.WorkoutExerciseId = Guid.NewGuid();

            await _repo.UpdateAsync(workout);
        }
        #endregion

        #region Delete
        public Task DeleteWorkoutAsync(Guid userId, Guid workoutId) =>
            _repo.DeleteAsync(workoutId, userId);
        #endregion

        #region Duplicate
        public async Task<WorkoutDto?> DuplicateAsync(Guid userId, Guid workoutId, DuplicateWorkoutDto dto)
        {
            var src = await _repo.GetByIdAsync(workoutId);
            if (src is null || src.UserId != userId) return null;

            var copy = new Workout
            {
                WorkoutId = Guid.NewGuid(),
                UserId = userId,
                WorkoutDate = dto.WorkoutDate ?? DateTime.UtcNow.Date,
                DurationMinutes = src.DurationMinutes,
                Notes = dto.Notes ?? src.Notes,
                Exercises = src.Exercises
                    .Select(e => new WorkoutExercise
                    {
                        WorkoutExerciseId = Guid.NewGuid(),
                        ExerciseId = e.ExerciseId,
                        SetNumber = e.SetNumber,
                        Repetitions = e.Repetitions,
                        Weight = e.Weight,
                        DurationSeconds = e.DurationSeconds,
                        Notes = e.Notes
                    })
                    .ToList()
            };

            await _repo.AddWorkoutAsync(copy);
            return copy.Adapt<WorkoutDto>();
        }
        #endregion

        public async Task<bool> SetStatusAsync(Guid userId, Guid workoutId, WorkoutStatus status)
        {
            var workout = await _repo.GetByIdAsync(workoutId);
            if (workout is null || workout.UserId != userId)
                return false;

            if (workout.Status == status)
                return true;

            workout.Status = status;
            await _repo.UpdateAsync(workout);

            return true;
        }

        public async Task<FePlanDto?> GetPlanFrontendAsync(Guid id)
        {
            var w = await _repo.GetByIdAsync(id);
            if (w is null) return null;

            return new FePlanDto
            {
                Id = (int)(w.WorkoutId.GetHashCode() & int.MaxValue),
                Name = $"Workout {w.WorkoutDate:d}",
                Type = "strength",
                Description = w.Notes ?? string.Empty,
                Exercises = w.Exercises
                    .OrderBy(e => e.SetNumber)
                    .GroupBy(e => e.ExerciseId)
                    .Select(g => new FeExerciseDto
                    {
                        Name = g.First().Exercise.Name,
                        Rest = g.First().RestSeconds,
                        Sets = g.Select(x => new FeSetDetailsDto
                        {
                            Reps = x.Repetitions,
                            Weight = x.Weight
                        }).ToList()
                    }).ToList()
            };
        }

        #region Frontend Scheduling
        public async Task<FeScheduledWorkoutDto?> SaveScheduledFrontendAsync(
            FeScheduledWorkoutDto dto,
            Guid userId)
        {
            // Parse date and time, assume local then convert to UTC
            var timePart = string.IsNullOrWhiteSpace(dto.Time) ? "00:00" : dto.Time;
            var dateTimeString = $"{dto.Date} {timePart}"; // e.g. "2025-06-04 13:20"
            var local = DateTime.ParseExact(
                dateTimeString,
                "yyyy-MM-dd HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal);
            var workoutDateUtc = local.ToUniversalTime();

            var workout = new Workout
            {
                WorkoutId = Guid.NewGuid(),
                UserId = userId,
                WorkoutDate = workoutDateUtc,
                Status = dto.Status,
                Notes = dto.PlanName,
                DurationMinutes = dto.Exercises.Sum(e => e.Sets.Count * 2)
            };

            // Process exercises sequentially to avoid DbContext concurrency issues
            var resolved = new List<(Guid ExerciseId, FeExerciseDto Ex)>();
            foreach (var ex in dto.Exercises)
            {
                // Try to get existing exercise
                var exerciseId = await _exerciseRepo.GetIdByNameAsync(ex.Name);
                if (exerciseId == Guid.Empty)
                {
                    // Create new exercise if not found
                    var newEx = new Exercise
                    {
                        ExerciseId = Guid.NewGuid(),
                        Name = ex.Name,
                        DifficultyLevel = DifficultyLevel.Beginner,
                    };
                    await _exerciseRepo.AddAsync(newEx);
                    exerciseId = newEx.ExerciseId;
                }
                resolved.Add((exerciseId, ex));
            }

            // Build WorkoutExercise entities
            var workoutExercises = resolved.SelectMany(tuple =>
            {
                var (exerciseId, ex) = tuple;
                return ex.Sets.Select((set, idx) => new WorkoutExercise
                {
                    WorkoutExerciseId = Guid.NewGuid(),
                    ExerciseId = exerciseId,
                    SetNumber = idx + 1,
                    Repetitions = set.Reps,
                    Weight = set.Weight,
                    RestSeconds = ex.Rest,
                    DurationSeconds = 0
                });
            }).ToList();

            workout.Exercises = workoutExercises;
            await _repo.AddWorkoutAsync(workout);

            dto.Id = workout.WorkoutId;
            return dto;
        }
        #endregion
    }
}
