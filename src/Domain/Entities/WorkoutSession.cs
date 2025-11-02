public class WorkoutSession
{
    public Guid Id { get; set; }
    public Guid ScheduledId { get; set; }        
    public Guid? UserId { get; set; }            
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int? DurationSec { get; set; }
    public string Status { get; set; } = "in_progress"; 
    public string? SessionNotes { get; set; }

    public ICollection<SessionExercise> Exercises { get; set; } = new List<SessionExercise>();
}

public class SessionExercise
{
    public Guid Id { get; set; }
    public Guid WorkoutSessionId { get; set; }   
    public int Order { get; set; }               
    public string Name { get; set; } = null!;
    public int RestSecPlanned { get; set; }
    public int? RestSecActual { get; set; }
    public ICollection<SessionSet> Sets { get; set; } = new List<SessionSet>();
}

public class SessionSet
{
    public Guid Id { get; set; }
    public Guid SessionExerciseId { get; set; }  
    public int SetNumber { get; set; }           
    public int RepsPlanned { get; set; }
    public decimal WeightPlanned { get; set; }
    public int? RepsDone { get; set; }
    public decimal? WeightDone { get; set; }
    public decimal? Rpe { get; set; }            
    public bool? IsFailure { get; set; }         
}