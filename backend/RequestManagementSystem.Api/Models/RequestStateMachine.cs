namespace RequestManagementSystem.Api.Models;

public static class RequestStateMachine
{
    public static bool CanTransition(
        string currentStatus,
        string targetStatus)
    {
        return (currentStatus, targetStatus) switch
        {
            (RequestStatuses.Pending, RequestStatuses.Approved) => true,
            (RequestStatuses.Pending, RequestStatuses.Rejected) => true,
            (RequestStatuses.Pending, RequestStatuses.Cancelled) => true,
            (RequestStatuses.Approved, RequestStatuses.Completed) => true,
            _ => false
        };
    }
}
