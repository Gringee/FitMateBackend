using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using Application.Intertfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services
{
    public class WorkoutService : IWorkoutService
    {
        private readonly IWorkoutRepository _repo;
        private readonly IExerciseRepository _exerciseRepository;

        public WorkoutService(
            IWorkoutRepository repo,
            IExerciseRepository exerciseRepository)
        {
            _repo = repo;
            _exerciseRepository = exerciseRepository;
        }

        public async Task CreateWorkoutAsync(Guid userId, CreateWorkoutDto dto)
        {
            // Sprawdź, czy wszystkie ćwiczenia istnieją
            var exerciseIds = dto.Exercises.Select(e => e.ExerciseId).ToList();
            var existingIds = await _exerciseRepository.GetExistingExerciseIdsAsync(exerciseIds);
            if (existingIds.Count != exerciseIds.Count)
                throw new ArgumentException("One or more exercise in DB does not exist");

            // Zbuduj encję
            var workout = new Workout
            {
                WorkoutId = Guid.NewGuid(),
                UserId = userId,
                WorkoutDate = dto.WorkoutDate,
                DurationMinutes = dto.DurationMinutes,
                Notes = dto.Notes,
                Exercises = dto.Exercises
                    .Select(ex => new WorkoutExercise
                    {
                        WorkoutExerciseId = Guid.NewGuid(),
                        ExerciseId = ex.ExerciseId,
                        SetNumber = ex.SetNumber,
                        Repetitions = ex.Repetitions,
                        Weight = ex.Weight,
                        DurationSeconds = ex.DurationSeconds,
                        Notes = ex.Notes
                    })
                    .ToList()
            };

            await _repo.AddWorkoutAsync(workout);
        }

        public async Task<List<WorkoutDto>> GetAllForUserAsync(Guid userId)
        {
            // Pobierz encje
            var workouts = await _repo.GetAllByUserAsync(userId);

            // Zamapuj na DTO
            return workouts
                .Select(w => new WorkoutDto
                {
                    WorkoutId = w.WorkoutId,
                    UserId = w.UserId,
                    WorkoutDate = w.WorkoutDate,
                    DurationMinutes = w.DurationMinutes,
                    Notes = w.Notes,
                    Exercises = w.Exercises
                        .Select(ex => new WorkoutExerciseDto
                        {
                            ExerciseId = ex.ExerciseId,
                            SetNumber = ex.SetNumber,
                            Repetitions = ex.Repetitions,
                            Weight = ex.Weight,
                            DurationSeconds = ex.DurationSeconds,
                            Notes = ex.Notes
                        })
                        .ToList()
                })
                .ToList();
        }

        public async Task DeleteWorkoutAsync(Guid userId, Guid workoutId)
        {
            await _repo.DeleteAsync(workoutId, userId);
        }

        public async Task UpdateWorkoutAsync(Guid userId, Guid workoutId, CreateWorkoutDto dto)
        {
            // Pobierz istniejący
            var workout = await _repo.GetByIdAsync(workoutId);
            if (workout == null || workout.UserId != userId)
                throw new KeyNotFoundException("Workout not found");

            // Uaktualnij pola
            workout.WorkoutDate = dto.WorkoutDate;
            workout.DurationMinutes = dto.DurationMinutes;
            workout.Notes = dto.Notes;

            // Zamień listę ćwiczeń
            workout.Exercises.Clear();
            foreach (var ex in dto.Exercises)
            {
                workout.Exercises.Add(new WorkoutExercise
                {
                    WorkoutExerciseId = Guid.NewGuid(),
                    ExerciseId = ex.ExerciseId,
                    SetNumber = ex.SetNumber,
                    Repetitions = ex.Repetitions,
                    Weight = ex.Weight,
                    DurationSeconds = ex.DurationSeconds,
                    Notes = ex.Notes
                });
            }

            await _repo.UpdateAsync(workout);
        }
    }
}
