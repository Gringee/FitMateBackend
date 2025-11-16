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

        builder.Property(u => u.FullName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasMaxLength(200)
            .IsRequired()
            .HasColumnType("citext");                 

        builder.Property(u => u.UserName)
            .HasMaxLength(50)
            .IsRequired()
            .HasColumnType("citext");                 

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(255)                        
            .IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.UserName).IsUnique();
        
        builder.HasCheckConstraint("CK_users_email_format",
            "\"Email\" ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}$'");
    }
}