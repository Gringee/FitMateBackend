using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class CreateWorkoutDto
    {
        public DateTime WorkoutDate { get; set; }
        public int DurationMinutes { get; set; }
        public string? Notes { get; set; }
        public List<WorkoutExerciseDto> Exercises { get; set; } = new();
    }

    public class WorkoutExerciseDto
    {
        public Guid ExerciseId { get; set; }
        public int SetNumber { get; set; }
        public int Repetitions { get; set; }
        public decimal Weight { get; set; }
        public int DurationSeconds { get; set; }
        public string? Notes { get; set; }
    }
}
