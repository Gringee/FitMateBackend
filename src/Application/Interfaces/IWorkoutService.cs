using Application.DTOs;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IWorkoutService
    {
        Task CreateWorkoutAsync(Guid userId, CreateWorkoutDto dto);
        Task<List<WorkoutDto>> GetAllForUserAsync(Guid userId);
        Task DeleteWorkoutAsync(Guid userId, Guid workoutId);
        Task UpdateWorkoutAsync(Guid userId, Guid workoutId, CreateWorkoutDto dto);
        Task<bool> SetStatusAsync(Guid userId, Guid workoutId, WorkoutStatus status);
        Task<WorkoutDto?> DuplicateAsync(Guid userId, Guid workoutId, DuplicateWorkoutDto dto);
        Task<FePlanDto?> GetPlanFrontendAsync(Guid workoutId);
        Task<FeScheduledWorkoutDto?> SaveScheduledFrontendAsync(FeScheduledWorkoutDto dto, Guid userId);
        Task<List<FePlanDto>> GetPlansByDateAsync(Guid userId, DateOnly date);
        Task<List<DayWorkoutRefDto>> GetAllWorkoutDaysAsync(Guid userId);
    }
}
