using System.ComponentModel.DataAnnotations;
using RequestManagementSystem.Api.Authorization;

namespace RequestManagementSystem.Api.DTOs;

public class CreateUserDto
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [RegularExpression(AppRoles.ValidationPattern)]
    public string Role { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int? DepartmentId { get; set; }
}
