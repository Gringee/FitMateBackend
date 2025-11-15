using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class SharedPlanConfiguration : IEntityTypeConfiguration<SharedPlan>
{
    public void Configure(EntityTypeBuilder<SharedPlan> builder)
    {
        builder.ToTable("shared_plans");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Plan)
            .WithMany()
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SharedByUser)
            .WithMany()
            .HasForeignKey(x => x.SharedByUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SharedWithUser)
            .WithMany()
            .HasForeignKey(x => x.SharedWithUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.SharedWithUserId, x.PlanId }).IsUnique();
        
        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Pending");

        builder.Property(x => x.RespondedAtUtc).IsRequired(false);
    }
}