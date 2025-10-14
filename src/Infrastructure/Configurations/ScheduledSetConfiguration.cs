using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class ScheduledSetConfiguration : IEntityTypeConfiguration<ScheduledSet>
{
    public void Configure(EntityTypeBuilder<ScheduledSet> builder)
    {
        builder.ToTable("scheduled_sets");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.SetNumber).IsRequired();
        builder.Property(s => s.Weight).HasPrecision(10, 2);

        builder.HasIndex(s => new { s.ScheduledExerciseId, s.SetNumber }).IsUnique();
    }
}
