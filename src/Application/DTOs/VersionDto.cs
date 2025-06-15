using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class VersionDto
    {
        public string Version { get; init; } = default!;
        public DateTime BuildTimeUtc { get; init; }

        public VersionDto(string version, DateTime buildTimeUtc)
        {
            Version = version;
            BuildTimeUtc = buildTimeUtc;
        }
    }
}
