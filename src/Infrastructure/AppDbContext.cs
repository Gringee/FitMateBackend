using Microsoft.EntityFrameworkCore;
using Domain.Entities;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Workout> Workouts { get; set; } = null!;
    public DbSet<Exercise> Exercises { get; set; } = null!;
    public DbSet<WorkoutExercise> WorkoutExercises { get; set; } = null!;
    public DbSet<BodyPart> BodyParts { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
