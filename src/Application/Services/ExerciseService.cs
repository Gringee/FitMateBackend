// src/Application/Services/ExerciseService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Services
{
    public class ExerciseService : IExerciseService
    {
        private readonly IExerciseRepository _repo;

        public ExerciseService(IExerciseRepository repo)
        {
            _repo = repo;
        }

        public async Task<ExerciseDto> CreateAsync(CreateExerciseDto dto)
        {
            var exercise = new Exercise
            {
                ExerciseId = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                DifficultyLevel = dto.DifficultyLevel,
                BodyPartId = dto.BodyPartId,
                CategoryId = dto.CategoryId
            };

            var created = await _repo.AddAsync(exercise);

            return new ExerciseDto
            {
                ExerciseId = created.ExerciseId,
                Name = created.Name,
                Description = created.Description,
                DifficultyLevel = created.DifficultyLevel,
                BodyPartId = created.BodyPartId,
                CategoryId = created.CategoryId
            };
        }

        public async Task<List<ExerciseDto>> GetAllAsync()
        {
            var all = await _repo.GetAllAsync();
            return all
                .Select(e => new ExerciseDto
                {
                    ExerciseId = e.ExerciseId,
                    Name = e.Name,
                    Description = e.Description,
                    DifficultyLevel = e.DifficultyLevel,
                    BodyPartId = e.BodyPartId,
                    CategoryId = e.CategoryId
                })
                .ToList();
        }

        public async Task<ExerciseDto?> GetByIdAsync(Guid id)
        {
            var e = await _repo.GetByIdAsync(id);
            if (e == null) return null;

            return new ExerciseDto
            {
                ExerciseId = e.ExerciseId,
                Name = e.Name,
                Description = e.Description,
                DifficultyLevel = e.DifficultyLevel,
                BodyPartId = e.BodyPartId,
                CategoryId = e.CategoryId
            };
        }

        public async Task<bool> UpdateAsync(Guid id, UpdateExerciseDto dto)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                return false;

            existing.Name = dto.Name;
            existing.Description = dto.Description;
            existing.DifficultyLevel = dto.DifficultyLevel;
            existing.BodyPartId = dto.BodyPartId;
            existing.CategoryId = dto.CategoryId;

            await _repo.UpdateAsync(existing);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                return false;

            await _repo.DeleteAsync(id);
            return true;
        }
    }
}
