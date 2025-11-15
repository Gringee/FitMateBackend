using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

namespace Infrastructure.Configurations;

public class PlanAccessConfiguration : IEntityTypeConfiguration<PlanAccess>
{
    public void Configure(EntityTypeBuilder<PlanAccess> builder)
    {
        builder.HasKey(x => new { x.UserId, x.PlanId });
        builder.Property(x => x.Permission).HasMaxLength(16).IsRequired();

        builder.HasOne(x => x.Plan).WithMany(p => p.Access).HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}