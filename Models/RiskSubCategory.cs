using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MIC.risk.Models
{
    public class RiskSubCategory
    {
        public long Id { get; set; }
        public required string NameEn { get; set; } = null!;
        public required string NameAr { get; set; } = null!;
        public required string Category { get; set; } = null!;
        public bool Active { get; set; } = true;
    }
}