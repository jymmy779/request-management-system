using System.ComponentModel.DataAnnotations;

namespace RequestManagementSystem.Api.DTOs;

public class ChangePasswordDto
{
    [Required]
    [StringLength(100)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string NewPassword { get; set; } = string.Empty;
}
