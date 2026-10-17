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

public class RequestCancellationApiTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RequestCancellationApiTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Cancel_ByRequester_ReturnsOkAndPersistsAudit()
    {
        var scenario = await CreatePendingRequestAsync(
            actorIsOwner: true);
        var client = CreateAuthenticatedClient(
            scenario.Actor,
            AppRoles.Employee);

        var response = await client.PostAsync(
            $"/api/requests/{scenario.RequestId}/cancel",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseDto = await response.Content
            .ReadFromJsonAsync<RequestResponseDto>();

        Assert.NotNull(responseDto);
        Assert.Equal(RequestStatuses.Cancelled, responseDto.Status);

        using var scope = _factory.Services.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var savedRequest = await dbContext.Requests
            .AsNoTracking()
            .SingleAsync(request => request.Id == scenario.RequestId);

        Assert.Equal(RequestStatuses.Cancelled, savedRequest.Status);

        var cancellationHistory = await dbContext.RequestStatusHistories
            .AsNoTracking()
            .SingleAsync(history =>
                history.RequestId == scenario.RequestId &&
                history.ToStatus == RequestStatuses.Cancelled);

        Assert.Equal(RequestStatuses.Pending, cancellationHistory.FromStatus);
        Assert.Equal(scenario.Actor.Id, cancellationHistory.ChangedByUserId);
        Assert.Equal(cancellationHistory.ChangedAt, savedRequest.UpdatedAt);
        Assert.Equal(savedRequest.UpdatedAt, responseDto.UpdatedAt);
    }

    [Theory]
    [InlineData(AppRoles.Employee)]
    [InlineData(AppRoles.Manager)]
    [InlineData(AppRoles.Admin)]
    public async Task Cancel_ByNonRequester_ReturnsForbiddenWithoutChanges(
        string role)
    {
        var scenario = await CreatePendingRequestAsync();
        var client = CreateAuthenticatedClient(
            scenario.Actor,
            role);

        var response = await client.PostAsync(
            $"/api/requests/{scenario.RequestId}/cancel",
            content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        await AssertRequestRemainsPendingAsync(scenario.RequestId);
    }

    [Fact]
    public async Task Cancel_WithMissingRequest_ReturnsNotFound()
    {
        var employee = await CreateActorAsync();
        var client = CreateAuthenticatedClient(
            employee,
            AppRoles.Employee);

        var response = await client.PostAsync(
            "/api/requests/999999/cancel",
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_ApprovedRequest_ReturnsConflictWithoutExtraAudit()
    {
        var scenario = await CreateApprovedRequestAsync();
        var client = CreateAuthenticatedClient(
            scenario.Owner,
            AppRoles.Employee);

        var response = await client.PostAsync(
            $"/api/requests/{scenario.RequestId}/cancel",
            content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

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

    private async Task<(int RequestId, ApplicationUser Actor)>
        CreatePendingRequestAsync(bool actorIsOwner = false)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var owner = CreateUser("cancellation-owner@test.local");
        var actor = actorIsOwner
            ? owner
            : CreateUser("cancellation-actor@test.local");

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

    private async Task<(int RequestId, ApplicationUser Owner)>
        CreateApprovedRequestAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var owner = CreateUser("approved-cancellation-owner@test.local");
        var manager = CreateUser(
            "approved-cancellation-manager@test.local");

        dbContext.Users.AddRange(owner, manager);
        await dbContext.SaveChangesAsync();

        var requestService =
            scope.ServiceProvider.GetRequiredService<RequestService>();

        var request = await requestService.CreateAsync(
            CreateValidRequestDto(),
            owner.Id);

        var approvalResult = await requestService.ApproveAsync(
            request.Id,
            manager.Id);

        Assert.Equal(
            RequestTransitionOutcome.Success,
            approvalResult.Outcome);

        return (request.Id, owner);
    }

    private async Task<ApplicationUser> CreateActorAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var actor = CreateUser("missing-request-employee@test.local");
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
            Title = "Cancellation endpoint test",
            Description =
                "This request is used to test the cancellation endpoint.",
            Category = RequestCategories.Equipment
        };
    }
}
