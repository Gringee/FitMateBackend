using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Represents a scheduled workout based on a plan.
/// </summary>
public class ScheduledWorkout
{
    /// <summary>
    /// Unique identifier of the scheduled workout.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Date of the scheduled workout.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Optional time of the scheduled workout.
    /// </summary>
    public TimeOnly? Time { get; set; }

    /// <summary>
    /// ID of the plan used.
    /// </summary>
    public Guid PlanId { get; set; }            

    /// <summary>
    /// Name of the plan (snapshot).
    /// </summary>
    public string PlanName { get; set; } = null!; 

    /// <summary>
    /// Optional notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Status of the scheduled workout.
    /// </summary>
    public ScheduledStatus Status { get; set; } = ScheduledStatus.Planned;

    /// <summary>
    /// Navigation property to the plan.
    /// </summary>
    public Plan Plan { get; set; } = null!;

    /// <summary>
    /// Collection of exercises in this workout.
    /// </summary>
    public ICollection<ScheduledExercise> Exercises { get; set; } = new List<ScheduledExercise>();
    
    /// <summary>
    /// ID of the user who owns this workout.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Navigation property to the user.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Whether this workout is visible to friends.
    /// </summary>
    public bool IsVisibleToFriends { get; set; } = false;
}
