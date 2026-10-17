namespace RequestManagementSystem.Api.Models;

public class Request
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = RequestCategories.Other;

    public string Priority { get; set; } = RequestPriorities.Medium;

    public int Quantity { get; set; } = 1;

    public decimal? EstimatedCost { get; set; }

    public string Status { get; set; } = RequestStatuses.Pending;

    public string RequesterId { get; set; } = string.Empty;

    public ApplicationUser Requester { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<RequestStatusHistory> StatusHistory { get; set; } =
        new List<RequestStatusHistory>();
}
