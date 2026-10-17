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

public class RequestUpdateTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RequestUpdateTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpdateAsync_ByOwnerWhilePending_UpdatesEditableFields()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var owner = CreateUser("update-owner@test.local");
        dbContext.Users.Add(owner);
        await dbContext.SaveChangesAsync();

        var requestService =
            scope.ServiceProvider.GetRequiredService<RequestService>();
        var created = await requestService.CreateAsync(
            CreateRequestDto(),
            owner.Id);

        Assert.Null(created.UpdatedAt);

        var result = await requestService.UpdateAsync(
            created.Id,
            owner.Id,
            CreateUpdateDto());

        Assert.Equal(RequestUpdateOutcome.Success, result.Outcome);
        Assert.NotNull(result.Request);
        Assert.Equal("Updated request title", result.Request.Title);
        Assert.Equal(RequestCategories.Software, result.Request.Category);
        Assert.Equal(RequestPriorities.High, result.Request.Priority);
        Assert.Equal(5, result.Request.Quantity);
        Assert.Equal(25_000_000m, result.Request.EstimatedCost);
        Assert.NotNull(result.Request.UpdatedAt);
        Assert.True(result.Request.UpdatedAt > created.CreatedAt);

        var savedRequest = await dbContext.Requests
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal("Updated request title", savedRequest.Title);
        Assert.Equal(
            "Updated request description for the owner.",
            savedRequest.Description);
        Assert.Equal(RequestCategories.Software, savedRequest.Category);
        Assert.Equal(RequestPriorities.High, savedRequest.Priority);
        Assert.Equal(5, savedRequest.Quantity);
        Assert.Equal(25_000_000m, savedRequest.EstimatedCost);
        Assert.Equal(RequestStatuses.Pending, savedRequest.Status);
        Assert.Equal(result.Request.UpdatedAt, savedRequest.UpdatedAt);
        Assert.Single(await dbContext.RequestStatusHistories.ToListAsync());
    }

    [Fact]
    public async Task UpdateAsync_ByNonOwner_ReturnsForbiddenWithoutChanges()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var owner = CreateUser("update-owner@test.local");
        var otherUser = CreateUser("update-other@test.local");
        dbContext.Users.AddRange(owner, otherUser);
        await dbContext.SaveChangesAsync();

        var requestService =
            scope.ServiceProvider.GetRequiredService<RequestService>();
        var created = await requestService.CreateAsync(
            CreateRequestDto(),
            owner.Id);

        var result = await requestService.UpdateAsync(
            created.Id,
            otherUser.Id,
            CreateUpdateDto());

        Assert.Equal(RequestUpdateOutcome.Forbidden, result.Outcome);
        Assert.Null(result.Request);

        var savedRequest = await dbContext.Requests
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal("Original request title", savedRequest.Title);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotPending_ReturnsConflictWithoutChanges()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var owner = CreateUser("update-owner@test.local");
        dbContext.Users.Add(owner);
        await dbContext.SaveChangesAsync();

        var requestService =
            scope.ServiceProvider.GetRequiredService<RequestService>();
        var created = await requestService.CreateAsync(
            CreateRequestDto(),
            owner.Id);
        var request = await dbContext.Requests.SingleAsync();
        request.Status = RequestStatuses.Approved;
        await dbContext.SaveChangesAsync();

        var result = await requestService.UpdateAsync(
            created.Id,
            owner.Id,
            CreateUpdateDto());

        Assert.Equal(RequestUpdateOutcome.Conflict, result.Outcome);
        Assert.Null(result.Request);
        Assert.Equal("Original request title", request.Title);
    }

    [Fact]
    public async Task UpdateEndpoint_ByOwner_ReturnsOkAndPersistsChanges()
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var owner = CreateUser("update-owner@test.local");
        dbContext.Users.Add(owner);
        await dbContext.SaveChangesAsync();

        var requestService =
            scope.ServiceProvider.GetRequiredService<RequestService>();
        var created = await requestService.CreateAsync(
            CreateRequestDto(),
            owner.Id);

        Authenticate(client, scope.ServiceProvider, owner, AppRoles.Employee);

        var response = await client.PutAsJsonAsync(
            $"/api/requests/{created.Id}",
            CreateUpdateDto());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseDto = await response.Content
            .ReadFromJsonAsync<RequestResponseDto>();

        Assert.NotNull(responseDto);
        Assert.Equal("Updated request title", responseDto.Title);
        Assert.Equal(RequestCategories.Software, responseDto.Category);
        Assert.Equal(RequestPriorities.High, responseDto.Priority);
        Assert.Equal(5, responseDto.Quantity);
        Assert.Equal(25_000_000m, responseDto.EstimatedCost);
        Assert.NotNull(responseDto.UpdatedAt);
    }

    [Fact]
    public async Task UpdateEndpoint_ByNonOwnerManager_ReturnsForbidden()
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var owner = CreateUser("update-owner@test.local");
        var manager = CreateUser("update-manager@test.local");
        dbContext.Users.AddRange(owner, manager);
        await dbContext.SaveChangesAsync();

        var requestService =
            scope.ServiceProvider.GetRequiredService<RequestService>();
        var created = await requestService.CreateAsync(
            CreateRequestDto(),
            owner.Id);

        Authenticate(client, scope.ServiceProvider, manager, AppRoles.Manager);

        var response = await client.PutAsJsonAsync(
            $"/api/requests/{created.Id}",
            CreateUpdateDto());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEndpoint_WithInvalidInput_ReturnsBadRequestWithoutChanges()
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var owner = CreateUser("update-owner@test.local");
        dbContext.Users.Add(owner);
        await dbContext.SaveChangesAsync();

        var requestService =
            scope.ServiceProvider.GetRequiredService<RequestService>();
        var created = await requestService.CreateAsync(
            CreateRequestDto(),
            owner.Id);

        Authenticate(client, scope.ServiceProvider, owner, AppRoles.Employee);

        var invalidDto = new UpdateRequestDto
        {
            Title = "Bad",
            Description = "Too short",
            Category = "Unknown"
        };

        var response = await client.PutAsJsonAsync(
            $"/api/requests/{created.Id}",
            invalidDto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var savedRequest = await dbContext.Requests
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal("Original request title", savedRequest.Title);
    }

    [Fact]
    public async Task UpdateEndpoint_WithMissingRequest_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var owner = CreateUser("update-owner@test.local");

        Authenticate(client, scope.ServiceProvider, owner, AppRoles.Employee);

        var response = await client.PutAsJsonAsync(
            "/api/requests/999999",
            CreateUpdateDto());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static void Authenticate(
        HttpClient client,
        IServiceProvider serviceProvider,
        ApplicationUser user,
        string role)
    {
        var tokenService =
            serviceProvider.GetRequiredService<TokenService>();
        var accessToken = tokenService.CreateToken(user, new[] { role });

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private static async Task ResetDatabaseAsync(AppDbContext dbContext)
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
            EmailConfirmed = true,
            FullName = "Update Test User"
        };
    }

    private static CreateRequestDto CreateRequestDto()
    {
        return new CreateRequestDto
        {
            Title = "Original request title",
            Description = "Original request description for update tests.",
            Category = RequestCategories.Equipment,
            Priority = RequestPriorities.Medium,
            Quantity = 2,
            EstimatedCost = 20_000_000m
        };
    }

    private static UpdateRequestDto CreateUpdateDto()
    {
        return new UpdateRequestDto
        {
            Title = "Updated request title",
            Description = "Updated request description for the owner.",
            Category = RequestCategories.Software,
            Priority = RequestPriorities.High,
            Quantity = 5,
            EstimatedCost = 25_000_000m
        };
    }
}
