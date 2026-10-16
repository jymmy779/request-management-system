namespace RequestManagementSystem.Api.Models;

public class Request
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending";

    public string RequesterId { get; set; } = string.Empty;

    public ApplicationUser Requester { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}