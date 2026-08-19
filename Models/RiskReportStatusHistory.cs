using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIC.risk.Models
{
    public class RiskReportStatusHistory
    {
        public long Id { get; set; }

        // FK to RiskReport
        public long ReportId { get; set; }
        public RiskReport Report { get; set; } = null!;

        // FK to Employee who changed status
        public long ChangedBy { get; set; }
        public Employee ChangedByEmployee { get; set; } = null!;

        public string OldStatus { get; set; } = null!;
        public string NewStatus { get; set; } = null!;
        public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}