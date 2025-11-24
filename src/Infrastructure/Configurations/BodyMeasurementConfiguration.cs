using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class BodyMeasurementConfiguration : IEntityTypeConfiguration<BodyMeasurement>
{
    public void Configure(EntityTypeBuilder<BodyMeasurement> builder)
    {
        builder.ToTable("body_measurements");
        builder.HasKey(bm => bm.Id);

        builder.Property(bm => bm.WeightKg)
            .HasPrecision(5, 2)  // Max 999.99 kg
            .IsRequired();

        builder.Property(bm => bm.HeightCm)
            .IsRequired();

        builder.Property(bm => bm.BMI)
            .HasPrecision(4, 2)  // Max 99.99
            .IsRequired();

        builder.Property(bm => bm.MeasuredAtUtc)
            .IsRequired();

        // Optional metrics
        builder.Property(bm => bm.BodyFatPercentage)
            .HasPrecision(4, 2);

        builder.Property(bm => bm.Notes)
            .HasMaxLength(500);

        // Relationship
        builder.HasOne(bm => bm.User)
            .WithMany(u => u.BodyMeasurements)
            .HasForeignKey(bm => bm.UserId)
            .OnDelete(DeleteBehavior.Cascade);  // Usunięcie user = usunięcie pomiarów

        // Indexes
        builder.HasIndex(bm => bm.UserId);
        builder.HasIndex(bm => bm.MeasuredAtUtc);
        builder.HasIndex(bm => new { bm.UserId, bm.MeasuredAtUtc })
            .IsDescending(false, true);  // Latest first

        // Check constraints
        builder.HasCheckConstraint("CK_body_measurements_weight_positive",
            "\"WeightKg\" > 0 AND \"WeightKg\" < 500");
        
        builder.HasCheckConstraint("CK_body_measurements_height_valid",
            "\"HeightCm\" >= 50 AND \"HeightCm\" <= 300");

        builder.HasCheckConstraint("CK_body_measurements_bmi_valid",
            "\"BMI\" >= 10 AND \"BMI\" <= 100");
    }
}
