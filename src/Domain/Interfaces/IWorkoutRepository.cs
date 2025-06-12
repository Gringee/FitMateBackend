using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces;

public interface IWorkoutRepository
{
    Task AddWorkoutAsync(Workout workout);
    Task<List<Workout>> GetAllByUserAsync(Guid userId);
    Task<Workout?> GetByIdAsync(Guid workoutId);
    Task UpdateAsync(Workout workout);
    Task DeleteAsync(Guid workoutId, Guid userId);
}
