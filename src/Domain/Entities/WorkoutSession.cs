namespace Domain.Entities;

using Domain.Enums;

/// <summary>
/// Represents a workout session instance.
/// </summary>
public class WorkoutSession
{
    /// <summary>
    /// Unique identifier of the session.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID of the scheduled workout this session is based on.
    /// </summary>
    public Guid ScheduledId { get; set; }       

    /// <summary>
    /// Navigation property to the scheduled workout.
    /// </summary>
    public ScheduledWorkout Scheduled { get; set; } = null!;

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
    /// Current status of the session.
    /// </summary>
    public WorkoutSessionStatus Status { get; set; } = WorkoutSessionStatus.InProgress; 

    /// <summary>
    /// Optional notes for the session.
    /// </summary>
    public string? SessionNotes { get; set; }

    /// <summary>
    /// Indicates if this session was created via quick complete endpoint (scheduled/{id}/complete)
    /// rather than live workout flow (sessions/start -> complete).
    /// Used to determine if session can be reopened.
    /// </summary>
    public bool IsQuickComplete { get; set; }

    /// <summary>
    /// Collection of exercises performed in the session.
    /// </summary>
    public ICollection<SessionExercise> Exercises { get; set; } = new List<SessionExercise>();
    
    /// <summary>
    /// ID of the user who performed the session.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to the user.
    /// </summary>
    public User User { get; set; } = null!;
}