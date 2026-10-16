namespace RequestManagementSystem.Api.Authorization;

public static class AppPolicies
{
    public const string ManagerOrAdmin = "ManagerOrAdmin";

    public const string CanAccessRequests = "CanAccessRequests";
}
