using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Analytics;

public class OverviewDto
{
    public decimal TotalVolume { get; set; }
    public double AvgIntensity { get; set; }
    public int SessionsCount { get; set; }
    public double AdherencePct { get; set; }
    public int NewPrs { get; set; }
}

public class TimePointDto
{
    public string Period { get; set; } = default!;
    public decimal Value { get; set; }
    public string? ExerciseName { get; set; }
}

public class E1rmPointDto
{
    public DateOnly Day { get; set; }
    public decimal E1Rm { get; set; }
    public Guid? SessionId { get; set; }
}

public class AdherenceDto
{
    public int Planned { get; set; }
    public int Completed { get; set; }
    public int Missed => Planned - Completed < 0 ? 0 : Planned - Completed;
    public double AdherencePct => Planned == 0 ? 0 : Math.Round((double)Completed / Planned * 100, 1);
}

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

public class OverviewQueryDto : IValidatableObject
{
    [Required] public DateTime From { get; set; }
    [Required] public DateTime To { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (To <= From) yield return new ValidationResult("'To' must be > 'From'.", new[] { nameof(To) });
    }

    public (DateTime FromUtc, DateTime ToUtc) ToUtcRange() => DateHelpers.NormalizeRange(From, To);
}

public class VolumeQueryDto : IValidatableObject
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

public class E1rmQueryDto : IValidatableObject
{
    [Required] public DateTime From { get; set; }
    [Required] public DateTime To { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (To <= From) yield return new ValidationResult("'To' must be > 'From'.", new[] { nameof(To) });
    }

    public (DateTime FromUtc, DateTime ToUtc) ToUtcRange() => DateHelpers.NormalizeRange(From, To);
}

public class AdherenceQueryDto : IValidatableObject
{
    [Required] public DateOnly FromDate { get; set; }
    [Required] public DateOnly ToDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (ToDate <= FromDate) yield return new ValidationResult("'To' must be > 'From'.", new[] { nameof(ToDate) });
    }
    
    public (DateOnly From, DateOnly To) ToRange() => (FromDate, ToDate);
}

public class PlanVsActualQueryDto
{
    [Required] public Guid SessionId { get; set; }
}

public static class DateHelpers 
{
    public static (DateTime From, DateTime To) NormalizeRange(DateTime from, DateTime to)
    {
        var f = from.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(from, DateTimeKind.Utc) : from.ToUniversalTime();
        var t = to.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(to, DateTimeKind.Utc) : to.ToUniversalTime();
        return (f, t);
    }
}