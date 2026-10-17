using RequestManagementSystem.Api.DTOs;

namespace RequestManagementSystem.Api.Services;

public enum RequestTransitionOutcome
{
    Success,
    NotFound,
    Forbidden,
    Conflict
}

public class RequestTransitionResult
{
    public RequestTransitionOutcome Outcome { get; set; }

    public RequestResponseDto? Request { get; set; }
}
