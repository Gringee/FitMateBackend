using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Application.Common;

namespace Application.DTOs;

public sealed class FriendScheduledWorkoutDto
{
    public Guid ScheduledId { get; set; }
    
    public Guid UserId { get; set; }
    public string UserName { get; set; } = null!;
    public string FullName { get; set; } = string.Empty;

    public DateOnly Date { get; set; }       
    public TimeOnly? Time { get; set; }           
    public string PlanName { get; set; } = null!;
    public string Status { get; set; } = null!;  // "planned" | "completed"
}

public sealed class FriendWorkoutSessionDto
{
    public Guid SessionId { get; set; }
    public Guid ScheduledId { get; set; }

    public Guid UserId { get; set; }
    public string UserName { get; set; } = null!;
    public string FullName { get; set; } = string.Empty;
    
    public string PlanName { get; set; } = null!;

    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int? DurationSec { get; set; }

    public string Status { get; set; } = null!; // "in_progress" | "completed" | "aborted" 
}

public sealed class FriendsScheduledRangeRequest : IValidatableObject
{
    [Required]
    public DateOnly From { get; set; } 

    [Required]
    public DateOnly To { get; set; }

    public (DateOnly FromDate, DateOnly ToDate) Normalize() => (From, To);

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (To < From)
        {
            yield return new ValidationResult(
                "To must be greater than or equal to From.",
                new[] { nameof(From), nameof(To) });
        }
    }
}

public sealed class FriendsSessionsRangeRequest : IValidatableObject
{
    [Required]
    public DateTime FromUtc { get; set; } 

    [Required]
    public DateTime ToUtc { get; set; }

    public (DateTime From, DateTime To) NormalizeToUtc() => DateHelpers.NormalizeRange(FromUtc, ToUtc);

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (ToUtc <= FromUtc)
        {
            yield return new ValidationResult(
                "ToUtc must be greater than FromUtc.",
                new[] { nameof(FromUtc), nameof(ToUtc) });
        }
    }
}