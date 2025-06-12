using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IExerciseService
    {
        Task<ExerciseDto> CreateAsync(CreateExerciseDto dto);
        Task<List<ExerciseDto>> GetAllAsync();
        Task<ExerciseDto?> GetByIdAsync(Guid id);
        Task<bool> UpdateAsync(Guid id, UpdateExerciseDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
