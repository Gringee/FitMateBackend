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
        
        public DbSet<WorkoutSession> WorkoutSessions { get; set; }
        public DbSet<SessionExercise> SessionExercises { get; set; }
        public DbSet<SessionSet> SessionSets { get; set; }
        
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; } 

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            base.OnModelCreating(modelBuilder);
        }
    }
}
