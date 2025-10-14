using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class ScheduledWorkoutConfiguration : IEntityTypeConfiguration<ScheduledWorkout>
{
    public void Configure(EntityTypeBuilder<ScheduledWorkout> builder)
    {
        builder.ToTable("scheduled_workouts");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Date).HasColumnType("date");
        builder.Property(s => s.Time).HasColumnType("time");
        builder.Property(s => s.PlanName).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Notes).HasColumnType("text");

        builder.HasOne(s => s.Plan)
               .WithMany()
               .HasForeignKey(s => s.PlanId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Exercises)
               .WithOne(e => e.ScheduledWorkout)
               .HasForeignKey(e => e.ScheduledWorkoutId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.Date);
    }
}
