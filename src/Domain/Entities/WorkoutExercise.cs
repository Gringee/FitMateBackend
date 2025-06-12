using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities;

public class WorkoutExercise
{
    public Guid WorkoutExerciseId { get; set; }
    public Guid WorkoutId { get; set; }
    public Guid ExerciseId { get; set; }

    public int SetNumber { get; set; }
    public int Repetitions { get; set; }
    public decimal Weight { get; set; }
    public int DurationSeconds { get; set; }
    public string? Notes { get; set; }

    public Workout Workout { get; set; } = null!;
    public Exercise Exercise { get; set; } = null!;
}
