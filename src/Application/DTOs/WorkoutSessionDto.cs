using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class SessionSetDto
{
    public Guid Id { get; set; }
    public int SetNumber { get; set; }
    public int RepsPlanned { get; set; }
    public decimal WeightPlanned { get; set; }
    public int? RepsDone { get; set; }
    public decimal? WeightDone { get; set; }
    public decimal? Rpe { get; set; }
    public bool? IsFailure { get; set; }
}

public class SessionExerciseDto
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public string Name { get; set; } = null!;
    public int RestSecPlanned { get; set; }
    public int? RestSecActual { get; set; }
    public List<SessionSetDto> Sets { get; set; } = new();
}

public class WorkoutSessionDto
{
    public Guid Id { get; set; }
    public Guid ScheduledId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int? DurationSec { get; set; }
    public string Status { get; set; } = null!;
    public string? SessionNotes { get; set; }
    public List<SessionExerciseDto> Exercises { get; set; } = new();
}

public class StartSessionRequest : IValidatableObject
{
    [Required(ErrorMessage = "ScheduledId is required.")]
    public Guid ScheduledId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (ScheduledId == Guid.Empty)
        {
            yield return new ValidationResult(
                "ScheduledId must be a non-empty GUID.",
                new[] { nameof(ScheduledId) });
        }
    }
}

public class PatchSetRequest : IValidatableObject
{
    public int? RepsDone { get; set; }
    public decimal? WeightDone { get; set; }
    public decimal? Rpe { get; set; }
    public bool? IsFailure { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (RepsDone is < 0)
            yield return new ValidationResult("RepsDone cannot be negative.", new[] { nameof(RepsDone) });

        if (WeightDone is < 0)
            yield return new ValidationResult("WeightDone cannot be negative.", new[] { nameof(WeightDone) });

        if (Rpe is < 0 or > 10)
            yield return new ValidationResult("Rpe must be between 0 and 10.", new[] { nameof(Rpe) });
    }
}

public class CompleteSessionRequest : IValidatableObject
{
    [MaxLength(1000, ErrorMessage = "SessionNotes max length is 1000 chars.")]
    public string? SessionNotes { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (CompletedAtUtc is { Kind: DateTimeKind.Local })
        {
            yield return new ValidationResult(
                "CompletedAtUtc should be in UTC (DateTimeKind.Utc).",
                new[] { nameof(CompletedAtUtc) });
        }
    }
}

public class AbortSessionRequest
{
    [MaxLength(500, ErrorMessage = "Reason max length is 500 chars.")]
    public string? Reason { get; set; }
}

public sealed class AddSessionExerciseRequest : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "Order must be >= 1.")]
    public int? Order { get; set; }

    [Required]
    [MaxLength(200, ErrorMessage = "Name max length is 200 chars.")]
    public string Name { get; set; } = null!;
    
    [Range(0, int.MaxValue, ErrorMessage = "RestSecPlanned cannot be negative.")]
    public int? RestSecPlanned { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one set is required.")]
    public List<AddSessionSetRequest> Sets { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (Sets is null || Sets.Count == 0)
        {
            yield return new ValidationResult(
                "At least one set must be provided.",
                new[] { nameof(Sets) });
        }
    }
}

public sealed class AddSessionSetRequest : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "SetNumber must be >= 1.")]
    public int? SetNumber { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "RepsPlanned must be >= 1.")]
    public int RepsPlanned { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "WeightPlanned cannot be negative.")]
    public decimal WeightPlanned { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (RepsPlanned <= 0)
        {
            yield return new ValidationResult(
                "RepsPlanned must be greater than 0.",
                new[] { nameof(RepsPlanned) });
        }
    }
}