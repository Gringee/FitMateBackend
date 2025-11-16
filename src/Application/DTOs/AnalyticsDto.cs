using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Analytics;

public class OverviewDto
{
    [Range(0, double.MaxValue)] public decimal TotalVolume { get; set; }

    public double AvgIntensity { get; set; }

    [Range(0, int.MaxValue)] public int SessionsCount { get; set; }

    [Range(0, 100)] public double AdherencePct { get; set; }

    [Range(0, int.MaxValue)] public int NewPrs { get; set; }
}

public class TimePointDto
{
    [Required]
    [MaxLength(32)] // "2025-11-01" albo "2025-W45" – 32 to aż nadto
    public string Period { get; set; } = default!;

    public decimal Value { get; set; }

    // przy groupBy=exercise będzie wypełnione
    public string? ExerciseName { get; set; }
}

public class E1rmPointDto
{
    public DateOnly Day { get; set; }

    [Range(0, double.MaxValue)] public decimal E1Rm { get; set; }

    public Guid? SessionId { get; set; }
}

public class AdherenceDto
{
    [Range(0, int.MaxValue)] public int Planned { get; set; }

    [Range(0, int.MaxValue)] public int Completed { get; set; }

    public int Missed => Planned - Completed < 0 ? 0 : Planned - Completed;

    [Range(0, 100)]
    public double AdherencePct => Planned == 0
        ? 0
        : Math.Round((double)Completed / Planned * 100, 1);
}

public class PlanVsActualItemDto
{
    [Required] [MaxLength(200)] public string ExerciseName { get; set; } = default!;

    [Range(1, int.MaxValue)] public int SetNumber { get; set; }

    [Range(0, int.MaxValue)] public int RepsPlanned { get; set; }

    [Range(0, double.MaxValue)] public decimal WeightPlanned { get; set; }

    [Range(0, int.MaxValue)] public int? RepsDone { get; set; }

    [Range(0, double.MaxValue)] public decimal? WeightDone { get; set; }

    [Range(0, 10)] public decimal? Rpe { get; set; }

    public bool? IsFailure { get; set; }

    public int RepsDiff => (RepsDone ?? 0) - RepsPlanned;
    public decimal WeightDiff => (WeightDone ?? 0) - WeightPlanned;
}

public class OverviewQueryDto : IValidatableObject
{
    [Required]
    public DateTime From { get; set; }

    [Required]
    public DateTime To { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        var (fromUtc, toUtc) = ToUtcRange();

        if (toUtc <= fromUtc)
            yield return new ValidationResult(
                "Parameter 'to' must be greater than 'from'.",
                new[] { nameof(To), nameof(From) });
    }

    public (DateTime FromUtc, DateTime ToUtc) ToUtcRange()
    {
        var from = From.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(From, DateTimeKind.Utc)
            : From;

        var to = To.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(To, DateTimeKind.Utc)
            : To;

        return (from, to);
    }
}

public class VolumeQueryDto : IValidatableObject
{
    [Required] public DateTime From { get; set; }

    [Required] public DateTime To { get; set; }
    
    public string GroupBy { get; set; } = "day";
    
    public string? ExerciseName { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        var (fromUtc, toUtc) = ToUtcRange();

        if (toUtc <= fromUtc)
            yield return new ValidationResult(
                "Parameter 'to' must be greater than 'from'.",
                new[] { nameof(To), nameof(From) });

        var gb = GroupByNormalized;
        if (gb is not ("day" or "week" or "exercise"))
            yield return new ValidationResult(
                "groupBy must be one of: 'day', 'week', 'exercise'.",
                new[] { nameof(GroupBy) });

        if (ExerciseName is { Length: > 200 })
            yield return new ValidationResult(
                "ExerciseName max length is 200 characters.",
                new[] { nameof(ExerciseName) });
    }

    public (DateTime FromUtc, DateTime ToUtc) ToUtcRange()
    {
        var from = From.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(From, DateTimeKind.Utc)
            : From;

        var to = To.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(To, DateTimeKind.Utc)
            : To;

        return (from, to);
    }

    public string GroupByNormalized =>
        string.IsNullOrWhiteSpace(GroupBy)
            ? "day"
            : GroupBy.Trim().ToLowerInvariant();
}

public class E1rmQueryDto : IValidatableObject
{
    [Required] public DateTime From { get; set; }

    [Required] public DateTime To { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        var (fromUtc, toUtc) = ToUtcRange();

        if (toUtc <= fromUtc)
            yield return new ValidationResult(
                "Parameter 'to' must be greater than 'from'.",
                new[] { nameof(To), nameof(From) });
    }

    public (DateTime FromUtc, DateTime ToUtc) ToUtcRange()
    {
        var from = From.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(From, DateTimeKind.Utc)
            : From;

        var to = To.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(To, DateTimeKind.Utc)
            : To;

        return (from, to);
    }
}

public class AdherenceQueryDto : IValidatableObject
{
    [Required] public DateOnly FromDate { get; set; }

    [Required] public DateOnly ToDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (ToDate <= FromDate)
            yield return new ValidationResult(
                "Parameter 'toDate' must be greater than 'fromDate'.",
                new[] { nameof(ToDate), nameof(FromDate) });
    }

    public (DateOnly From, DateOnly To) ToRange()
    {
        return (FromDate, ToDate);
    }
}

public class PlanVsActualQueryDto : IValidatableObject
{
    [Required] public Guid SessionId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (SessionId == Guid.Empty)
            yield return new ValidationResult(
                "SessionId must be a non-empty GUID.",
                new[] { nameof(SessionId) });
    }
}