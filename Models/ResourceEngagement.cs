using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIC.risk.Models
{
    public class ResourceEngagement
    {
        public long Id { get; set; }

        // FK to Employee
        public long EmpId { get; set; }
        public Employee Employee { get; set; } = null!;

        // FK to Resource
        public long ResourceId { get; set; }
        public Resource Resource { get; set; } = null!;

    public bool Viewed { get; set; }
    public bool? SurveyCompleted { get; set; }
    public DateTimeOffset? ViewedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
}