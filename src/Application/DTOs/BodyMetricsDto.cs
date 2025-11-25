namespace Application.DTOs.BodyMetrics;

/// <summary>
/// Represents a body measurement record.
/// </summary>
/// <param name="Id">Unique identifier of the measurement.</param>
/// <param name="MeasuredAtUtc">Date and time when the measurement was taken.</param>
/// <param name="WeightKg">Body weight in kilograms.</param>
/// <param name="HeightCm">Height in centimeters.</param>
/// <param name="BMI">Calculated Body Mass Index.</param>
/// <param name="BodyFatPercentage">Optional body fat percentage.</param>
/// <param name="ChestCm">Optional chest circumference in centimeters.</param>
/// <param name="WaistCm">Optional waist circumference in centimeters.</param>
/// <param name="HipsCm">Optional hips circumference in centimeters.</param>
/// <param name="BicepsCm">Optional biceps circumference in centimeters.</param>
/// <param name="ThighsCm">Optional thighs circumference in centimeters.</param>
/// <param name="Notes">Optional notes about the measurement.</param>
public record BodyMeasurementDto(
    Guid Id,
    DateTime MeasuredAtUtc,
    decimal WeightKg,
    int HeightCm,
    decimal BMI,
    decimal? BodyFatPercentage,
    int? ChestCm,
    int? WaistCm,
    int? HipsCm,
    int? BicepsCm,
    int? ThighsCm,
    string? Notes
);

/// <summary>
/// Request to create a new body measurement.
/// </summary>
/// <param name="WeightKg">Body weight in kilograms.</param>
/// <param name="HeightCm">Height in centimeters.</param>
/// <param name="BodyFatPercentage">Optional body fat percentage.</param>
/// <param name="ChestCm">Optional chest circumference in centimeters.</param>
/// <param name="WaistCm">Optional waist circumference in centimeters.</param>
/// <param name="HipsCm">Optional hips circumference in centimeters.</param>
/// <param name="BicepsCm">Optional biceps circumference in centimeters.</param>
/// <param name="ThighsCm">Optional thighs circumference in centimeters.</param>
/// <param name="Notes">Optional notes about the measurement.</param>
public record CreateBodyMeasurementDto(
    decimal WeightKg,
    int HeightCm,
    decimal? BodyFatPercentage,
    int? ChestCm,
    int? WaistCm,
    int? HipsCm,
    int? BicepsCm,
    int? ThighsCm,
    string? Notes
);

/// <summary>
/// Statistics summary of body metrics.
/// </summary>
/// <param name="CurrentWeightKg">Current weight in kilograms.</param>
/// <param name="CurrentBMI">Current Body Mass Index.</param>
/// <param name="BMICategory">BMI category (Underweight, Normal, Overweight, Obese).</param>
/// <param name="WeightChangeLast30Days">Weight change in the last 30 days.</param>
/// <param name="LowestWeight">Lowest recorded weight.</param>
/// <param name="HighestWeight">Highest recorded weight.</param>
/// <param name="TotalMeasurements">Total number of measurements recorded.</param>
public record BodyMetricsStatsDto(
    decimal? CurrentWeightKg,
    decimal? CurrentBMI,
    string? BMICategory,  // "Underweight", "Normal", "Overweight", "Obese"
    decimal? WeightChangeLast30Days,
    decimal? LowestWeight,
    decimal? HighestWeight,
    int TotalMeasurements
);

/// <summary>
/// Represents a data point for body metrics progress over time.
/// </summary>
/// <param name="Date">Date of the measurement.</param>
/// <param name="WeightKg">Weight in kilograms.</param>
/// <param name="BMI">Body Mass Index.</param>
public record BodyMetricsProgressDto(
    DateTime Date,
    decimal WeightKg,
    decimal BMI
);