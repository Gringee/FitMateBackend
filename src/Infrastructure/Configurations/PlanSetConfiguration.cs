using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class PlanSetConfiguration : IEntityTypeConfiguration<PlanSet>
{
    public void Configure(EntityTypeBuilder<PlanSet> builder)
    {
        builder.ToTable("plan_sets");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.SetNumber).IsRequired();
        builder.Property(s => s.Weight).HasPrecision(10, 2);

        builder.HasIndex(s => new { s.PlanExerciseId, s.SetNumber }).IsUnique();
    }
}
