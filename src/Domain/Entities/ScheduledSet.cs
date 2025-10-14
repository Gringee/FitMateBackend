namespace Domain.Entities;

public class ScheduledSet
{
    public Guid Id { get; set; }
    public Guid ScheduledExerciseId { get; set; }
    public int SetNumber { get; set; }
    public int Reps { get; set; }
    public decimal Weight { get; set; }

    public ScheduledExercise ScheduledExercise { get; set; } = null!;
}
