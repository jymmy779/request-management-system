using System.ComponentModel.DataAnnotations;

namespace RequestManagementSystem.Api.DTOs;

public class ResetPasswordDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(2048)]
    public string Token { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string NewPassword { get; set; } = string.Empty;
}
