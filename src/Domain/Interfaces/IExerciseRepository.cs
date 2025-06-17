using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Interfaces;

public interface IExerciseRepository
{
    Task<List<Guid>> GetExistingExerciseIdsAsync(IEnumerable<Guid> exerciseIds);

    Task<Exercise> AddAsync(Exercise exercise);
    Task<Exercise?> GetByIdAsync(Guid exerciseId);
    Task<List<Exercise>> GetAllAsync();
    Task UpdateAsync(Exercise exercise);
    Task DeleteAsync(Guid exerciseId);
    Task<List<Exercise>> SearchAsync(string query);
    Task<Guid> GetIdByNameAsync(string name);
}