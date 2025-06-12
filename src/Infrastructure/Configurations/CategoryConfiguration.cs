using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.CategoryId);

        builder.Property(c => c.Name)
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(c => c.Description)
               .HasColumnType("text");

        builder.HasIndex(c => c.Name).IsUnique();
    }
}
