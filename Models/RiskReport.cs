using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIC.risk.Models
{
    public class RiskReport
    {
        public long Id { get; set; }

        // FK to Employee who submitted the report
        public long EmpId { get; set; }
        public Employee Employee { get; set; } = null!;

        // FK to RiskSubCategory
        public long SubCategoryId { get; set; }
        public RiskSubCategory SubCategory { get; set; } = null!;

        // FK to Evaluation submitted by Reporter
        public long ReportedEvaluationId { get; set; }
        public RiskReportEvaluation ReportedEvaluation { get; set; } = null!;

        // Optional FK to Evaluation submitted by Auditor
        public long? AuditorEvaluationId { get; set; }
        public RiskReportEvaluation? AuditorEvaluation { get; set; }

        public string Description { get; set; } = null!;
        public string Status { get; set; } = "Submitted";
        public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ResolvedAt { get; set; }

        // Navigation collection to audit trail
        public ICollection<RiskReportStatusHistory> StatusHistories { get; set; } = new List<RiskReportStatusHistory>();
    }
}