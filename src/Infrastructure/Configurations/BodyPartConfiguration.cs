using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

internal sealed class BodyPartConfiguration : IEntityTypeConfiguration<BodyPart>
{
    public void Configure(EntityTypeBuilder<BodyPart> builder)
    {
        builder.ToTable("body_parts");
        builder.HasKey(bp => bp.BodyPartId);
        builder.Property(bp => bp.Name).HasMaxLength(50).IsRequired();
        builder.Property(bp => bp.Description).HasColumnType("text");
        builder.HasIndex(bp => bp.Name).IsUnique();
    }
}
