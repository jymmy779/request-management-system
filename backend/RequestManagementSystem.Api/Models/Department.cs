using System.ComponentModel.DataAnnotations;

namespace RequestManagementSystem.Api.Models;

public class Department
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public ICollection<ApplicationUser> Users { get; set; } =
        new List<ApplicationUser>();
}
