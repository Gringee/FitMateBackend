using System.Collections.Generic;

namespace Domain.Entities;

/// <summary>
/// Represents a workout plan template.
/// </summary>
public class Plan
{
    public Guid Id { get; set; }
    public string PlanName { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string? Notes { get; set; }

    public ICollection<PlanExercise> Exercises { get; set; } = new List<PlanExercise>();
    
    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
}
