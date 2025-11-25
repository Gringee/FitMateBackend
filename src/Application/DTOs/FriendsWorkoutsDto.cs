using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Application.Common;

namespace Application.DTOs;

/// <summary>
/// Represents a scheduled workout from a friend's timeline.
/// </summary>
public sealed class FriendScheduledWorkoutDto
{
    /// <summary>
    /// Unique identifier of the scheduled workout.
    /// </summary>
    public Guid ScheduledId { get; set; }
    
    /// <summary>
    /// ID of the friend user who owns this workout.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Username of the friend.
    /// </summary>
    public string UserName { get; set; } = null!;

    /// <summary>
    /// Full name of the friend.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Date of the scheduled workout.
    /// </summary>
    public DateOnly Date { get; set; }       

    /// <summary>
    /// Optional time of the workout.
    /// </summary>
    public TimeOnly? Time { get; set; }           

    /// <summary>
    /// Name of the workout plan.
    /// </summary>
    public string PlanName { get; set; } = null!;

    /// <summary>
    /// Status of the workout.
    /// Possible values: "planned", "completed".
    /// </summary>
    public string Status { get; set; } = null!;  // "planned" | "completed"
}

/// <summary>
/// Represents a workout session from a friend's timeline.
/// </summary>
public sealed class FriendWorkoutSessionDto
{
    /// <summary>
    /// Unique identifier of the workout session.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// ID of the scheduled workout this session is based on.
    /// </summary>
    public Guid ScheduledId { get; set; }

    /// <summary>
    /// ID of the friend user who performed this session.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Username of the friend.
    /// </summary>
    public string UserName { get; set; } = null!;

    /// <summary>
    /// Full name of the friend.
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the workout plan.
    /// </summary>
    public string PlanName { get; set; } = null!;

    /// <summary>
    /// Start time of the session in UTC.
    /// </summary>
    public DateTime StartedAtUtc { get; set; }

    /// <summary>
    /// Completion time of the session in UTC (if completed).
    /// </summary>
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>
    /// Duration of the session in seconds.
    /// </summary>
    public int? DurationSec { get; set; }

    /// <summary>
    /// Status of the session.
    /// Possible values: "in_progress", "completed", "aborted".
    /// </summary>
    public string Status { get; set; } = null!; // "in_progress" | "completed" | "aborted" 
}

/// <summary>
/// Request to retrieve friends' scheduled workouts within a date range.
/// </summary>
public sealed class FriendsScheduledRangeRequest : IValidatableObject
{
    /// <summary>
    /// Start date of the range.
    /// </summary>
    [Required]
    public DateOnly From { get; set; } 

    /// <summary>
    /// End date of the range.
    /// </summary>
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

/// <summary>
/// Request to retrieve friends' workout sessions within a date/time range.
/// </summary>
public sealed class FriendsSessionsRangeRequest : IValidatableObject
{
    /// <summary>
    /// Start date and time of the range (UTC).
    /// </summary>
    [Required]
    public DateTime FromUtc { get; set; } 

    /// <summary>
    /// End date and time of the range (UTC).
    /// </summary>
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