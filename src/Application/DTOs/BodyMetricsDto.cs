namespace Application.DTOs.BodyMetrics;

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

public record BodyMetricsStatsDto(
    decimal? CurrentWeightKg,
    decimal? CurrentBMI,
    string? BMICategory,  // "Underweight", "Normal", "Overweight", "Obese"
    decimal? WeightChangeLast30Days,
    decimal? LowestWeight,
    decimal? HighestWeight,
    int TotalMeasurements
);

public record BodyMetricsProgressDto(
    DateTime Date,
    decimal WeightKg,
    decimal BMI
);