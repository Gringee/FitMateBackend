using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class ScheduledExerciseConfiguration : IEntityTypeConfiguration<ScheduledExercise>
{
    public void Configure(EntityTypeBuilder<ScheduledExercise> builder)
    {
        builder.ToTable("scheduled_exercises");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.RestSeconds).HasDefaultValue(90);

        builder.HasMany(e => e.Sets)
               .WithOne(s => s.ScheduledExercise)
               .HasForeignKey(s => s.ScheduledExerciseId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.ScheduledWorkoutId);
    }
}
