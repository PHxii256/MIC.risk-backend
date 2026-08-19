using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MIC.risk.Models
{
    public class Employee
    {
        public long Id { get; set; }

        // FK to ASP.NET Core Identity
        public string IdentityUserId { get; set; } = null!;
        public AppUser IdentityUser { get; set; } = null!;

        [Column("Name")]
        public string Name { get; set; } = null!;

        // FK to Department
        public long DeptId { get; set; }
        public Department Department { get; set; } = null!;

        public bool Active { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}