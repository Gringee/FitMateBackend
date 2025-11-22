using System.Numerics;

namespace Domain.Entities;

/// <summary>
/// Represents an exercise within a workout plan template.
/// </summary>
public class PlanExercise
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public string Name { get; set; } = null!;
    public int RestSeconds { get; set; } = 90;

    public Plan Plan { get; set; } = null!;
    public ICollection<PlanSet> Sets { get; set; } = new List<PlanSet>();
}
