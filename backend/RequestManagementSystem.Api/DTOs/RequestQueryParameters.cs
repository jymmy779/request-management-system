using System.ComponentModel.DataAnnotations;
using RequestManagementSystem.Api.Models;

namespace RequestManagementSystem.Api.DTOs;

public class RequestQueryParameters
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    [StringLength(100)]
    public string? Search { get; set; }

    [RegularExpression(RequestStatuses.ValidationPattern)]
    public string? Status { get; set; }

    [RegularExpression(RequestCategories.ValidationPattern)]
    public string? Category { get; set; }

    [RegularExpression(RequestPriorities.ValidationPattern)]
    public string? Priority { get; set; }

    [Range(1, int.MaxValue)]
    public int? DepartmentId { get; set; }

    [RegularExpression("^(createdAt|title|status)$")]
    public string SortBy { get; set; } = "createdAt";

    [RegularExpression("^(asc|desc)$")]
    public string SortDirection { get; set; } = "desc";
}
