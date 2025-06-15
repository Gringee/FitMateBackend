using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class CreateCategoryDto
    {
        [Required, MaxLength(50)]
        public string Name { get; set; } = null!;

        [MaxLength(200)]
        public string? Description { get; set; }
    }

    public class UpdateCategoryDto
    {
        [Required, MaxLength(50)]
        public string Name { get; set; } = null!;

        [MaxLength(200)]
        public string? Description { get; set; }
    }

    public class CategoryDto
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }
}
