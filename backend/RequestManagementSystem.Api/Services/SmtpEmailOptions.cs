using System.ComponentModel.DataAnnotations;

namespace RequestManagementSystem.Api.Services;

public class SmtpEmailOptions
{
    public const string SectionName = "Email";

    [Required]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; }

    [Required]
    [EmailAddress]
    public string FromAddress { get; set; } = string.Empty;

    [Required]
    public string FromName { get; set; } = string.Empty;

    public bool UseSsl { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }
}
