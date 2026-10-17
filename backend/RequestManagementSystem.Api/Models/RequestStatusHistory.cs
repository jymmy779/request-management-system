namespace RequestManagementSystem.Api.Models;

public class RequestStatusHistory
{
    public long Id { get; set; }

    public int RequestId { get; set; }

    public Request Request { get; set; } = null!;

    public string? FromStatus { get; set; }

    public string ToStatus { get; set; } = string.Empty;

    public string ChangedByUserId { get; set; } = string.Empty;

    public ApplicationUser ChangedByUser { get; set; } = null!;

    public string? Note { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
