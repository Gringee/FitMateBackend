namespace Domain.Entities;

public class Exercise
{
    public Guid ExerciseId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string DifficultyLevel { get; set; } = null!;
    public Guid BodyPartId { get; set; }
    public Guid? CategoryId { get; set; }

    public ICollection<WorkoutExercise> WorkoutExercises { get; set; } = new List<WorkoutExercise>();
}
