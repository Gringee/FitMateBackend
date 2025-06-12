using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class CreateExerciseDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string DifficultyLevel { get; set; } = null!;
        public Guid BodyPartId { get; set; }
        public Guid? CategoryId { get; set; }
    }

    public class UpdateExerciseDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string DifficultyLevel { get; set; } = null!;
        public Guid BodyPartId { get; set; }
        public Guid? CategoryId { get; set; }
    }

    public class ExerciseDto
    {
        public Guid ExerciseId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string DifficultyLevel { get; set; } = null!;
        public Guid BodyPartId { get; set; }
        public Guid? CategoryId { get; set; }
    }
}

