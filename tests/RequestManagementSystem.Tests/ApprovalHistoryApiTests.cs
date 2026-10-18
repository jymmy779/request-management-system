using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RequestManagementSystem.Api.Authorization;
using RequestManagementSystem.Api.Data;
using RequestManagementSystem.Api.DTOs;
using RequestManagementSystem.Api.Models;
using RequestManagementSystem.Api.Services;
using RequestModel = RequestManagementSystem.Api.Models.Request;

namespace RequestManagementSystem.Tests;

public class ApprovalHistoryApiTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ApprovalHistoryApiTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetApprovalHistory_WithoutAccessToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/api/requests/approval-history");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetApprovalHistory_WithEmployeeRole_ReturnsForbidden()
    {
        var scenario = await CreateScenarioAsync();
        var client = CreateAuthenticatedClient(
            scenario.Actor,
            AppRoles.Employee);

        var response = await client.GetAsync(
            "/api/requests/approval-history");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(AppRoles.Manager)]
    [InlineData(AppRoles.Admin)]
    public async Task GetApprovalHistory_WithAuthorizedRole_ReturnsOnlyActorDecisions(
        string role)
    {
        var scenario = await CreateScenarioAsync();
        var client = CreateAuthenticatedClient(scenario.Actor, role);

        var response = await client.GetAsync(
            "/api/requests/approval-history" +
            "?category=Equipment&page=1&pageSize=1" +
            "&sortBy=title&sortDirection=asc");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<RequestResponseDto>>();

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Single(result.Items);
        Assert.Equal("Actor approved request", result.Items[0].Title);
    }

    private async Task<ApprovalHistoryScenario> CreateScenarioAsync()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var actor = CreateUser(
            "approval-history-actor@test.local",
            "Approval History Actor");
        var otherApprover = CreateUser(
            "approval-history-other@test.local",
            "Other Approver");
        var requester = CreateUser(
            "approval-history-requester@test.local",
            "Request Owner");

        dbContext.Users.AddRange(actor, otherApprover, requester);

        var approvedByActor = CreateRequest(
            "Actor approved request",
            requester.Id,
            RequestStatuses.Completed);
        var rejectedByActor = CreateRequest(
            "Actor rejected request",
            requester.Id,
            RequestStatuses.Rejected);
        var approvedByOther = CreateRequest(
            "Other actor approved request",
            requester.Id,
            RequestStatuses.Approved);
        var pendingRequest = CreateRequest(
            "Pending request",
            requester.Id,
            RequestStatuses.Pending);

        dbContext.Requests.AddRange(
            approvedByActor,
            rejectedByActor,
            approvedByOther,
            pendingRequest);

        await dbContext.SaveChangesAsync();

        dbContext.RequestStatusHistories.AddRange(
            CreateDecisionHistory(
                approvedByActor.Id,
                actor.Id,
                RequestStatuses.Approved),
            CreateDecisionHistory(
                rejectedByActor.Id,
                actor.Id,
                RequestStatuses.Rejected),
            CreateDecisionHistory(
                approvedByOther.Id,
                otherApprover.Id,
                RequestStatuses.Approved));

        await dbContext.SaveChangesAsync();

        return new ApprovalHistoryScenario(actor);
    }

    private HttpClient CreateAuthenticatedClient(
        ApplicationUser actor,
        string role)
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var tokenService =
            scope.ServiceProvider.GetRequiredService<TokenService>();

        var accessToken = tokenService.CreateToken(
            actor,
            new[] { role });

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        return client;
    }

    private static ApplicationUser CreateUser(
        string email,
        string fullName)
    {
        return new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName
        };
    }

    private static RequestModel CreateRequest(
        string title,
        string requesterId,
        string status)
    {
        return new RequestModel
        {
            Title = title,
            Description = $"Description for {title}.",
            Category = RequestCategories.Equipment,
            Priority = RequestPriorities.Medium,
            Status = status,
            RequesterId = requesterId
        };
    }

    private static RequestStatusHistory CreateDecisionHistory(
        int requestId,
        string actorUserId,
        string toStatus)
    {
        return new RequestStatusHistory
        {
            RequestId = requestId,
            FromStatus = RequestStatuses.Pending,
            ToStatus = toStatus,
            ChangedByUserId = actorUserId,
            ChangedAt = DateTime.UtcNow
        };
    }

    private sealed record ApprovalHistoryScenario(
        ApplicationUser Actor);
}
