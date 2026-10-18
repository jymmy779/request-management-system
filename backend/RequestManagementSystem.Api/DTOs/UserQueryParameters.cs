using System.ComponentModel.DataAnnotations;
using RequestManagementSystem.Api.Authorization;

namespace RequestManagementSystem.Api.DTOs;

public class UserQueryParameters
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    [StringLength(100)]
    public string? Search { get; set; }

    [RegularExpression(AppRoles.ValidationPattern)]
    public string? Role { get; set; }

    [Range(1, int.MaxValue)]
    public int? DepartmentId { get; set; }

    [RegularExpression("^(fullName|email)$")]
    public string SortBy { get; set; } = "fullName";

    [RegularExpression("^(asc|desc)$")]
    public string SortDirection { get; set; } = "asc";
}
