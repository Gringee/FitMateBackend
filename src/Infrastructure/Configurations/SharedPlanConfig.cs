using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class SharedPlanConfig : IEntityTypeConfiguration<SharedPlan>
{
    public void Configure(EntityTypeBuilder<SharedPlan> b)
    {
        b.ToTable("shared_plans");
        b.HasKey(x => x.Id);

        b.HasOne(x => x.Plan)
            .WithMany()
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.SharedByUser)
            .WithMany()
            .HasForeignKey(x => x.SharedByUserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.SharedWithUser)
            .WithMany()
            .HasForeignKey(x => x.SharedWithUserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.SharedWithUserId, x.PlanId }).IsUnique();
        
        b.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Pending");

        b.Property(x => x.RespondedAtUtc).IsRequired(false);
    }
}