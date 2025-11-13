using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
{
    public void Configure(EntityTypeBuilder<Friendship> b)
    {
        b.ToTable("friendships");
        b.HasKey(x => x.Id);

        b.Property(x => x.Status).HasMaxLength(20).IsRequired();
        b.Property(x => x.CreatedAtUtc).IsRequired();
        
        b.HasIndex(x => new { x.UserAId, x.UserBId }).IsUnique();

        b.HasOne(x => x.UserA).WithMany().HasForeignKey(x => x.UserAId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.UserB).WithMany().HasForeignKey(x => x.UserBId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.RequestedByUser).WithMany().HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.Cascade);
    }
}