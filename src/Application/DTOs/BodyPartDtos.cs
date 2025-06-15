using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class CreateBodyPartDto
    {
        [Required, MaxLength(50)]
        public string Name { get; set; } = null!;

        [MaxLength(200)]
        public string? Description { get; set; }
    }

    public class UpdateBodyPartDto
    {
        [Required, MaxLength(50)]
        public string Name { get; set; } = null!;

        [MaxLength(200)]
        public string? Description { get; set; }
    }

    public class BodyPartDto
    {
        public Guid BodyPartId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
    }
}
