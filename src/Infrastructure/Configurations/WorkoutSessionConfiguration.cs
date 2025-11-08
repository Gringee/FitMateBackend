using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class WorkoutSessionConfiguration : IEntityTypeConfiguration<WorkoutSession>
{
    public void Configure(EntityTypeBuilder<WorkoutSession> b)
    {
        b.ToTable("workout_sessions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasMaxLength(20).IsRequired();
        b.Property(x => x.SessionNotes).HasColumnType("text");
        b.HasMany(x => x.Exercises).WithOne().HasForeignKey(x => x.WorkoutSessionId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.ScheduledId });
        b.HasIndex(x => new { x.Status, x.StartedAtUtc });
        
        b.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SessionExerciseConfiguration : IEntityTypeConfiguration<SessionExercise>
{
    public void Configure(EntityTypeBuilder<SessionExercise> b)
    {
        b.ToTable("session_exercises");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.HasMany(x => x.Sets).WithOne().HasForeignKey(s => s.SessionExerciseId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.WorkoutSessionId, x.Order }).IsUnique();
    }
}

public class SessionSetConfiguration : IEntityTypeConfiguration<SessionSet>
{
    public void Configure(EntityTypeBuilder<SessionSet> b)
    {
        b.ToTable("session_sets");
        b.HasKey(x => x.Id);
        b.Property(x => x.WeightPlanned).HasColumnType("decimal(10,2)");
        b.Property(x => x.WeightDone).HasColumnType("decimal(10,2)");
        b.HasIndex(x => new { x.SessionExerciseId, x.SetNumber }).IsUnique();
    }
}