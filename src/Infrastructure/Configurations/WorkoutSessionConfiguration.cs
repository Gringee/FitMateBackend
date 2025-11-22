using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class WorkoutSessionConfiguration : IEntityTypeConfiguration<WorkoutSession>
{
    public void Configure(EntityTypeBuilder<WorkoutSession> builder)
    {
        builder.ToTable("workout_sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status)
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired();
        builder.Property(x => x.SessionNotes).HasColumnType("text");
        builder.HasMany(x => x.Exercises).WithOne().HasForeignKey(x => x.WorkoutSessionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.ScheduledId });
        builder.HasIndex(x => new { x.Status, x.StartedAtUtc });
        builder.HasIndex(x => new { x.UserId, x.StartedAtUtc });
        
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SessionExerciseConfiguration : IEntityTypeConfiguration<SessionExercise>
{
    public void Configure(EntityTypeBuilder<SessionExercise> builder)
    {
        builder.ToTable("session_exercises");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        
        builder.Property(x => x.ScheduledExerciseId).IsRequired(false);

        builder.HasMany(x => x.Sets).WithOne().HasForeignKey(s => s.SessionExerciseId).OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(x => new { x.WorkoutSessionId, x.Order }); 
    }
}

public class SessionSetConfiguration : IEntityTypeConfiguration<SessionSet>
{
    public void Configure(EntityTypeBuilder<SessionSet> builder)
    {
        builder.ToTable("session_sets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.WeightPlanned).HasColumnType("decimal(10,2)");
        builder.Property(x => x.WeightDone).HasColumnType("decimal(10,2)");
        builder.HasIndex(x => new { x.SessionExerciseId, x.SetNumber }).IsUnique();
    }
}