namespace Domain.Entities;

/// <summary>
/// Represents an exercise within a scheduled workout.
/// </summary>
public class ScheduledExercise
{
    public Guid Id { get; set; }
    public Guid ScheduledWorkoutId { get; set; }
    public string Name { get; set; } = null!;
    public int RestSeconds { get; set; } = 90;

    public ScheduledWorkout ScheduledWorkout { get; set; } = null!;
    public ICollection<ScheduledSet> Sets { get; set; } = new List<ScheduledSet>();
}
