using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Infrastructure.Persistence
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Plan> Plans => Set<Plan>();
        public DbSet<PlanExercise> PlanExercises => Set<PlanExercise>();
        public DbSet<PlanSet> PlanSets => Set<PlanSet>();

        public DbSet<ScheduledWorkout> ScheduledWorkouts => Set<ScheduledWorkout>();
        public DbSet<ScheduledExercise> ScheduledExercises => Set<ScheduledExercise>();
        public DbSet<ScheduledSet> ScheduledSets => Set<ScheduledSet>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Załadowanie wszystkich konfiguracji z Infrastructure.Configurations
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            base.OnModelCreating(modelBuilder);
        }
    }
}
