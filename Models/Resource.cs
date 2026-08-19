using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MIC.risk.Models
{
    public class Resource
    {
        public long Id { get; set; }
        [Column("Name")]
        public string Name { get; set; } = null!;

        // FK to Employee
        public long EmpId { get; set; }
        public Employee Employee { get; set; } = null!;

        public string Url { get; set; } = null!;

        public string Type { get; set; } = null!;

        public string? Description { get; set; }

        public bool Active { get; set; } = true;

        public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}