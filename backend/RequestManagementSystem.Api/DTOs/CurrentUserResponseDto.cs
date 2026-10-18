namespace RequestManagementSystem.Api.DTOs;

public class CurrentUserResponseDto
{
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string? DepartmentName { get; set; }
}
