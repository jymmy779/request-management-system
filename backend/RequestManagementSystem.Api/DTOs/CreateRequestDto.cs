using System.ComponentModel.DataAnnotations;
using RequestManagementSystem.Api.Models;

namespace RequestManagementSystem.Api.DTOs;

public class CreateRequestDto
{
    [Required]
    [StringLength(150, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [RegularExpression(
        RequestCategories.ValidationPattern,
        ErrorMessage = "Category must be Equipment, Software, or Other.")]
    public string Category { get; set; } = string.Empty;
}
