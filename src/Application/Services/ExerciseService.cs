using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Mapster;

namespace Application.Services
{
    /// <summary>
    /// CRUD + query logic for <see cref="Exercise"/> entities.
    /// Implementacja oparta na Mapster eliminuje ręczne mapowanie pól.
    /// </summary>
    public class ExerciseService : IExerciseService
    {
        private readonly IExerciseRepository _repo;

        public ExerciseService(IExerciseRepository repo) => _repo = repo;

        public async Task<ExerciseDto> CreateAsync(CreateExerciseDto dto)
        {
            // DTO ➜ encja domenowa
            var entity = dto.Adapt<Exercise>();
            entity.ExerciseId = Guid.NewGuid();

            var created = await _repo.AddAsync(entity);
            return created.Adapt<ExerciseDto>(); // encja ➜ DTO
        }

        public async Task<List<ExerciseDto>> GetAllAsync() =>
            (await _repo.GetAllAsync()).Adapt<List<ExerciseDto>>();

        public async Task<ExerciseDto?> GetByIdAsync(Guid id) =>
            (await _repo.GetByIdAsync(id))?.Adapt<ExerciseDto>();

        public async Task<bool> UpdateAsync(Guid id, UpdateExerciseDto dto)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity is null) return false;

            dto.Adapt(entity);              // nadpisuje tylko zmienione pola
            await _repo.UpdateAsync(entity);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity is null) return false;

            await _repo.DeleteAsync(id);
            return true;
        }

        public async Task<List<ExerciseDto>> SearchAsync(string query)
        {
            return (await _repo.SearchAsync(query)).Adapt<List<ExerciseDto>>();
        }
    }
}
