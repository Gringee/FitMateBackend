using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
{
    public void Configure(EntityTypeBuilder<Friendship> builder)
    {
        builder.ToTable("friendships");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        
        builder.HasIndex(x => new { x.UserAId, x.UserBId }).IsUnique();

        builder.HasOne(x => x.UserA).WithMany().HasForeignKey(x => x.UserAId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.UserB).WithMany().HasForeignKey(x => x.UserBId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.RequestedByUser).WithMany().HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.Cascade);
    }
}