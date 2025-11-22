using System.ComponentModel.DataAnnotations;
using Application.Common;

namespace Application.DTOs.Analytics;

/// <summary>
/// Represents an overview of workout statistics.
/// </summary>
public class OverviewDto
{
    public decimal TotalVolume { get; set; }
    public double AvgIntensity { get; set; }
    public int SessionsCount { get; set; }
    public double AdherencePct { get; set; }
    public int NewPrs { get; set; }
}

/// <summary>
/// Represents a data point for volume over time.
/// </summary>
public class TimePointDto
{
    public string Period { get; set; } = default!;
    public decimal Value { get; set; }
    public string? ExerciseName { get; set; }
}

/// <summary>
/// Represents a data point for estimated 1-rep max over time.
/// </summary>
public class E1rmPointDto
{
    public DateOnly Day { get; set; }
    public decimal E1Rm { get; set; }
    public Guid? SessionId { get; set; }
}

/// <summary>
/// Represents adherence statistics.
/// </summary>
public class AdherenceDto
{
    public int Planned { get; set; }
    public int Completed { get; set; }
    public int Missed => Planned - Completed < 0 ? 0 : Planned - Completed;
    public double AdherencePct => Planned == 0 ? 0 : Math.Round((double)Completed / Planned * 100, 1);
}

/// <summary>
/// Represents a comparison between planned and actual performance for an exercise.
/// </summary>
public class PlanVsActualItemDto
{
    public string ExerciseName { get; set; } = default!;
    public int SetNumber { get; set; }
    public int RepsPlanned { get; set; }
    public decimal WeightPlanned { get; set; }
    public int? RepsDone { get; set; }
    public decimal? WeightDone { get; set; }
    public decimal? Rpe { get; set; }
    public bool? IsFailure { get; set; }
    public bool IsExtra { get; set; }
    public int RepsDiff => (RepsDone ?? 0) - RepsPlanned;
    public decimal WeightDiff => (WeightDone ?? 0) - WeightPlanned;
}

/// <summary>
/// Query parameters for retrieving overview statistics.
/// </summary>
public sealed class OverviewQueryDto : IValidatableObject
{
    [Required] public DateTime From { get; set; }
    [Required] public DateTime To { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (To <= From) yield return new ValidationResult("'To' must be > 'From'.", new[] { nameof(To) });
    }

    public (DateTime FromUtc, DateTime ToUtc) ToUtcRange() => DateHelpers.NormalizeRange(From, To);
}

/// <summary>
/// Query parameters for retrieving volume statistics.
/// </summary>
public sealed class VolumeQueryDto : IValidatableObject
{
    [Required] public DateTime From { get; set; }
    [Required] public DateTime To { get; set; }
    
    [RegularExpression("(?i)^(day|week|exercise)$", ErrorMessage = "GroupBy: 'day', 'week', 'exercise'.")]
    public string GroupBy { get; set; } = "day";
    
    [StringLength(200)]
    public string? ExerciseName { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (To <= From) yield return new ValidationResult("'To' must be > 'From'.", new[] { nameof(To) });
    }

    public (DateTime FromUtc, DateTime ToUtc) ToUtcRange() => DateHelpers.NormalizeRange(From, To);
    public string GroupByNormalized => GroupBy.Trim().ToLowerInvariant();
}

/// <summary>
/// Query parameters for retrieving estimated 1-rep max statistics.
/// </summary>
public sealed class E1rmQueryDto : IValidatableObject
{
    [Required] public DateTime From { get; set; }
    [Required] public DateTime To { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (To <= From) yield return new ValidationResult("'To' must be > 'From'.", new[] { nameof(To) });
    }

    public (DateTime FromUtc, DateTime ToUtc) ToUtcRange() => DateHelpers.NormalizeRange(From, To);
}

/// <summary>
/// Query parameters for retrieving adherence statistics.
/// </summary>
public sealed class AdherenceQueryDto : IValidatableObject
{
    [Required] public DateOnly FromDate { get; set; }
    [Required] public DateOnly ToDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (ToDate <= FromDate) yield return new ValidationResult("'To' must be > 'From'.", new[] { nameof(ToDate) });
    }
    
    public (DateOnly From, DateOnly To) ToRange() => (FromDate, ToDate);
}

/// <summary>
/// Query parameters for retrieving plan vs actual comparison.
/// </summary>
public sealed class PlanVsActualQueryDto
{
    [Required] public Guid SessionId { get; set; }
}
