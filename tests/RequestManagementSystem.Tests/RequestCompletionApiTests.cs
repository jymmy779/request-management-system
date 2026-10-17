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

public class RequestCompletionApiTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RequestCompletionApiTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(AppRoles.Manager)]
    [InlineData(AppRoles.Admin)]
    public async Task Complete_WithAuthorizedRole_ReturnsOkAndPersistsAudit(
        string role)
    {
        var scenario = await CreateApprovedRequestAsync();
        var client = CreateAuthenticatedClient(
            scenario.Actor,
            role);

        var response = await client.PostAsync(
            $"/api/requests/{scenario.RequestId}/complete",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseDto = await response.Content
            .ReadFromJsonAsync<RequestResponseDto>();

        Assert.NotNull(responseDto);
        Assert.Equal(RequestStatuses.Completed, responseDto.Status);

        using var scope = _factory.Services.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var savedRequest = await dbContext.Requests
            .AsNoTracking()
            .SingleAsync(request => request.Id == scenario.RequestId);

        Assert.Equal(RequestStatuses.Completed, savedRequest.Status);

        var completionHistory = await dbContext.RequestStatusHistories
            .AsNoTracking()
            .SingleAsync(history =>
                history.RequestId == scenario.RequestId &&
                history.ToStatus == RequestStatuses.Completed);

        Assert.Equal(RequestStatuses.Approved, completionHistory.FromStatus);
        Assert.Equal(scenario.Actor.Id, completionHistory.ChangedByUserId);
        Assert.Equal(completionHistory.ChangedAt, savedRequest.UpdatedAt);
        Assert.Equal(savedRequest.UpdatedAt, responseDto.UpdatedAt);
    }

    [Fact]
    public async Task Complete_WithEmployeeRole_ReturnsForbiddenWithoutChanges()
    {
        var scenario = await CreateApprovedRequestAsync(
            useSeparateApprover: true);
        var client = CreateAuthenticatedClient(
            scenario.Actor,
            AppRoles.Employee);

        var response = await client.PostAsync(
            $"/api/requests/{scenario.RequestId}/complete",
            content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var savedRequest = await dbContext.Requests
            .AsNoTracking()
            .SingleAsync(request => request.Id == scenario.RequestId);

        Assert.Equal(RequestStatuses.Approved, savedRequest.Status);
        Assert.Equal(
            2,
            await dbContext.RequestStatusHistories.CountAsync(history =>
                history.RequestId == scenario.RequestId));
    }

    [Fact]
    public async Task Complete_WithMissingRequest_ReturnsNotFound()
    {
        var manager = await CreateActorAsync();
        var client = CreateAuthenticatedClient(
            manager,
            AppRoles.Manager);

        var response = await client.PostAsync(
            "/api/requests/999999/complete",
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Complete_PendingRequest_ReturnsConflictWithoutExtraAudit()
    {
        var scenario = await CreatePendingRequestAsync();
        var client = CreateAuthenticatedClient(
            scenario.Actor,
            AppRoles.Manager);

        var response = await client.PostAsync(
            $"/api/requests/{scenario.RequestId}/complete",
            content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var savedRequest = await dbContext.Requests
            .AsNoTracking()
            .SingleAsync(request => request.Id == scenario.RequestId);

        Assert.Equal(RequestStatuses.Pending, savedRequest.Status);
        Assert.Single(await dbContext.RequestStatusHistories.ToListAsync());
    }

    private async Task<(int RequestId, ApplicationUser Actor)>
        CreateApprovedRequestAsync(bool useSeparateApprover = false)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var owner = CreateUser("completion-owner@test.local");
        var actor = CreateUser("completion-actor@test.local");
        var approver = useSeparateApprover
            ? CreateUser("completion-approver@test.local")
            : actor;

        dbContext.Users.AddRange(owner, actor);

        if (useSeparateApprover)
        {
            dbContext.Users.Add(approver);
        }

        await dbContext.SaveChangesAsync();

        var requestService =
            scope.ServiceProvider.GetRequiredService<RequestService>();

        var request = await requestService.CreateAsync(
            CreateValidRequestDto(),
            owner.Id);

        var approvalResult = await requestService.ApproveAsync(
            request.Id,
            approver.Id);

        Assert.Equal(
            RequestTransitionOutcome.Success,
            approvalResult.Outcome);

        return (request.Id, actor);
    }

    private async Task<(int RequestId, ApplicationUser Actor)>
        CreatePendingRequestAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var owner = CreateUser("pending-completion-owner@test.local");
        var actor = CreateUser("pending-completion-actor@test.local");

        dbContext.Users.AddRange(owner, actor);
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
            Title = "Completion endpoint test",
            Description =
                "This request is used to test the completion endpoint.",
            Category = RequestCategories.Equipment
        };
    }
}
