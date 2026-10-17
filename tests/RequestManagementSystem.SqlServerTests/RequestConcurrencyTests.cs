using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RequestManagementSystem.Api.Data;
using RequestManagementSystem.Api.DTOs;
using RequestManagementSystem.Api.Models;
using RequestManagementSystem.Api.Services;

namespace RequestManagementSystem.SqlServerTests;

public class RequestConcurrencyTests
{
    private const string ConnectionVariable =
        "RMS_SQLSERVER_TEST_CONNECTION";

    [Fact]
    public async Task TwoManagersApproveSameVersion_OneSucceedsAndOneConflicts()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable(ConnectionVariable);

        Assert.False(
            string.IsNullOrWhiteSpace(baseConnectionString),
            $"Set {ConnectionVariable} before running SQL Server tests.");

        var databaseName =
            $"RequestManagementSystemConcurrencyTests_{Guid.NewGuid():N}";

        var connectionBuilder = new SqlConnectionStringBuilder(
            baseConnectionString)
        {
            InitialCatalog = databaseName
        };

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionBuilder.ConnectionString)
            .Options;

        try
        {
            var scenario = await CreatePendingRequestAsync(options);

            await using var firstContext = new AppDbContext(options);
            await using var secondContext = new AppDbContext(options);

            var firstCopy = await firstContext.Requests
                .SingleAsync(request =>
                    request.Id == scenario.RequestId);
            var secondCopy = await secondContext.Requests
                .SingleAsync(request =>
                    request.Id == scenario.RequestId);

            Assert.Equal(firstCopy.RowVersion, secondCopy.RowVersion);

            var firstService = new RequestService(firstContext);
            var secondService = new RequestService(secondContext);

            var results = await Task.WhenAll(
                firstService.ApproveAsync(
                    scenario.RequestId,
                    scenario.FirstManagerId),
                secondService.ApproveAsync(
                    scenario.RequestId,
                    scenario.SecondManagerId));

            Assert.Single(
                results,
                result =>
                    result.Outcome == RequestTransitionOutcome.Success);
            Assert.Single(
                results,
                result =>
                    result.Outcome == RequestTransitionOutcome.Conflict);

            await using var verificationContext =
                new AppDbContext(options);

            var savedRequest = await verificationContext.Requests
                .AsNoTracking()
                .SingleAsync(request =>
                    request.Id == scenario.RequestId);

            Assert.Equal(RequestStatuses.Approved, savedRequest.Status);

            var approvalHistoryCount =
                await verificationContext.RequestStatusHistories
                    .CountAsync(history =>
                        history.RequestId == scenario.RequestId &&
                        history.ToStatus == RequestStatuses.Approved);

            Assert.Equal(1, approvalHistoryCount);
        }
        finally
        {
            await using var cleanupContext = new AppDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    private static async Task<ConcurrencyScenario>
        CreatePendingRequestAsync(
            DbContextOptions<AppDbContext> options)
    {
        await using var setupContext = new AppDbContext(options);
        await setupContext.Database.MigrateAsync();

        var owner = CreateUser("concurrency-owner@test.local");
        var firstManager = CreateUser(
            "concurrency-manager-one@test.local");
        var secondManager = CreateUser(
            "concurrency-manager-two@test.local");

        setupContext.Users.AddRange(
            owner,
            firstManager,
            secondManager);
        await setupContext.SaveChangesAsync();

        var requestService = new RequestService(setupContext);
        var request = await requestService.CreateAsync(
            new CreateRequestDto
            {
                Title = "Concurrent approval test",
                Description =
                    "Two managers attempt to approve this request together.",
                Category = RequestCategories.Equipment
            },
            owner.Id);

        return new ConcurrencyScenario
        {
            RequestId = request.Id,
            FirstManagerId = firstManager.Id,
            SecondManagerId = secondManager.Id
        };
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

    private sealed class ConcurrencyScenario
    {
        public int RequestId { get; set; }

        public string FirstManagerId { get; set; } = string.Empty;

        public string SecondManagerId { get; set; } = string.Empty;
    }
}
