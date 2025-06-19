using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class WorkoutRepository : IWorkoutRepository
    {
        private readonly AppDbContext _context;
        public WorkoutRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddWorkoutAsync(Workout workout)
        {
            await _context.Workouts.AddAsync(workout);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Workout>> GetAllByUserAsync(Guid userId)
        {
            return await _context.Workouts
                .Include(w => w.Exercises)
                .ThenInclude(e => e.Exercise)
                .Where(w => w.UserId == userId)
                .ToListAsync();
        }

        public async Task DeleteAsync(Guid workoutId, Guid userId)
        {
            var workout = await _context.Workouts
                .FirstOrDefaultAsync(w => w.WorkoutId == workoutId && w.UserId == userId);

            if (workout == null) throw new Exception("Workout not found");

            _context.Workouts.Remove(workout);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Workout workout)
        {
            _context.Workouts.Update(workout);
            await _context.SaveChangesAsync();
        }

        public async Task<Workout?> GetByIdAsync(Guid workoutId)
        {
            return await _context.Workouts
                .Include(w => w.Exercises)
                .ThenInclude(we => we.Exercise)
                .FirstOrDefaultAsync(w => w.WorkoutId == workoutId);
        }

        public Task<List<Workout>> GetByUserAndRangeAsync(Guid userId, DateTime startUtc, DateTime endUtc) =>
            _context.Workouts
            .Include(w => w.Exercises)
            .ThenInclude(we => we.Exercise)
            .Where(w => w.UserId == userId &&
                    w.WorkoutDate >= startUtc &&
                    w.WorkoutDate <= endUtc)
            .ToListAsync();

        public Task<List<(Guid Id, DateTime Date)>> GetIdsAndDatesAsync(Guid userId) =>
            _context.Workouts
            .Where(w => w.UserId == userId)
            .Select(w => new ValueTuple<Guid, DateTime>(w.WorkoutId, w.WorkoutDate))
            .ToListAsync();
    }
}
