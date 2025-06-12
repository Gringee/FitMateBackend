using Domain.Enums;

namespace Domain.Entities;

public class Exercise
{
    public Guid ExerciseId { get; set; }

    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public DifficultyLevel DifficultyLevel { get; set; }

    public Guid? BodyPartId { get; set; }
    public Guid? CategoryId { get; set; }

    public BodyPart? BodyPart { get; set; }
    public Category? Category { get; set; }

    public ICollection<WorkoutExercise> WorkoutExercises { get; set; } = new List<WorkoutExercise>();
}
