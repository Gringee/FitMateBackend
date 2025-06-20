﻿using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ExerciseRepository : IExerciseRepository
    {
        private readonly AppDbContext _context;

        public ExerciseRepository(AppDbContext context) => _context = context;

        public async Task<List<Guid>> GetExistingExerciseIdsAsync(IEnumerable<Guid> exerciseIds)
        {
            return await _context.Exercises
                .Where(e => exerciseIds.Contains(e.ExerciseId))
                .Select(e => e.ExerciseId)
                .ToListAsync();
        }

        public async Task<Exercise> AddAsync(Exercise exercise)
        {
            await _context.Exercises.AddAsync(exercise);
            await _context.SaveChangesAsync();
            return exercise;
        }

        public async Task<Exercise?> GetByIdAsync(Guid exerciseId)
        {
            return await _context.Exercises
                .FirstOrDefaultAsync(e => e.ExerciseId == exerciseId);
        }

        public async Task<List<Exercise>> GetAllAsync()
        {
            return await _context.Exercises
                .OrderBy(e => e.Name)
                .ToListAsync();
        }

        public async Task UpdateAsync(Exercise exercise)
        {
            _context.Exercises.Update(exercise);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid exerciseId)
        {
            var existing = await _context.Exercises
                .FirstOrDefaultAsync(e => e.ExerciseId == exerciseId);

            if (existing != null)
            {
                _context.Exercises.Remove(existing);
                await _context.SaveChangesAsync();
            }
        }

        public Task<List<Exercise>> SearchAsync(string query)
        {
            var q = $"%{query.ToLower()}%";

            return _context.Exercises.AsNoTracking()
                .Where(e =>
                    EF.Functions.ILike(e.Name, q) ||
                    EF.Functions.ILike(e.Description ?? string.Empty, q))
                .OrderBy(e => e.Name)
                .ToListAsync();
        }

        public async Task<Guid> GetIdByNameAsync(string name)
        {
            var id = await _context.Exercises
                        .Where(e => e.Name.ToLower() == name.ToLower())
                        .Select(e => e.ExerciseId)
                        .FirstOrDefaultAsync();

            return id == Guid.Empty ? Guid.Empty : id;
        }
    }
}
