// src/Application/Services/WorkoutService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
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
            // 1️⃣  Walidacja – czy wszystkie ExerciseId istnieją?
            var ids = dto.Exercises.Select(e => e.ExerciseId).ToList();
            var existing = await _exerciseRepo.GetExistingExerciseIdsAsync(ids);
            if (existing.Count != ids.Count)
                throw new ArgumentException("One or more exercises do not exist");

            // 2️⃣  Mapster: DTO ➜ encja
            var workout = dto.Adapt<Workout>();
            workout.WorkoutId = Guid.NewGuid();
            workout.UserId = userId;

            // 👉 generujemy Guid dla każdego setu
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

            // proste pola
            dto.Adapt(workout);      // WorkoutDate, DurationMinutes, Notes

            // sety: podmieniamy listę w całości
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
    }
}
