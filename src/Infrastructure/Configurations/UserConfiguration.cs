using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);

        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(u => u.UserName)
            .IsRequired()          
            .HasMaxLength(100);    
        builder.HasIndex(u => u.UserName)
            .IsUnique();           

        builder.Property(u => u.PasswordHash)
            .IsRequired();
    }
}