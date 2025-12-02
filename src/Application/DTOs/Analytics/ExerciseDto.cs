namespace Application.DTOs.Analytics;

/// <summary>
/// Represents a summary of an exercise performed by the user.
/// </summary>
public class ExerciseSummaryDto
{
    /// <summary>
    /// Name of the exercise.
    /// </summary>
    public string Name { get; set; } = default!;
    
    /// <summary>
    /// Number of workout sessions containing this exercise.
    /// </summary>
    public int WorkoutCount { get; set; }
    
    /// <summary>
    /// First time this exercise was performed (UTC).
    /// </summary>
    public DateTime FirstPerformedUtc { get; set; }
    
    /// <summary>
    /// Most recent time this exercise was performed (UTC).
    /// </summary>
    public DateTime LastPerformedUtc { get; set; }
}
