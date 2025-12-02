using System.ComponentModel.DataAnnotations;
using Application.Common;

namespace Application.DTOs.Analytics;

/// <summary>
/// Represents an overview of workout statistics.
/// </summary>
public class OverviewDto
{
    /// <summary>
    /// Total volume (weight × reps) across all sessions.
    /// </summary>
    public decimal TotalVolume { get; set; }
    
    /// <summary>
    /// Average weight used across all sets.
    /// </summary>
    public double AvgWeight { get; set; }
    
    /// <summary>
    /// Total number of workout sessions.
    /// </summary>
    public int SessionsCount { get; set; }
    
    /// <summary>
    /// Adherence percentage (completed workouts / planned workouts × 100).
    /// </summary>
    public double AdherencePct { get; set; }
    
    /// <summary>
    /// Number of new personal records achieved.
    /// </summary>
    public int NewPrs { get; set; }
}

/// <summary>
/// Represents a data point for volume over time.
/// </summary>
public class TimePointDto
{
    /// <summary>
    /// The time period for this data point (e.g., "2024-01-15" for day, "2024-W03" for week).
    /// </summary>
    public string Period { get; set; } = default!;
    
    /// <summary>
    /// The volume value for this period.
    /// </summary>
    public decimal Value { get; set; }
    
    /// <summary>
    /// Name of the exercise if grouped by exercise, otherwise null.
    /// </summary>
    public string? ExerciseName { get; set; }
}

/// <summary>
/// Represents a data point for estimated 1-rep max over time.
/// </summary>
public class E1rmPointDto
{
    /// <summary>
    /// The date of this E1RM measurement.
    /// </summary>
    public DateOnly Day { get; set; }
    
    /// <summary>
    /// Estimated 1-rep max value calculated using Brzycki formula.
    /// </summary>
    public decimal E1Rm { get; set; }
    
    /// <summary>
    /// ID of the workout session where this E1RM was achieved, if applicable.
    /// </summary>
    public Guid? SessionId { get; set; }
}

/// <summary>
/// Represents adherence statistics.
/// </summary>
public class AdherenceDto
{
    /// <summary>
    /// Number of planned workouts in the period.
    /// </summary>
    public int Planned { get; set; }
    
    /// <summary>
    /// Number of completed workouts in the period.
    /// </summary>
    public int Completed { get; set; }
    
    /// <summary>
    /// Number of missed workouts (Planned - Completed).
    /// </summary>
    public int Missed => Planned - Completed < 0 ? 0 : Planned - Completed;
    
    /// <summary>
    /// Adherence percentage (Completed / Planned × 100), rounded to 1 decimal place.
    /// </summary>
    public double AdherencePct => Planned == 0 ? 0 : Math.Round((double)Completed / Planned * 100, 1);
}

/// <summary>
/// Represents a comparison between planned and actual performance for an exercise.
/// </summary>
public class PlanVsActualItemDto
{
    /// <summary>
    /// Name of the exercise.
    /// </summary>
    public string ExerciseName { get; set; } = default!;
    
    /// <summary>
    /// Set number within the exercise.
    /// </summary>
    public int SetNumber { get; set; }
    
    /// <summary>
    /// Number of reps planned for this set.
    /// </summary>
    public int RepsPlanned { get; set; }
    
    /// <summary>
    /// Weight planned for this set.
    /// </summary>
    public decimal WeightPlanned { get; set; }
    
    /// <summary>
    /// Number of reps actually completed, or null if not performed.
    /// </summary>
    public int? RepsDone { get; set; }
    
    /// <summary>
    /// Weight actually used, or null if not performed.
    /// </summary>
    public decimal? WeightDone { get; set; }
    
    /// <summary>
    /// Rate of Perceived Exertion (RPE) for this set, if recorded.
    /// </summary>
    public decimal? Rpe { get; set; }
    
    /// <summary>
    /// Indicates whether this set was performed to failure.
    /// </summary>
    public bool? IsFailure { get; set; }
    
    /// <summary>
    /// Indicates whether this set was an extra set not in the original plan.
    /// </summary>
    public bool IsExtra { get; set; }
    
    /// <summary>
    /// Difference between actual and planned reps (RepsDone - RepsPlanned).
    /// </summary>
    public int RepsDiff => (RepsDone ?? 0) - RepsPlanned;
    
    /// <summary>
    /// Difference between actual and planned weight (WeightDone - WeightPlanned).
    /// </summary>
    public decimal WeightDiff => (WeightDone ?? 0) - WeightPlanned;
}

/// <summary>
/// Query parameters for retrieving overview statistics.
/// </summary>
public sealed class OverviewQueryDto : IValidatableObject
{
    /// <summary>
    /// Start date of the query range.
    /// </summary>
    [Required] public DateTime From { get; set; }
    
    /// <summary>
    /// End date of the query range (must be after From).
    /// </summary>
    [Required] public DateTime To { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (To < From) yield return new ValidationResult("'To' must be >= 'From'.", new[] { nameof(To) });
    }

    public (DateTime FromUtc, DateTime ToUtc) ToUtcRange() => DateHelpers.NormalizeRange(From, To);
}

/// <summary>
/// Query parameters for retrieving volume statistics.
/// </summary>
public sealed class VolumeQueryDto : IValidatableObject
{
    /// <summary>
    /// Start date of the query range.
    /// </summary>
    [Required] public DateTime From { get; set; }
    
    /// <summary>
    /// End date of the query range (must be after From).
    /// </summary>
    [Required] public DateTime To { get; set; }
    
    /// <summary>
    /// How to group the volume data: 'day', 'week', or 'exercise'.
    /// </summary>
    [RegularExpression("(?i)^(day|week|exercise)$", ErrorMessage = "GroupBy: 'day', 'week', 'exercise'.")]
    public string GroupBy { get; set; } = "day";
    
    /// <summary>
    /// Optional filter for a specific exercise name.
    /// </summary>
    [StringLength(200)]
    public string? ExerciseName { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (To < From) yield return new ValidationResult("'To' must be >= 'From'.", new[] { nameof(To) });
    }

    public (DateTime FromUtc, DateTime ToUtc) ToUtcRange() => DateHelpers.NormalizeRange(From, To);
    public string GroupByNormalized => GroupBy.Trim().ToLowerInvariant();
}

/// <summary>
/// Query parameters for retrieving estimated 1-rep max statistics.
/// </summary>
public sealed class E1rmQueryDto : IValidatableObject
{
    /// <summary>
    /// Start date of the query range.
    /// </summary>
    [Required] public DateTime From { get; set; }
    
    /// <summary>
    /// End date of the query range (must be after From).
    /// </summary>
    [Required] public DateTime To { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (To < From) yield return new ValidationResult("'To' must be >= 'From'.", new[] { nameof(To) });
    }

    public (DateTime FromUtc, DateTime ToUtc) ToUtcRange() => DateHelpers.NormalizeRange(From, To);
}

/// <summary>
/// Query parameters for retrieving adherence statistics.
/// </summary>
public sealed class AdherenceQueryDto : IValidatableObject
{
    /// <summary>
    /// Start date of the query range.
    /// </summary>
    [Required] public DateOnly FromDate { get; set; }
    
    /// <summary>
    /// End date of the query range (must be after FromDate).
    /// </summary>
    [Required] public DateOnly ToDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (ToDate < FromDate) yield return new ValidationResult("'To' must be >= 'From'.", new[] { nameof(ToDate) });
    }
    
    public (DateOnly From, DateOnly To) ToRange() => (FromDate, ToDate);
}

/// <summary>
/// Query parameters for retrieving plan vs actual comparison.
/// </summary>
public sealed class PlanVsActualQueryDto
{
    /// <summary>
    /// ID of the workout session to compare against its planned workout.
    /// </summary>
    [Required] public Guid SessionId { get; set; }
}
