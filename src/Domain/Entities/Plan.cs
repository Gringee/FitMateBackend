using System.Collections.Generic;

namespace Domain.Entities;

/// <summary>
/// Represents a workout plan template.
/// </summary>
public class Plan
{
    /// <summary>
    /// Unique identifier of the plan.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the plan.
    /// </summary>
    public string PlanName { get; set; } = null!;

    /// <summary>
    /// Type of the plan (e.g., PPL, BroSplit).
    /// </summary>
    public string Type { get; set; } = null!;

    /// <summary>
    /// Optional notes for the plan.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Collection of exercises in the plan.
    /// </summary>
    public ICollection<PlanExercise> Exercises { get; set; } = new List<PlanExercise>();
    
    /// <summary>
    /// ID of the user who created the plan.
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Navigation property to the creator user.
    /// </summary>
    public User CreatedByUser { get; set; } = null!;
}
