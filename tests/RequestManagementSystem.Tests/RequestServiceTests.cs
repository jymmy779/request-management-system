using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RequestManagementSystem.Api.Data;
using RequestManagementSystem.Api.DTOs;
using RequestManagementSystem.Api.Models;
using RequestManagementSystem.Api.Services;

namespace RequestManagementSystem.Tests;

public class RequestServiceTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RequestServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ApproveAsync_PendingRequest_ApprovesAndWritesHistory()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var owner = CreateUser("approve-owner@test.local");
        var manager = CreateUser("approve-manager@test.local");

        dbContext.Users.AddRange(owner, manager);
        await dbContext.SaveChangesAsync();

        var requestService =
            scope.ServiceProvider.GetRequiredService<RequestService>();

        var createdRequest = await requestService.CreateAsync(
            CreateValidRequestDto(),
            owner.Id);

        var result = await requestService.ApproveAsync(
            createdRequest.Id,
            manager.Id);

        Assert.Equal(RequestTransitionOutcome.Success, result.Outcome);
        Assert.NotNull(result.Request);
        Assert.Equal(RequestStatuses.Approved, result.Request.Status);

        var savedRequest = await dbContext.Requests
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(RequestStatuses.Approved, savedRequest.Status);

        var history = await dbContext.RequestStatusHistories
            .AsNoTracking()
            .OrderBy(entry => entry.ChangedAt)
            .ThenBy(entry => entry.Id)
            .ToListAsync();

        Assert.Equal(2, history.Count);
        Assert.Equal(RequestStatuses.Pending, history[1].FromStatus);
        Assert.Equal(RequestStatuses.Approved, history[1].ToStatus);
        Assert.Equal(manager.Id, history[1].ChangedByUserId);
    }

    [Fact]
    public async Task ApproveAsync_ByRequester_ReturnsForbiddenWithoutChanges()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var owner = CreateUser("self-approve@test.local");

        dbContext.Users.Add(owner);
        await dbContext.SaveChangesAsync();

        var requestService =
            scope.ServiceProvider.GetRequiredService<RequestService>();

        var createdRequest = await requestService.CreateAsync(
            CreateValidRequestDto(),
            owner.Id);

        var result = await requestService.ApproveAsync(
            createdRequest.Id,
            owner.Id);

        Assert.Equal(RequestTransitionOutcome.Forbidden, result.Outcome);
        Assert.Null(result.Request);

        var savedRequest = await dbContext.Requests
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(RequestStatuses.Pending, savedRequest.Status);
        Assert.Single(await dbContext.RequestStatusHistories.ToListAsync());
    }

    [Fact]
    public async Task ApproveAsync_AlreadyApproved_ReturnsConflictWithoutExtraHistory()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var owner = CreateUser("conflict-owner@test.local");
        var manager = CreateUser("conflict-manager@test.local");

        dbContext.Users.AddRange(owner, manager);
        await dbContext.SaveChangesAsync();

        var requestService =
            scope.ServiceProvider.GetRequiredService<RequestService>();

        var createdRequest = await requestService.CreateAsync(
            CreateValidRequestDto(),
            owner.Id);

        var firstResult = await requestService.ApproveAsync(
            createdRequest.Id,
            manager.Id);

        Assert.Equal(RequestTransitionOutcome.Success, firstResult.Outcome);

        var secondResult = await requestService.ApproveAsync(
            createdRequest.Id,
            manager.Id);

        Assert.Equal(RequestTransitionOutcome.Conflict, secondResult.Outcome);
        Assert.Null(secondResult.Request);
        Assert.Equal(
            2,
            await dbContext.RequestStatusHistories.CountAsync());
    }

    [Fact]
    public async Task ApproveAsync_MissingRequest_ReturnsNotFound()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var requestService =
            scope.ServiceProvider.GetRequiredService<RequestService>();

        var result = await requestService.ApproveAsync(
            999999,
            "missing-manager-id");

        Assert.Equal(RequestTransitionOutcome.NotFound, result.Outcome);
        Assert.Null(result.Request);
    }

    [Fact]
    public async Task RejectAsync_PendingRequest_RejectsAndWritesReasonToHistory()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var owner = CreateUser("reject-owner@test.local");
        var manager = CreateUser("reject-manager@test.local");

        dbContext.Users.AddRange(owner, manager);
        await dbContext.SaveChangesAsync();

        var requestService =
            scope.ServiceProvider.GetRequiredService<RequestService>();

        var createdRequest = await requestService.CreateAsync(
            CreateValidRequestDto(),
            owner.Id);

        var rejectDto = new RejectRequestDto
        {
            Reason = "The request exceeds the current budget."
        };

        var result = await requestService.RejectAsync(
            createdRequest.Id,
            manager.Id,
            rejectDto);

        Assert.Equal(RequestTransitionOutcome.Success, result.Outcome);
        Assert.NotNull(result.Request);
        Assert.Equal(RequestStatuses.Rejected, result.Request.Status);

        var savedRequest = await dbContext.Requests
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(RequestStatuses.Rejected, savedRequest.Status);

        var history = await dbContext.RequestStatusHistories
            .AsNoTracking()
            .OrderBy(entry => entry.ChangedAt)
            .ThenBy(entry => entry.Id)
            .ToListAsync();

        Assert.Equal(2, history.Count);
        Assert.Equal(RequestStatuses.Pending, history[1].FromStatus);
        Assert.Equal(RequestStatuses.Rejected, history[1].ToStatus);
        Assert.Equal(manager.Id, history[1].ChangedByUserId);
        Assert.Equal(rejectDto.Reason, history[1].Note);
    }

    [Fact]
    public async Task RejectAsync_ByRequester_ReturnsForbiddenWithoutChanges()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var owner = CreateUser("self-reject@test.local");

        dbContext.Users.Add(owner);
        await dbContext.SaveChangesAsync();

        var requestService =
            scope.ServiceProvider.GetRequiredService<RequestService>();

        var createdRequest = await requestService.CreateAsync(
            CreateValidRequestDto(),
            owner.Id);

        var result = await requestService.RejectAsync(
            createdRequest.Id,
            owner.Id,
            CreateValidRejectDto());

        Assert.Equal(RequestTransitionOutcome.Forbidden, result.Outcome);
        Assert.Null(result.Request);

        var savedRequest = await dbContext.Requests
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(RequestStatuses.Pending, savedRequest.Status);
        Assert.Single(await dbContext.RequestStatusHistories.ToListAsync());
    }

    [Fact]
    public async Task RejectAsync_ApprovedRequest_ReturnsConflictWithoutExtraHistory()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var owner = CreateUser("reject-conflict-owner@test.local");
        var manager = CreateUser("reject-conflict-manager@test.local");

        dbContext.Users.AddRange(owner, manager);
        await dbContext.SaveChangesAsync();

        var requestService =
            scope.ServiceProvider.GetRequiredService<RequestService>();

        var createdRequest = await requestService.CreateAsync(
            CreateValidRequestDto(),
            owner.Id);

        var approvalResult = await requestService.ApproveAsync(
            createdRequest.Id,
            manager.Id);

        Assert.Equal(
            RequestTransitionOutcome.Success,
            approvalResult.Outcome);

        var rejectionResult = await requestService.RejectAsync(
            createdRequest.Id,
            manager.Id,
            CreateValidRejectDto());

        Assert.Equal(
            RequestTransitionOutcome.Conflict,
            rejectionResult.Outcome);
        Assert.Null(rejectionResult.Request);
        Assert.Equal(
            2,
            await dbContext.RequestStatusHistories.CountAsync());
    }

    [Fact]
    public async Task RejectAsync_MissingRequest_ReturnsNotFound()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var requestService =
            scope.ServiceProvider.GetRequiredService<RequestService>();

        var result = await requestService.RejectAsync(
            999999,
            "missing-manager-id",
            CreateValidRejectDto());

        Assert.Equal(RequestTransitionOutcome.NotFound, result.Outcome);
        Assert.Null(result.Request);
    }

    [Fact]
    public async Task CompleteAsync_ApprovedRequest_CompletesAndWritesHistory()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var owner = CreateUser("complete-owner@test.local");
        var manager = CreateUser("complete-manager@test.local");

        dbContext.Users.AddRange(owner, manager);
        await dbContext.SaveChangesAsync();

        var requestService =
            scope.ServiceProvider.GetRequiredService<RequestService>();

        var createdRequest = await requestService.CreateAsync(
            CreateValidRequestDto(),
            owner.Id);

        var approvalResult = await requestService.ApproveAsync(
            createdRequest.Id,
            manager.Id);

        Assert.Equal(
            RequestTransitionOutcome.Success,
            approvalResult.Outcome);

        var completionResult = await requestService.CompleteAsync(
            createdRequest.Id,
            manager.Id);

        Assert.Equal(
            RequestTransitionOutcome.Success,
            completionResult.Outcome);
        Assert.NotNull(completionResult.Request);
        Assert.Equal(
            RequestStatuses.Completed,
            completionResult.Request.Status);

        var savedRequest = await dbContext.Requests
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(RequestStatuses.Completed, savedRequest.Status);

        var history = await dbContext.RequestStatusHistories
            .AsNoTracking()
            .OrderBy(entry => entry.ChangedAt)
            .ThenBy(entry => entry.Id)
            .ToListAsync();

        Assert.Equal(3, history.Count);
        Assert.Equal(RequestStatuses.Approved, history[2].FromStatus);
        Assert.Equal(RequestStatuses.Completed, history[2].ToStatus);
        Assert.Equal(manager.Id, history[2].ChangedByUserId);
    }

    [Fact]
    public async Task CompleteAsync_PendingRequest_ReturnsConflictWithoutExtraHistory()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var owner = CreateUser("pending-complete-owner@test.local");
        var manager = CreateUser("pending-complete-manager@test.local");

        dbContext.Users.AddRange(owner, manager);
        await dbContext.SaveChangesAsync();

        var requestService =
            scope.ServiceProvider.GetRequiredService<RequestService>();

        var createdRequest = await requestService.CreateAsync(
            CreateValidRequestDto(),
            owner.Id);

        var result = await requestService.CompleteAsync(
            createdRequest.Id,
            manager.Id);

        Assert.Equal(RequestTransitionOutcome.Conflict, result.Outcome);
        Assert.Null(result.Request);
        Assert.Single(await dbContext.RequestStatusHistories.ToListAsync());
    }

    [Fact]
    public async Task CompleteAsync_MissingRequest_ReturnsNotFound()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var requestService =
            scope.ServiceProvider.GetRequiredService<RequestService>();

        var result = await requestService.CompleteAsync(
            999999,
            "missing-manager-id");

        Assert.Equal(RequestTransitionOutcome.NotFound, result.Outcome);
        Assert.Null(result.Request);
    }

    [Fact]
    public async Task CancelAsync_ByRequester_CancelsAndWritesHistory()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var owner = CreateUser("cancel-owner@test.local");

        dbContext.Users.Add(owner);
        await dbContext.SaveChangesAsync();

        var requestService =
            scope.ServiceProvider.GetRequiredService<RequestService>();

        var createdRequest = await requestService.CreateAsync(
            CreateValidRequestDto(),
            owner.Id);

        var result = await requestService.CancelAsync(
            createdRequest.Id,
            owner.Id);

        Assert.Equal(RequestTransitionOutcome.Success, result.Outcome);
        Assert.NotNull(result.Request);
        Assert.Equal(RequestStatuses.Cancelled, result.Request.Status);

        var savedRequest = await dbContext.Requests
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(RequestStatuses.Cancelled, savedRequest.Status);

        var history = await dbContext.RequestStatusHistories
            .AsNoTracking()
            .OrderBy(entry => entry.ChangedAt)
            .ThenBy(entry => entry.Id)
            .ToListAsync();

        Assert.Equal(2, history.Count);
        Assert.Equal(RequestStatuses.Pending, history[1].FromStatus);
        Assert.Equal(RequestStatuses.Cancelled, history[1].ToStatus);
        Assert.Equal(owner.Id, history[1].ChangedByUserId);
    }

    [Fact]
    public async Task CancelAsync_ByNonRequester_ReturnsForbiddenWithoutChanges()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var owner = CreateUser("cancel-owner@test.local");
        var otherUser = CreateUser("cancel-other@test.local");

        dbContext.Users.AddRange(owner, otherUser);
        await dbContext.SaveChangesAsync();

        var requestService =
            scope.ServiceProvider.GetRequiredService<RequestService>();

        var createdRequest = await requestService.CreateAsync(
            CreateValidRequestDto(),
            owner.Id);

        var result = await requestService.CancelAsync(
            createdRequest.Id,
            otherUser.Id);

        Assert.Equal(RequestTransitionOutcome.Forbidden, result.Outcome);
        Assert.Null(result.Request);

        var savedRequest = await dbContext.Requests
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(RequestStatuses.Pending, savedRequest.Status);
        Assert.Single(await dbContext.RequestStatusHistories.ToListAsync());
    }

    [Fact]
    public async Task CancelAsync_ApprovedRequest_ReturnsConflictWithoutExtraHistory()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var owner = CreateUser("cancel-approved-owner@test.local");
        var manager = CreateUser("cancel-approved-manager@test.local");

        dbContext.Users.AddRange(owner, manager);
        await dbContext.SaveChangesAsync();

        var requestService =
            scope.ServiceProvider.GetRequiredService<RequestService>();

        var createdRequest = await requestService.CreateAsync(
            CreateValidRequestDto(),
            owner.Id);

        var approvalResult = await requestService.ApproveAsync(
            createdRequest.Id,
            manager.Id);

        Assert.Equal(
            RequestTransitionOutcome.Success,
            approvalResult.Outcome);

        var cancellationResult = await requestService.CancelAsync(
            createdRequest.Id,
            owner.Id);

        Assert.Equal(
            RequestTransitionOutcome.Conflict,
            cancellationResult.Outcome);
        Assert.Null(cancellationResult.Request);
        Assert.Equal(
            2,
            await dbContext.RequestStatusHistories.CountAsync());
    }

    [Fact]
    public async Task CancelAsync_MissingRequest_ReturnsNotFound()
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await ResetDatabaseAsync(dbContext);

        var requestService =
            scope.ServiceProvider.GetRequiredService<RequestService>();

        var result = await requestService.CancelAsync(
            999999,
            "missing-owner-id");

        Assert.Equal(RequestTransitionOutcome.NotFound, result.Outcome);
        Assert.Null(result.Request);
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
            Title = "Request approval test",
            Description = "This request is used to test approval behavior.",
            Category = RequestCategories.Equipment
        };
    }

    private static RejectRequestDto CreateValidRejectDto()
    {
        return new RejectRequestDto
        {
            Reason = "The request cannot be approved at this time."
        };
    }
}
