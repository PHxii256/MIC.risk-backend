using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MIC.risk.DTOs.Auth
{
    public class RegisterDto
    {
        [Required]
        [MinLength(3)]
        [MaxLength(40)]
        public string Username { get; set; } = string.Empty;
        [EmailAddress]
        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}