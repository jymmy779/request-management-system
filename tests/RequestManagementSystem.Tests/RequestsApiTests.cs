using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RequestManagementSystem.Api.Data;
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
    public async Task GetAll_WhenRequestsExist_ReturnsRequests()
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureCreatedAsync();

        dbContext.Requests.AddRange(
            new RequestModel
            {
                Title = "Request a monitor",
                Description = "Need a second monitor.",
                Status = "Pending",
                CreatedAt = new DateTime(
                    2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new RequestModel
            {
                Title = "Request a keyboard",
                Description = "Current keyboard is broken.",
                Status = "Approved",
                CreatedAt = new DateTime(
                    2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
            });

        await dbContext.SaveChangesAsync();

        var response = await client.GetAsync("/api/requests");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var requests =
            await response.Content.ReadFromJsonAsync<List<RequestModel>>();

        Assert.NotNull(requests);
        Assert.Equal(2, requests.Count);
        Assert.Contains(
            requests,
            request => request.Title == "Request a monitor");
    }
}