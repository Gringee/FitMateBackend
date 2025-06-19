using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;

namespace Domain.Entities
{
    public class Workout
    {
        public Guid WorkoutId { get; set; }
        public Guid UserId { get; set; }
        public DateTime WorkoutDate { get; set; }
        public int DurationMinutes { get; set; }
        public string? Name { get; set; }
        public string? Notes { get; set; }
        public WorkoutStatus Status { get; set; } = WorkoutStatus.Planned;

        public ICollection<WorkoutExercise> Exercises { get; set; } = new List<WorkoutExercise>();
    }
}
