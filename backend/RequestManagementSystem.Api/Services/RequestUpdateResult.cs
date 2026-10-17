using RequestManagementSystem.Api.DTOs;

namespace RequestManagementSystem.Api.Services;

public enum RequestUpdateOutcome
{
    Success,
    NotFound,
    Forbidden,
    Conflict
}

public class RequestUpdateResult
{
    public RequestUpdateOutcome Outcome { get; set; }

    public RequestResponseDto? Request { get; set; }
}
