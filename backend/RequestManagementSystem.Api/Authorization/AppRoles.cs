namespace RequestManagementSystem.Api.Authorization;

public static class AppRoles
{
    public const string ValidationPattern = "^(Employee|Manager|Admin)$";

    public const string Employee = "Employee";
    public const string Manager = "Manager";
    public const string Admin = "Admin";
}
