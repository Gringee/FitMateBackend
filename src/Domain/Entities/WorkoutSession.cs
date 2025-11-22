namespace Domain.Entities;

using Domain.Enums;

/// <summary>
/// Represents a workout session instance.
/// </summary>
public class WorkoutSession
{
    public Guid Id { get; set; }
    public Guid ScheduledId { get; set; }       
    public ScheduledWorkout Scheduled { get; set; } = null!;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int? DurationSec { get; set; }
    public WorkoutSessionStatus Status { get; set; } = WorkoutSessionStatus.InProgress; 
    public string? SessionNotes { get; set; }

    public ICollection<SessionExercise> Exercises { get; set; } = new List<SessionExercise>();
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}