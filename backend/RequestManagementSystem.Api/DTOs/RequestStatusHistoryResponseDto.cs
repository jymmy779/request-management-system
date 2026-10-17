namespace RequestManagementSystem.Api.DTOs;

public class RequestStatusHistoryResponseDto
{
    public long Id { get; set; }

    public int RequestId { get; set; }

    public string? FromStatus { get; set; }

    public string ToStatus { get; set; } = string.Empty;

    public string ChangedByUserId { get; set; } = string.Empty;

    public string ChangedByFullName { get; set; } = string.Empty;

    public string ChangedByEmail { get; set; } = string.Empty;

    public string? Note { get; set; }

    public DateTime ChangedAt { get; set; }
}
