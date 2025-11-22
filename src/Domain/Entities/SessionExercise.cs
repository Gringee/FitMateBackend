namespace Domain.Entities;

/// <summary>
/// Represents an exercise performed in a workout session.
/// </summary>
public class SessionExercise
{
    public Guid Id { get; set; }
    public Guid WorkoutSessionId { get; set; }   
    public int Order { get; set; }               
    public string Name { get; set; } = null!;
    public int RestSecPlanned { get; set; }
    public int? RestSecActual { get; set; }
    public ICollection<SessionSet> Sets { get; set; } = new List<SessionSet>();
    public bool IsAdHoc { get; set; } = false; 
    public Guid? ScheduledExerciseId { get; set; }
}
