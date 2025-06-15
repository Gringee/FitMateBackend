using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class DuplicateWorkoutDto
    {
        /// <summary>Docelowa data treningu; jeśli null – weź DateTime.Today.</summary>
        public DateTime? WorkoutDate { get; set; }
        public string? Notes { get; set; }
    }
}
