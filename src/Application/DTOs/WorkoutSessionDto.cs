namespace Application.DTOs;

public class SessionSetDto
{
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

// Requests
public class StartSessionRequest { public Guid ScheduledId { get; set; } }

public class PatchSetRequest
{
    public int ExerciseOrder { get; set; }
    public int SetNumber { get; set; }
    public int? RepsDone { get; set; }
    public decimal? WeightDone { get; set; }
    public decimal? Rpe { get; set; }
    public bool? IsFailure { get; set; }
}

public class CompleteSessionRequest
{
    public string? SessionNotes { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public class AbortSessionRequest
{
    public string? Reason { get; set; } 
}