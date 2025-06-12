using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

internal sealed class WorkoutConfiguration : IEntityTypeConfiguration<Workout>
{
    public void Configure(EntityTypeBuilder<Workout> builder)
    {
        builder.ToTable("workouts");

        builder.HasKey(w => w.WorkoutId);

        builder.Property(w => w.WorkoutDate).IsRequired();
        builder.Property(w => w.DurationMinutes).IsRequired();

        builder.Property(w => w.Notes)
               .HasColumnType("text");

        builder.HasIndex(w => w.UserId);      
    }
}
