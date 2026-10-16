using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RequestManagementSystem.Api.Authorization;
using RequestManagementSystem.Api.Data;
using RequestManagementSystem.Api.Models;
using RequestManagementSystem.Api.Services;
using RequestModel = RequestManagementSystem.Api.Models.Request;

namespace RequestManagementSystem.Tests;

public class RequestsApiTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RequestsApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_WithoutAccessToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/requests");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Theory]
    [InlineData(AppRoles.Employee, 2, false)]
    [InlineData(AppRoles.Manager, 3, true)]
    [InlineData(AppRoles.Admin, 3, true)]
    public async Task GetAll_FiltersRequestsBasedOnRole(string role, int expectedCount, bool canSeeOtherEmployeeRequest)
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureDeletedAsync();

        await dbContext.Database.EnsureCreatedAsync();

        var currentUser = new ApplicationUser
        {
            UserName = "current-user@test.local",
            Email = "current-user@test.local",
            EmailConfirmed = true
        };

        var otherUser = new ApplicationUser
        {
            UserName = "other-user@test.local",
            Email = "other-user@test.local",
            EmailConfirmed = true
        };

        dbContext.Users.AddRange(
            currentUser,
            otherUser);

        dbContext.Requests.AddRange(
            new RequestModel
            {
                Title = "Request a monitor",
                Description = "Need a second monitor.",
                Status = "Pending",
                RequesterId = currentUser.Id,
                CreatedAt = new DateTime(
                    2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new RequestModel
            {
                Title = "Request a keyboard",
                Description = "Current keyboard is broken.",
                Status = "Approved",
                RequesterId = currentUser.Id,
                CreatedAt = new DateTime(
                    2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
            },
            new RequestModel
            {
                Title = "Other employee request",
                Description = "This request must not be visible.",
                Status = "Pending",
                RequesterId = otherUser.Id,
                CreatedAt = new DateTime(
                2026, 1, 3, 0, 0, 0, DateTimeKind.Utc)
            });

        await dbContext.SaveChangesAsync();

        var tokenService =
            scope.ServiceProvider.GetRequiredService<TokenService>();

        var accessToken = tokenService.CreateToken(
            currentUser,
            new[] { role });

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        var response = await client.GetAsync("/api/requests");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var requests =
            await response.Content.ReadFromJsonAsync<List<RequestModel>>();

        Assert.NotNull(requests);
        Assert.Equal(expectedCount, requests.Count);

        Assert.Contains(
            requests,
            request => request.Title == "Request a monitor");

        var containsOtherEmployeeRequest = requests.Any(
            request => request.Title == "Other employee request");

        Assert.Equal(
            canSeeOtherEmployeeRequest,
            containsOtherEmployeeRequest);

    }
}