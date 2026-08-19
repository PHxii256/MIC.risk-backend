using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIC.risk.Models
{
    public class RiskReportEvaluation
    {
        public long Id { get; set; }

        // FK to Employee
        public long EmpId { get; set; }
        public Employee Employee { get; set; } = null!;

        public int Severity { get; set; }
        public int Frequency { get; set; }
        public int ControlEffectiveness { get; set; }

        // Computed in the database: Severity * Frequency
        public int InherentRisk { get; private set; }

        // Computed in the database: InherentRisk * the control-effectiveness rate
        public int ResidualRisk { get; private set; }

        public string? ExistingMeasures { get; set; }
        public string? ProposedMeasures { get; set; }
        public int Priority { get; set; } = 1;
        public DateTimeOffset EvaluatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}