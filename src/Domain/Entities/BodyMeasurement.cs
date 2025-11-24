using Domain.Entities;

namespace Domain.Entities;

public class BodyMeasurement
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime MeasuredAtUtc { get; set; }
    
    // Core metrics
    public decimal WeightKg { get; set; }
    public int HeightCm { get; set; }
    public decimal BMI { get; set; }  // Denormalized
    
    // Optional extended metrics
    public decimal? BodyFatPercentage { get; set; }
    public int? ChestCm { get; set; }
    public int? WaistCm { get; set; }
    public int? HipsCm { get; set; }
    public int? BicepsCm { get; set; }
    public int? ThighsCm { get; set; }
    
    public string? Notes { get; set; }
    
    // Navigation
    public User User { get; set; } = null!;
}
