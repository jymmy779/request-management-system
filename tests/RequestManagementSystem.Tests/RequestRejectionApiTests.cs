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

public class RequestRejectionApiTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RequestRejectionApiTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public static IEnumerable<object[]> InvalidReasons()
    {
        yield return new object[] { string.Empty };
        yield return new object[] { "1234" };
        yield return new object[] { new string('a', 501) };
    }

    [Theory]
    [InlineData(AppRoles.Manager)]
    [InlineData(AppRoles.Admin)]
    public async Task Reject_WithAuthorizedRole_ReturnsOkAndPersistsAudit(
        string role)
    {
        var scenario = await CreatePendingRequestAsync();
        var client = CreateAuthenticatedClient(
            scenario.Actor,
            role);
        var dto = CreateValidRejectDto();

        var response = await client.PostAsJsonAsync(
            $"/api/requests/{scenario.RequestId}/reject",
            dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseDto = await response.Content
            .ReadFromJsonAsync<RequestResponseDto>();

        Assert.NotNull(responseDto);
        Assert.Equal(RequestStatuses.Rejected, responseDto.Status);

        using var scope = _factory.Services.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var savedRequest = await dbContext.Requests
            .AsNoTracking()
            .SingleAsync(request => request.Id == scenario.RequestId);

        Assert.Equal(RequestStatuses.Rejected, savedRequest.Status);

        var rejectionHistory = await dbContext.RequestStatusHistories
            .AsNoTracking()
            .SingleAsync(history =>
                history.RequestId == scenario.RequestId &&
                history.ToStatus == RequestStatuses.Rejected);

        Assert.Equal(RequestStatuses.Pending, rejectionHistory.FromStatus);
        Assert.Equal(scenario.Actor.Id, rejectionHistory.ChangedByUserId);
        Assert.Equal(dto.Reason, rejectionHistory.Note);
        Assert.Equal(rejectionHistory.ChangedAt, savedRequest.UpdatedAt);
        Assert.Equal(savedRequest.UpdatedAt, responseDto.UpdatedAt);
    }

    [Theory]
    [MemberData(nameof(InvalidReasons))]
    public async Task Reject_WithInvalidReason_ReturnsBadRequest(
        string reason)
    {
        var scenario = await CreatePendingRequestAsync();
        var client = CreateAuthenticatedClient(
            scenario.Actor,
            AppRoles.Manager);

        var response = await client.PostAsJsonAsync(
            $"/api/requests/{scenario.RequestId}/reject",
            new RejectRequestDto { Reason = reason });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await AssertRequestRemainsPendingAsync(scenario.RequestId);
    }

    [Fact]
    public async Task Reject_WithEmployeeRole_ReturnsForbiddenWithoutChanges()
    {
        var scenario = await CreatePendingRequestAsync();
        var client = CreateAuthenticatedClient(
            scenario.Actor,
            AppRoles.Employee);

        var response = await client.PostAsJsonAsync(
            $"/api/requests/{scenario.RequestId}/reject",
            CreateValidRejectDto());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        await AssertRequestRemainsPendingAsync(scenario.RequestId);
    }

    [Fact]
    public async Task Reject_ByRequester_ReturnsForbiddenWithoutChanges()
    {
        var scenario = await CreatePendingRequestAsync(
            actorIsOwner: true);
        var client = CreateAuthenticatedClient(
            scenario.Actor,
            AppRoles.Manager);

        var response = await client.PostAsJsonAsync(
            $"/api/requests/{scenario.RequestId}/reject",
            CreateValidRejectDto());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        await AssertRequestRemainsPendingAsync(scenario.RequestId);
    }

    [Fact]
    public async Task Reject_WithMissingRequest_ReturnsNotFound()
    {
        var manager = await CreateActorAsync();
        var client = CreateAuthenticatedClient(
            manager,
            AppRoles.Manager);

        var response = await client.PostAsJsonAsync(
            "/api/requests/999999/reject",
            CreateValidRejectDto());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Reject_ApprovedRequest_ReturnsConflictWithoutExtraAudit()
    {
        var scenario = await CreatePendingRequestAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var requestService =
                scope.ServiceProvider.GetRequiredService<RequestService>();

            var approvalResult = await requestService.ApproveAsync(
                scenario.RequestId,
                scenario.Actor.Id);

            Assert.Equal(
                RequestTransitionOutcome.Success,
                approvalResult.Outcome);
        }

        var client = CreateAuthenticatedClient(
            scenario.Actor,
            AppRoles.Manager);

        var response = await client.PostAsJsonAsync(
            $"/api/requests/{scenario.RequestId}/reject",
            CreateValidRejectDto());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var verificationScope = _factory.Services.CreateScope();
        var dbContext = verificationScope.ServiceProvider
            .GetRequiredService<AppDbContext>();

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

        var owner = CreateUser("rejection-owner@test.local");
        var actor = actorIsOwner
            ? owner
            : CreateUser("rejection-actor@test.local");

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
            Title = "Rejection endpoint test",
            Description =
                "This request is used to test the rejection endpoint.",
            Category = RequestCategories.Equipment
        };
    }

    private static RejectRequestDto CreateValidRejectDto()
    {
        return new RejectRequestDto
        {
            Reason = "The request exceeds the current budget."
        };
    }
}
