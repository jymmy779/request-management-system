using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace RequestManagementSystem.Api.Models;

public class ApplicationUser : IdentityUser
{
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }

    public Department? Department { get; set; }
}
