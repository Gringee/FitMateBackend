using Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class CreateExerciseDto
    {
        [Required, MaxLength(50)]
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        [EnumDataType(typeof(DifficultyLevel))]
        public DifficultyLevel DifficultyLevel { get; set; }
        public Guid? BodyPartId { get; set; }
        public Guid? CategoryId { get; set; }
    }

    public class UpdateExerciseDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public DifficultyLevel DifficultyLevel { get; set; }
        public Guid? BodyPartId { get; set; }
        public Guid? CategoryId { get; set; }
    }

    public class ExerciseDto
    {
        public Guid ExerciseId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public DifficultyLevel DifficultyLevel { get; set; }
        public Guid? BodyPartId { get; set; }
        public Guid? CategoryId { get; set; }
    }
}

