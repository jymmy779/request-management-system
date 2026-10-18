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

public class RequestApprovalQueueApiTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RequestApprovalQueueApiTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMyApprovals_WithoutAccessToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/api/requests/my-approvals");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyApprovals_WithEmployeeRole_ReturnsForbidden()
    {
        var actor = await CreateScenarioAsync();
        var client = CreateAuthenticatedClient(actor, AppRoles.Employee);

        var response = await client.GetAsync(
            "/api/requests/my-approvals");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(AppRoles.Manager)]
    [InlineData(AppRoles.Admin)]
    public async Task GetMyApprovals_WithAuthorizedRole_ReturnsOnlyEligibleQueue(
        string role)
    {
        var actor = await CreateScenarioAsync();
        var client = CreateAuthenticatedClient(actor, role);

        var response = await client.GetAsync(
            "/api/requests/my-approvals" +
            "?category=Equipment&priority=High" +
            "&page=1&pageSize=1" +
            "&sortBy=title&sortDirection=asc");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<RequestResponseDto>>();

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Single(result.Items);
        Assert.Equal("Alpha eligible request", result.Items[0].Title);
        Assert.Equal(RequestStatuses.Pending, result.Items[0].Status);
        Assert.NotEqual(actor.Id, result.Items[0].RequesterId);
    }

    private async Task<ApplicationUser> CreateScenarioAsync()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var actor = CreateUser(
            "approval-queue-actor@test.local",
            "Approval Queue Actor");
        var owner = CreateUser(
            "approval-queue-owner@test.local",
            "Approval Queue Owner");

        dbContext.Users.AddRange(actor, owner);

        dbContext.Requests.AddRange(
            CreateRequest(
                "Alpha eligible request",
                owner.Id),
            CreateRequest(
                "Beta eligible request",
                owner.Id),
            CreateRequest(
                "Actor own pending request",
                actor.Id),
            CreateRequest(
                "Approved request",
                owner.Id,
                status: RequestStatuses.Approved),
            CreateRequest(
                "Wrong priority request",
                owner.Id,
                priority: RequestPriorities.Low),
            CreateRequest(
                "Wrong category request",
                owner.Id,
                category: RequestCategories.Software));

        await dbContext.SaveChangesAsync();

        return actor;
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
        string status = RequestStatuses.Pending,
        string priority = RequestPriorities.High,
        string category = RequestCategories.Equipment)
    {
        return new RequestModel
        {
            Title = title,
            Description = $"Description for {title}.",
            Category = category,
            Priority = priority,
            Status = status,
            RequesterId = requesterId
        };
    }
}
