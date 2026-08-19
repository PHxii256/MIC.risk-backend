using System.ComponentModel.DataAnnotations;

namespace MIC.risk.DTOs.Auth;

public class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>An administrator setting a new password for an employee who cannot sign in.</summary>
public class ResetPasswordDto
{
    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}
