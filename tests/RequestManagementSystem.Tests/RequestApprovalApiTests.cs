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

namespace RequestManagementSystem.Tests;

public class RequestApprovalApiTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RequestApprovalApiTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(AppRoles.Manager)]
    [InlineData(AppRoles.Admin)]
    public async Task Approve_WithAuthorizedRole_ReturnsOkAndPersistsAudit(
        string role)
    {
        var scenario = await CreatePendingRequestAsync();
        var client = CreateAuthenticatedClient(
            scenario.Actor,
            role);

        var response = await client.PostAsync(
            $"/api/requests/{scenario.RequestId}/approve",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseDto = await response.Content
            .ReadFromJsonAsync<RequestResponseDto>();

        Assert.NotNull(responseDto);
        Assert.Equal(RequestStatuses.Approved, responseDto.Status);

        using var scope = _factory.Services.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var savedRequest = await dbContext.Requests
            .AsNoTracking()
            .SingleAsync(request => request.Id == scenario.RequestId);

        Assert.Equal(RequestStatuses.Approved, savedRequest.Status);

        var approvalHistory = await dbContext.RequestStatusHistories
            .AsNoTracking()
            .SingleAsync(history =>
                history.RequestId == scenario.RequestId &&
                history.ToStatus == RequestStatuses.Approved);

        Assert.Equal(RequestStatuses.Pending, approvalHistory.FromStatus);
        Assert.Equal(scenario.Actor.Id, approvalHistory.ChangedByUserId);
    }

    [Fact]
    public async Task Approve_WithEmployeeRole_ReturnsForbiddenWithoutChanges()
    {
        var scenario = await CreatePendingRequestAsync();
        var client = CreateAuthenticatedClient(
            scenario.Actor,
            AppRoles.Employee);

        var response = await client.PostAsync(
            $"/api/requests/{scenario.RequestId}/approve",
            content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        await AssertRequestRemainsPendingAsync(scenario.RequestId);
    }

    [Fact]
    public async Task Approve_ByRequester_ReturnsForbiddenWithoutChanges()
    {
        var scenario = await CreatePendingRequestAsync(
            actorIsOwner: true);
        var client = CreateAuthenticatedClient(
            scenario.Actor,
            AppRoles.Manager);

        var response = await client.PostAsync(
            $"/api/requests/{scenario.RequestId}/approve",
            content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        await AssertRequestRemainsPendingAsync(scenario.RequestId);
    }

    [Fact]
    public async Task Approve_WithMissingRequest_ReturnsNotFound()
    {
        var manager = await CreateActorAsync();
        var client = CreateAuthenticatedClient(
            manager,
            AppRoles.Manager);

        var response = await client.PostAsync(
            "/api/requests/999999/approve",
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Approve_Twice_ReturnsConflictWithoutExtraAudit()
    {
        var scenario = await CreatePendingRequestAsync();
        var client = CreateAuthenticatedClient(
            scenario.Actor,
            AppRoles.Manager);

        var firstResponse = await client.PostAsync(
            $"/api/requests/{scenario.RequestId}/approve",
            content: null);
        var secondResponse = await client.PostAsync(
            $"/api/requests/{scenario.RequestId}/approve",
            content: null);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var historyCount = await dbContext.RequestStatusHistories
            .CountAsync(history =>
                history.RequestId == scenario.RequestId);

        Assert.Equal(2, historyCount);
    }

    private async Task<(int RequestId, ApplicationUser Actor)>
        CreatePendingRequestAsync(bool actorIsOwner = false)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var owner = CreateUser("approval-owner@test.local");
        var actor = actorIsOwner
            ? owner
            : CreateUser("approval-actor@test.local");

        dbContext.Users.Add(owner);

        if (!actorIsOwner)
        {
            dbContext.Users.Add(actor);
        }

        await dbContext.SaveChangesAsync();

        var requestService =
            scope.ServiceProvider.GetRequiredService<RequestService>();

        var request = await requestService.CreateAsync(
            CreateValidRequestDto(),
            owner.Id);

        return (request.Id, actor);
    }

    private async Task<ApplicationUser> CreateActorAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var actor = CreateUser("missing-request-manager@test.local");
        dbContext.Users.Add(actor);
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
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        return client;
    }

    private async Task AssertRequestRemainsPendingAsync(int requestId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var savedRequest = await dbContext.Requests
            .AsNoTracking()
            .SingleAsync(request => request.Id == requestId);

        var historyCount = await dbContext.RequestStatusHistories
            .CountAsync(history => history.RequestId == requestId);

        Assert.Equal(RequestStatuses.Pending, savedRequest.Status);
        Assert.Equal(1, historyCount);
    }

    private static async Task ResetDatabaseAsync(
        AppDbContext dbContext)
    {
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    private static ApplicationUser CreateUser(string email)
    {
        return new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };
    }

    private static CreateRequestDto CreateValidRequestDto()
    {
        return new CreateRequestDto
        {
            Title = "Approval endpoint test",
            Description =
                "This request is used to test the approval endpoint.",
            Category = RequestCategories.Equipment
        };
    }
}
