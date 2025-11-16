using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class PlanExerciseConfiguration : IEntityTypeConfiguration<PlanExercise>
{
    public void Configure(EntityTypeBuilder<PlanExercise> builder)
    {
        builder.ToTable("plan_exercises");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.RestSeconds).HasDefaultValue(90);

        builder.HasMany(e => e.Sets)
               .WithOne(s => s.PlanExercise)
               .HasForeignKey(s => s.PlanExerciseId)
               .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(e => e.Plan)
            .WithMany(p => p.Exercises)
            .HasForeignKey(e => e.PlanId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(e => e.PlanId);
    }
}
