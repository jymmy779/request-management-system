using System.ComponentModel.DataAnnotations;

namespace RequestManagementSystem.Api.DTOs;

public class RejectRequestDto
{
    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string Reason { get; set; } = string.Empty;
}
