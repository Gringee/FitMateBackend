namespace Application.DTOs;

/// <summary>
/// Request to mark a scheduled workout as completed.
/// </summary>
public class CompleteScheduledRequest
{
    /// <summary>
    /// Optional start time of the session. Defaults to now if not provided.
    /// </summary>
    public DateTime? StartedAtUtc { get; set; }

    /// <summary>
    /// Optional completion time of the session. Defaults to now if not provided.
    /// </summary>
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>
    /// Optional notes for the completed session.
    /// </summary>
    public string? SessionNotes { get; set; }

    /// <summary>
    /// If true, copies planned reps/weight to actual reps/weight for each set. Defaults to true.
    /// </summary>
    public bool PopulateActuals { get; set; } = true;
}
