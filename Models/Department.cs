using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MIC.risk.Models
{
    public class Department
    {
        public long Id { get; set; }
        [Column("Name")]
        public string Name { get; set; } = null!;
        public string BranchLocation { get; set; } = null!;

        // Navigation collection
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}