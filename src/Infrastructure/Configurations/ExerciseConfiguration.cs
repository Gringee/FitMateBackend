using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

internal sealed class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.ToTable("exercises");

        builder.HasKey(e => e.ExerciseId);

        builder.Property(e => e.Name)
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(e => e.Description)
               .HasColumnType("text");

        builder.Property(e => e.DifficultyLevel)
               .HasConversion<string>()            
               .IsRequired();

        builder.HasOne(e => e.BodyPart)
               .WithMany(bp => bp.Exercises)
               .HasForeignKey(e => e.BodyPartId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Category)
               .WithMany(c => c.Exercises)
               .HasForeignKey(e => e.CategoryId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.Name)                    
         .HasDatabaseName("ix_exercises_name_lower")
         .IsUnique();
    }
}
