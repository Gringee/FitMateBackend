using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

internal sealed class WorkoutExerciseConfiguration : IEntityTypeConfiguration<WorkoutExercise>
{
    public void Configure(EntityTypeBuilder<WorkoutExercise> builder)
    {
        builder.ToTable("workout_exercises");

        builder.HasKey(we => we.WorkoutExerciseId);

        builder.Property(we => we.Repetitions).IsRequired();
        builder.Property(we => we.Weight)
               .HasPrecision(5, 2)      
               .IsRequired();

        builder.Property(we => we.DurationSeconds);
        builder.Property(we => we.Notes)
               .HasColumnType("text");

        builder.Property(x => x.RestSeconds)
         .HasDefaultValue(90)
         .IsRequired();

        builder.HasOne(we => we.Workout)
               .WithMany(w => w.Exercises)
               .HasForeignKey(we => we.WorkoutId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(we => we.Exercise)
               .WithMany(e => e.WorkoutExercises)
               .HasForeignKey(we => we.ExerciseId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(we => new { we.WorkoutId, we.ExerciseId, we.SetNumber })
       .HasDatabaseName("IX_workout_exercises_WorkoutId_ExerciseId_SetNumber")
       .IsUnique();
    }
}
