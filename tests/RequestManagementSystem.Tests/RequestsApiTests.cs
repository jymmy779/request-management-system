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
            EmailConfirmed = true,
            FullName = "Current User"
        };

        var otherUser = new ApplicationUser
        {
            UserName = "other-user@test.local",
            Email = "other-user@test.local",
            EmailConfirmed = true,
            FullName = "Other User"
        };

        dbContext.Users.AddRange(
            currentUser,
            otherUser);

        dbContext.Requests.AddRange(
            new RequestModel
            {
                Title = "Request a monitor",
                Description = "Need a second monitor.",
                Status = RequestStatuses.Pending,
                RequesterId = currentUser.Id,
                CreatedAt = new DateTime(
                    2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new RequestModel
            {
                Title = "Request a keyboard",
                Description = "Current keyboard is broken.",
                Status = RequestStatuses.Approved,
                RequesterId = currentUser.Id,
                CreatedAt = new DateTime(
                    2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
            },
            new RequestModel
            {
                Title = "Other employee request",
                Description = "This request must not be visible.",
                Status = RequestStatuses.Pending,
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

        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<RequestResponseDto>>();

        Assert.NotNull(result);
        Assert.Equal(expectedCount, result.TotalCount);
        Assert.Equal(expectedCount, result.Items.Count);

        Assert.Contains(
            result.Items,
            request => request.Title == "Request a monitor"
                && request.RequesterFullName == currentUser.FullName
                && request.RequesterEmail == currentUser.Email);

        var containsOtherEmployeeRequest = result.Items.Any(
            request => request.Title == "Other employee request");

        Assert.Equal(
            canSeeOtherEmployeeRequest,
            containsOtherEmployeeRequest);

    }

    [Fact]
    public async Task GetAll_AppliesSearchFiltersAndSorting()
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var manager = new ApplicationUser
        {
            UserName = "query-manager@test.local",
            Email = "query-manager@test.local",
            EmailConfirmed = true,
            FullName = "Query Manager"
        };

        dbContext.Users.Add(manager);

        dbContext.Requests.AddRange(
            CreateRequest(
                "Beta monitor",
                RequestCategories.Equipment,
                RequestStatuses.Pending,
                manager.Id),
            CreateRequest(
                "Alpha monitor",
                RequestCategories.Equipment,
                RequestStatuses.Pending,
                manager.Id),
            CreateRequest(
                "Approved monitor",
                RequestCategories.Equipment,
                RequestStatuses.Approved,
                manager.Id),
            CreateRequest(
                "Software monitor",
                RequestCategories.Software,
                RequestStatuses.Pending,
                manager.Id),
            CreateRequest(
                "Unrelated equipment",
                RequestCategories.Equipment,
                RequestStatuses.Pending,
                manager.Id));

        await dbContext.SaveChangesAsync();

        var tokenService =
            scope.ServiceProvider.GetRequiredService<TokenService>();

        var accessToken = tokenService.CreateToken(
            manager,
            new[] { AppRoles.Manager });

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        var response = await client.GetAsync(
            "/api/requests?search=monitor&status=Pending" +
            "&category=Equipment&sortBy=title&sortDirection=asc");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<RequestResponseDto>>();

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal("Alpha monitor", result.Items[0].Title);
        Assert.Equal("Beta monitor", result.Items[1].Title);
    }

    [Fact]
    public async Task GetAll_ReturnsRequestedPageWithMetadata()
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var manager = new ApplicationUser
        {
            UserName = "pagination-manager@test.local",
            Email = "pagination-manager@test.local",
            EmailConfirmed = true,
            FullName = "Pagination Manager"
        };

        dbContext.Users.Add(manager);

        foreach (var title in new[]
                 {
                     "Request A",
                     "Request B",
                     "Request C",
                     "Request D",
                     "Request E"
                 })
        {
            dbContext.Requests.Add(
                CreateRequest(
                    title,
                    RequestCategories.Other,
                    RequestStatuses.Pending,
                    manager.Id));
        }

        await dbContext.SaveChangesAsync();

        var tokenService =
            scope.ServiceProvider.GetRequiredService<TokenService>();

        var accessToken = tokenService.CreateToken(
            manager,
            new[] { AppRoles.Manager });

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        var response = await client.GetAsync(
            "/api/requests?page=2&pageSize=2" +
            "&sortBy=title&sortDirection=asc");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<RequestResponseDto>>();

        Assert.NotNull(result);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Request C", result.Items[0].Title);
        Assert.Equal("Request D", result.Items[1].Title);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=101")]
    [InlineData("status=Unknown")]
    [InlineData("category=Unknown")]
    [InlineData("sortBy=unknown")]
    [InlineData("sortDirection=unknown")]
    public async Task GetAll_WithInvalidQuery_ReturnsBadRequest(
        string queryString)
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();

        var tokenService =
            scope.ServiceProvider.GetRequiredService<TokenService>();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "query-validation@test.local",
            Email = "query-validation@test.local"
        };

        var accessToken = tokenService.CreateToken(
            user,
            new[] { AppRoles.Employee });

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        var response = await client.GetAsync(
            $"/api/requests?{queryString}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithRolelessAccessToken_ReturnsForbidden()
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();

        var tokenService =
            scope.ServiceProvider.GetRequiredService<TokenService>();

        var rolelessUser = new ApplicationUser
        {
            UserName = "roleless@test.local",
            Email = "roleless@test.local",
            EmailConfirmed = true
        };

        var accessToken = tokenService.CreateToken(
            rolelessUser,
            Array.Empty<string>());

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        var response = await client.GetAsync("/api/requests");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidInput_ReturnsCreatedAndPersistsOwnedRequest()
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var department = new Department
        {
            Code = "IT",
            Name = "Information Technology"
        };

        var employee = new ApplicationUser
        {
            UserName = "employee@test.local",
            Email = "employee@test.local",
            EmailConfirmed = true,
            FullName = "Demo Employee",
            Department = department
        };

        dbContext.Users.Add(employee);
        await dbContext.SaveChangesAsync();

        var tokenService =
            scope.ServiceProvider.GetRequiredService<TokenService>();

        var accessToken = tokenService.CreateToken(
            employee,
            new[] { AppRoles.Employee });

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        var dto = new CreateRequestDto
        {
            Title = "Request a new monitor",
            Description = "A second monitor is needed for development work.",
            Category = RequestCategories.Equipment,
            Priority = RequestPriorities.High,
            Quantity = 3,
            EstimatedCost = 18_000_000m
        };

        var response = await client.PostAsJsonAsync(
            "/api/requests",
            dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdRequest =
            await response.Content.ReadFromJsonAsync<RequestResponseDto>();

        Assert.NotNull(createdRequest);
        Assert.Equal(employee.Id, createdRequest.RequesterId);
        Assert.Equal(employee.FullName, createdRequest.RequesterFullName);
        Assert.Equal(employee.Email, createdRequest.RequesterEmail);
        Assert.Equal(department.Name, createdRequest.RequesterDepartmentName);
        Assert.Equal(RequestStatuses.Pending, createdRequest.Status);
        Assert.Equal(RequestCategories.Equipment, createdRequest.Category);
        Assert.Equal(RequestPriorities.High, createdRequest.Priority);
        Assert.Equal(3, createdRequest.Quantity);
        Assert.Equal(18_000_000m, createdRequest.EstimatedCost);
        Assert.Null(createdRequest.UpdatedAt);
        Assert.Equal(
            $"/api/requests/{createdRequest.Id}",
            response.Headers.Location?.ToString());

        var savedRequest = await dbContext.Requests
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(createdRequest.Id, savedRequest.Id);
        Assert.Equal(employee.Id, savedRequest.RequesterId);
        Assert.Equal(dto.Title, savedRequest.Title);
        Assert.Equal(RequestPriorities.High, savedRequest.Priority);
        Assert.Equal(3, savedRequest.Quantity);
        Assert.Equal(18_000_000m, savedRequest.EstimatedCost);
        Assert.Null(savedRequest.UpdatedAt);

        var savedHistory = await dbContext.RequestStatusHistories
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(savedRequest.Id, savedHistory.RequestId);
        Assert.Null(savedHistory.FromStatus);
        Assert.Equal(RequestStatuses.Pending, savedHistory.ToStatus);
        Assert.Equal(employee.Id, savedHistory.ChangedByUserId);
        Assert.Equal(savedRequest.CreatedAt, savedHistory.ChangedAt);
    }

    [Theory]
    [InlineData(
        "Bad",
        "This description is long enough.",
        RequestCategories.Equipment,
        RequestPriorities.Medium,
        1,
        1000)]
    [InlineData(
        "A valid title",
        "Too short",
        RequestCategories.Software,
        RequestPriorities.Medium,
        1,
        1000)]
    [InlineData(
        "A valid title",
        "This description is long enough.",
        "Invalid category",
        RequestPriorities.Medium,
        1,
        1000)]
    [InlineData(
        "A valid title",
        "This description is long enough.",
        RequestCategories.Other,
        "Urgent",
        1,
        1000)]
    [InlineData(
        "A valid title",
        "This description is long enough.",
        RequestCategories.Equipment,
        RequestPriorities.Medium,
        0,
        1000)]
    [InlineData(
        "A valid title",
        "This description is long enough.",
        RequestCategories.Equipment,
        RequestPriorities.Medium,
        1,
        -1)]
    public async Task Create_WithInvalidInput_ReturnsBadRequestAndDoesNotPersist(
        string title,
        string description,
        string category,
        string priority,
        int quantity,
        int estimatedCost)
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var employee = new ApplicationUser
        {
            UserName = "employee@test.local",
            Email = "employee@test.local",
            EmailConfirmed = true
        };

        dbContext.Users.Add(employee);
        await dbContext.SaveChangesAsync();

        var tokenService =
            scope.ServiceProvider.GetRequiredService<TokenService>();

        var accessToken = tokenService.CreateToken(
            employee,
            new[] { AppRoles.Employee });

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        var dto = new CreateRequestDto
        {
            Title = title,
            Description = description,
            Category = category,
            Priority = priority,
            Quantity = quantity,
            EstimatedCost = estimatedCost
        };

        var response = await client.PostAsJsonAsync(
            "/api/requests",
            dto);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Empty(
            await dbContext.Requests
                .AsNoTracking()
                .ToListAsync());

        Assert.Empty(
            await dbContext.RequestStatusHistories
                .AsNoTracking()
                .ToListAsync());
    }

    [Theory]
    [InlineData(AppRoles.Employee, true, HttpStatusCode.OK)]
    [InlineData(AppRoles.Employee, false, HttpStatusCode.NotFound)]
    [InlineData(AppRoles.Manager, false, HttpStatusCode.OK)]
    [InlineData(AppRoles.Admin, false, HttpStatusCode.OK)]
    public async Task GetById_EnforcesOwnershipAndRole(
        string role,
        bool isOwner,
        HttpStatusCode expectedStatusCode)
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var department = new Department
        {
            Code = "IT",
            Name = "Information Technology"
        };

        var owner = new ApplicationUser
        {
            UserName = "owner@test.local",
            Email = "owner@test.local",
            EmailConfirmed = true,
            FullName = "Request Owner",
            Department = department
        };

        var caller = isOwner
            ? owner
            : new ApplicationUser
            {
                UserName = "caller@test.local",
                Email = "caller@test.local",
                EmailConfirmed = true
            };

        dbContext.Users.Add(owner);

        if (!isOwner)
        {
            dbContext.Users.Add(caller);
        }

        var request = new RequestModel
        {
            Title = "Request detail test",
            Description = "This request is used to test detail access.",
            Category = RequestCategories.Software,
            RequesterId = owner.Id
        };

        dbContext.Requests.Add(request);
        await dbContext.SaveChangesAsync();

        var tokenService =
            scope.ServiceProvider.GetRequiredService<TokenService>();

        var accessToken = tokenService.CreateToken(
            caller,
            new[] { role });

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        var response = await client.GetAsync(
            $"/api/requests/{request.Id}");

        Assert.Equal(expectedStatusCode, response.StatusCode);

        if (expectedStatusCode == HttpStatusCode.OK)
        {
            var responseDto = await response.Content
                .ReadFromJsonAsync<RequestResponseDto>();

            Assert.NotNull(responseDto);
            Assert.Equal(request.Id, responseDto.Id);
            Assert.Equal(owner.Id, responseDto.RequesterId);
            Assert.Equal(owner.FullName, responseDto.RequesterFullName);
            Assert.Equal(owner.Email, responseDto.RequesterEmail);
            Assert.Equal(department.Name, responseDto.RequesterDepartmentName);
        }
    }

    [Fact]
    public async Task GetById_WithMissingId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var employee = new ApplicationUser
        {
            UserName = "employee@test.local",
            Email = "employee@test.local",
            EmailConfirmed = true
        };

        dbContext.Users.Add(employee);
        await dbContext.SaveChangesAsync();

        var tokenService =
            scope.ServiceProvider.GetRequiredService<TokenService>();

        var accessToken = tokenService.CreateToken(
            employee,
            new[] { AppRoles.Employee });

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        var response = await client.GetAsync(
            "/api/requests/999999");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Theory]
    [InlineData(AppRoles.Employee, true, HttpStatusCode.OK)]
    [InlineData(AppRoles.Employee, false, HttpStatusCode.NotFound)]
    [InlineData(AppRoles.Manager, false, HttpStatusCode.OK)]
    [InlineData(AppRoles.Admin, false, HttpStatusCode.OK)]
    public async Task GetHistory_EnforcesAccessAndReturnsChronologicalEntries(
        string role,
        bool isOwner,
        HttpStatusCode expectedStatusCode)
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var owner = new ApplicationUser
        {
            UserName = "history-owner@test.local",
            Email = "history-owner@test.local",
            EmailConfirmed = true,
            FullName = "History Owner"
        };

        var caller = isOwner
            ? owner
            : new ApplicationUser
            {
                UserName = "history-caller@test.local",
                Email = "history-caller@test.local",
                EmailConfirmed = true
            };

        dbContext.Users.Add(owner);

        if (!isOwner)
        {
            dbContext.Users.Add(caller);
        }

        var request = new RequestModel
        {
            Title = "History test request",
            Description = "This request has multiple history entries.",
            Category = RequestCategories.Other,
            Status = RequestStatuses.Approved,
            RequesterId = owner.Id
        };

        request.StatusHistory.Add(new RequestStatusHistory
        {
            FromStatus = RequestStatuses.Pending,
            ToStatus = RequestStatuses.Approved,
            ChangedByUserId = owner.Id,
            ChangedAt = new DateTime(
                2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        });

        request.StatusHistory.Add(new RequestStatusHistory
        {
            FromStatus = null,
            ToStatus = RequestStatuses.Pending,
            ChangedByUserId = owner.Id,
            ChangedAt = new DateTime(
                2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        dbContext.Requests.Add(request);
        await dbContext.SaveChangesAsync();

        var tokenService =
            scope.ServiceProvider.GetRequiredService<TokenService>();

        var accessToken = tokenService.CreateToken(
            caller,
            new[] { role });

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        var response = await client.GetAsync(
            $"/api/requests/{request.Id}/history");

        Assert.Equal(expectedStatusCode, response.StatusCode);

        if (expectedStatusCode == HttpStatusCode.OK)
        {
            var history = await response.Content
                .ReadFromJsonAsync<List<RequestStatusHistoryResponseDto>>();

            Assert.NotNull(history);
            Assert.Equal(2, history.Count);
            Assert.Equal(RequestStatuses.Pending, history[0].ToStatus);
            Assert.Equal(RequestStatuses.Approved, history[1].ToStatus);
            Assert.All(
                history,
                entry =>
                {
                    Assert.Equal(owner.Id, entry.ChangedByUserId);
                    Assert.Equal(owner.FullName, entry.ChangedByFullName);
                    Assert.Equal(owner.Email, entry.ChangedByEmail);
                });
        }
    }

    [Fact]
    public async Task GetHistory_WithMissingId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();

        var tokenService =
            scope.ServiceProvider.GetRequiredService<TokenService>();

        var employee = new ApplicationUser
        {
            UserName = "history-employee@test.local",
            Email = "history-employee@test.local",
            EmailConfirmed = true
        };

        var accessToken = tokenService.CreateToken(
            employee,
            new[] { AppRoles.Employee });

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        var response = await client.GetAsync(
            "/api/requests/999999/history");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    private static RequestModel CreateRequest(
        string title,
        string category,
        string status,
        string requesterId)
    {
        return new RequestModel
        {
            Title = title,
            Description = $"Description for {title}.",
            Category = category,
            Status = status,
            RequesterId = requesterId
        };
    }
}
