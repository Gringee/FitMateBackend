namespace Domain.Entities;

/// <summary>
/// Represents a set performed in a session exercise.
/// </summary>
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
