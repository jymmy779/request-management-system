using System.ComponentModel.DataAnnotations;

namespace RequestManagementSystem.Api.Services;

public class FrontendOptions
{
    public const string SectionName = "Frontend";

    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;
}
