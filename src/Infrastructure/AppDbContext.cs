using System;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;

public class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Workout> Workouts { get; set; }
    public DbSet<Exercise> Exercises { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<User>().HasKey(u => u.UserId);
	}
}
