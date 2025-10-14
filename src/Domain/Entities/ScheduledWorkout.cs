using Domain.Enums;

namespace Domain.Entities;

public class ScheduledWorkout
{
    public Guid Id { get; set; }

    public DateOnly Date { get; set; }
    public TimeOnly? Time { get; set; }

    public Guid PlanId { get; set; }            
    public string PlanName { get; set; } = null!; 
    public string? Notes { get; set; }

    public ScheduledStatus Status { get; set; } = ScheduledStatus.Planned;

    public Plan Plan { get; set; } = null!;
    public ICollection<ScheduledExercise> Exercises { get; set; } = new List<ScheduledExercise>();
}
