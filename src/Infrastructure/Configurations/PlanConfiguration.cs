using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("plans");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.PlanName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Notes).HasColumnType("text");

        builder.HasMany(p => p.Exercises)
               .WithOne(e => e.Plan)
               .HasForeignKey(e => e.PlanId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
