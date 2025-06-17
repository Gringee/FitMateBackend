using System;
using System.Collections.Generic;
using Domain.Enums;

namespace Application.DTOs
{
    public class WorkoutDto
    {
        public Guid WorkoutId { get; set; }
        public Guid UserId { get; set; }
        public DateTime WorkoutDate { get; set; }
        public int DurationMinutes { get; set; }
        public string? Notes { get; set; }
        public WorkoutStatus Status { get; set; } = WorkoutStatus.Planned;

        public List<WorkoutExerciseDto> Exercises { get; set; } = new();
    }
    public class CreateWorkoutDto
    {
        public DateTime WorkoutDate { get; set; }
        public int DurationMinutes { get; set; }
        public string? Notes { get; set; }
        public List<WorkoutExerciseDto> Exercises { get; set; } = new();
    }

    public class UpdateWorkoutStatusDto
    {
        public WorkoutStatus Status { get; set; }
    }

    public class DuplicateWorkoutDto
    {
        public DateTime? WorkoutDate { get; set; }
        public string? Notes { get; set; }
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