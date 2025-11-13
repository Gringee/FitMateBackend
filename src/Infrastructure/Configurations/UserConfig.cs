using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class UserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);

        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(200);
        
        b.Property(u => u.UserName)
            .IsRequired()          
            .HasMaxLength(100);    
        b.HasIndex(u => u.UserName)
            .IsUnique();           

        b.Property(x => x.PasswordHash)
            .IsRequired();
    }
}