namespace Domain.Entities;

public class PlanSet
{
    public Guid Id { get; set; }
    public Guid PlanExerciseId { get; set; }
    public int SetNumber { get; set; }
    public int Reps { get; set; }
    public decimal Weight { get; set; }

    public PlanExercise PlanExercise { get; set; } = null!;
}
