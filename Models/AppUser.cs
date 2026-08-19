using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace MIC.risk.Models
{
    public class AppUser : IdentityUser
    {
        public Employee? EmployeeProfile { get; set; }
    }
}