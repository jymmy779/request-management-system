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

    [Required]
    [RegularExpression(
        RequestPriorities.ValidationPattern,
        ErrorMessage = "Priority must be Low, Medium, or High.")]
    public string Priority { get; set; } = RequestPriorities.Medium;

    [Range(1, 1000)]
    public int Quantity { get; set; } = 1;

    [Range(
        typeof(decimal),
        "0.01",
        "999999999999.99",
        ErrorMessage = "Estimated cost must be greater than 0.")]
    public decimal? EstimatedCost { get; set; }
}
