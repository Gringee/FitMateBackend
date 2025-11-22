using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Plan> Plans { get; }
    DbSet<PlanExercise> PlanExercises { get; }
    DbSet<PlanSet> PlanSets { get; }
    DbSet<ScheduledWorkout> ScheduledWorkouts { get; }
    DbSet<ScheduledExercise> ScheduledExercises { get; }
    DbSet<ScheduledSet> ScheduledSets { get; }
    DbSet<WorkoutSession> WorkoutSessions { get; }
    DbSet<SessionExercise> SessionExercises { get; }
    DbSet<SessionSet> SessionSets { get; }
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<SharedPlan> SharedPlans { get; }
    DbSet<Friendship> Friendships { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    
    Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database { get; }
    Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker ChangeTracker { get; }
    Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
    Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Add(object entity);
    Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Remove(object entity);
}
