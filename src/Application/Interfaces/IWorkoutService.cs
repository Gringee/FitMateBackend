using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Intertfaces
{
    public interface IWorkoutService
    {
        Task CreateWorkoutAsync(Guid userId, CreateWorkoutDto dto);
        Task<List<WorkoutDto>> GetAllForUserAsync(Guid userId);
        Task DeleteWorkoutAsync(Guid userId, Guid workoutId);
        Task UpdateWorkoutAsync(Guid userId, Guid workoutId, CreateWorkoutDto dto);
    }
}
