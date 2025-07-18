﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class BodyPart
    {
        public Guid BodyPartId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        public ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
    }
}
