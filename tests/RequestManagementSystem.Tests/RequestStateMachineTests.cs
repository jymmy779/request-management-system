using RequestManagementSystem.Api.Models;

namespace RequestManagementSystem.Tests;

public class RequestStateMachineTests
{
    [Theory]
    [InlineData(RequestStatuses.Pending, RequestStatuses.Approved, true)]
    [InlineData(RequestStatuses.Pending, RequestStatuses.Rejected, true)]
    [InlineData(RequestStatuses.Pending, RequestStatuses.Cancelled, true)]
    [InlineData(RequestStatuses.Approved, RequestStatuses.Completed, true)]
    [InlineData(RequestStatuses.Pending, RequestStatuses.Completed, false)]
    [InlineData(RequestStatuses.Approved, RequestStatuses.Rejected, false)]
    [InlineData(RequestStatuses.Rejected, RequestStatuses.Approved, false)]
    [InlineData(RequestStatuses.Completed, RequestStatuses.Pending, false)]
    [InlineData(RequestStatuses.Cancelled, RequestStatuses.Pending, false)]
    public void CanTransition_ReturnsExpectedResult(
        string currentStatus,
        string targetStatus,
        bool expectedResult)
    {
        var result = RequestStateMachine.CanTransition(
            currentStatus,
            targetStatus);

        Assert.Equal(expectedResult, result);
    }
}
