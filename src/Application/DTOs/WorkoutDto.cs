using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class WorkoutDto
    {
        public Guid WorkoutId { get; set; }
        public Guid UserId { get; set; }
        public DateTime WorkoutDate { get; set; }
        public int DurationMinutes { get; set; }
        public string? Notes { get; set; }
        public List<WorkoutExerciseDto> Exercises { get; set; } = new();
    }
}
