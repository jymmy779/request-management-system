using RequestManagementSystem.Api.DTOs;

namespace RequestManagementSystem.Api.Services;

public enum UserCreationOutcome
{
    Created,
    Conflict,
    InvalidDepartment,
    ValidationFailed
}

public class UserCreationResult
{
    public UserCreationOutcome Outcome { get; set; }

    public UserResponseDto? User { get; set; }

    public List<string> Errors { get; set; } = [];
}
