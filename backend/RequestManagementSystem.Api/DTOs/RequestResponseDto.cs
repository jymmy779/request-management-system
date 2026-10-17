namespace RequestManagementSystem.Api.DTOs;

public class RequestResponseDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string RequesterId { get; set; } = string.Empty;

    public string RequesterFullName { get; set; } = string.Empty;

    public string RequesterEmail { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
