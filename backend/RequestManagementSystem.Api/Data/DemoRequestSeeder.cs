using Microsoft.EntityFrameworkCore;
using RequestManagementSystem.Api.Authorization;
using RequestManagementSystem.Api.Models;

namespace RequestManagementSystem.Api.Data;

public static class DemoRequestSeeder
{
    private const string DemoTitlePrefix = "[DEMO-LOAD]";
    private const int BatchSize = 500;

    private static readonly string[] Titles =
    [
        "Request a new monitor",
        "Request software license",
        "Replace broken keyboard",
        "Install development tools",
        "Request office equipment"
    ];

    private static readonly string[] Categories =
    [
        RequestCategories.Equipment,
        RequestCategories.Software,
        RequestCategories.Other
    ];

    public static async Task<int> SeedAsync(
        AppDbContext context,
        IConfiguration configuration,
        int requestCount)
    {
        if (requestCount is < 1 or > 50_000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requestCount),
                "Request count must be between 1 and 50,000.");
        }

        if (await context.Requests.AnyAsync(
                request => request.Title.StartsWith(DemoTitlePrefix)))
        {
            return 0;
        }

        var employeeEmail = GetDemoEmail(
            configuration,
            AppRoles.Employee);

        var managerEmail = GetDemoEmail(
            configuration,
            AppRoles.Manager);

        var adminEmail = GetDemoEmail(
            configuration,
            AppRoles.Admin);

        var demoEmails = new[]
        {
            employeeEmail,
            managerEmail,
            adminEmail
        };

        var users = await context.Users
            .AsNoTracking()
            .Where(user =>
                user.Email != null &&
                demoEmails.Contains(user.Email))
            .ToListAsync();

        if (users.Count != demoEmails.Length)
        {
            throw new InvalidOperationException(
                "All three demo users must exist before seeding requests.");
        }

        var employeeId = users.Single(
            user => user.Email == employeeEmail).Id;

        var managerId = users.Single(
            user => user.Email == managerEmail).Id;

        var adminId = users.Single(
            user => user.Email == adminEmail).Id;

        var ownerIds = new[]
        {
            employeeId,
            managerId,
            adminId
        };

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        for (var number = 1; number <= requestCount; number++)
        {
            var ownerId =
                ownerIds[(number - 1) % ownerIds.Length];

            var actorId =
                ownerId == managerId
                    ? adminId
                    : managerId;

            context.Requests.Add(
                CreateRequest(
                    number,
                    ownerId,
                    actorId));

            if (number % BatchSize == 0)
            {
                await context.SaveChangesAsync();
                context.ChangeTracker.Clear();
            }
        }

        if (requestCount % BatchSize != 0)
        {
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
        }

        await transaction.CommitAsync();

        return requestCount;
    }

    public static async Task<int> BackfillAsync(AppDbContext context)
    {
        var updatedCount = 0;
        var lastRequestId = 0;

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        while (true)
        {
            var requests = await context.Requests
                .Where(request =>
                    request.Id > lastRequestId &&
                    request.Title.StartsWith(DemoTitlePrefix))
                .OrderBy(request => request.Id)
                .Take(BatchSize)
                .Include(request => request.StatusHistory)
                .ToListAsync();

            if (requests.Count == 0)
            {
                break;
            }

            foreach (var request in requests)
            {
                lastRequestId = request.Id;

                if (!TryGetDemoNumber(request.Title, out var number))
                {
                    continue;
                }

                var priority = GetPriority(number);
                var quantity = ((number - 1) % 5) + 1;
                var estimatedCost = GetEstimatedCost(
                    request.Category,
                    quantity,
                    number);
                var updatedAt = request.Status == RequestStatuses.Pending
                    ? null
                    : request.StatusHistory
                        .OrderBy(history => history.ChangedAt)
                        .ThenBy(history => history.Id)
                        .Select(history => (DateTime?)history.ChangedAt)
                        .LastOrDefault();

                if (request.Priority == priority &&
                    request.Quantity == quantity &&
                    request.EstimatedCost == estimatedCost &&
                    request.UpdatedAt == updatedAt)
                {
                    continue;
                }

                request.Priority = priority;
                request.Quantity = quantity;
                request.EstimatedCost = estimatedCost;
                request.UpdatedAt = updatedAt;
                updatedCount++;
            }

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
        }

        await transaction.CommitAsync();

        return updatedCount;
    }

    private static Request CreateRequest(
        int number,
        string ownerId,
        string actorId)
    {
        var status = GetStatus(number);
        var category = Categories[(number - 1) % Categories.Length];
        var quantity = ((number - 1) % 5) + 1;

        var createdAt = DateTime.UtcNow
            .AddDays(-1)
            .AddMinutes(-number * 10);

        var request = new Request
        {
            Title =
                $"{DemoTitlePrefix} {number:D5} - " +
                Titles[(number - 1) % Titles.Length],
            Description =
                $"Generated demo request number {number}.",
            Category = category,
            Priority = GetPriority(number),
            Quantity = quantity,
            EstimatedCost = GetEstimatedCost(
                category,
                quantity,
                number),
            Status = status,
            RequesterId = ownerId,
            CreatedAt = createdAt
        };

        request.StatusHistory.Add(new RequestStatusHistory
        {
            FromStatus = null,
            ToStatus = RequestStatuses.Pending,
            ChangedByUserId = ownerId,
            ChangedAt = createdAt
        });

        AddFinalStatusHistory(
            request,
            status,
            ownerId,
            actorId,
            createdAt);

        return request;
    }

    private static void AddFinalStatusHistory(
        Request request,
        string status,
        string ownerId,
        string actorId,
        DateTime createdAt)
    {
        if (status == RequestStatuses.Pending)
        {
            return;
        }

        if (status == RequestStatuses.Completed)
        {
            request.UpdatedAt = createdAt.AddMinutes(20);

            request.StatusHistory.Add(new RequestStatusHistory
            {
                FromStatus = RequestStatuses.Pending,
                ToStatus = RequestStatuses.Approved,
                ChangedByUserId = actorId,
                ChangedAt = createdAt.AddMinutes(10)
            });

            request.StatusHistory.Add(new RequestStatusHistory
            {
                FromStatus = RequestStatuses.Approved,
                ToStatus = RequestStatuses.Completed,
                ChangedByUserId = actorId,
                ChangedAt = createdAt.AddMinutes(20)
            });

            return;
        }

        request.UpdatedAt = createdAt.AddMinutes(10);

        request.StatusHistory.Add(new RequestStatusHistory
        {
            FromStatus = RequestStatuses.Pending,
            ToStatus = status,
            ChangedByUserId =
                status == RequestStatuses.Cancelled
                    ? ownerId
                    : actorId,
            Note =
                status == RequestStatuses.Rejected
                    ? "Generated rejection reason."
                    : null,
            ChangedAt = createdAt.AddMinutes(10)
        });
    }

    private static string GetStatus(int number)
    {
        return (number % 20) switch
        {
            <= 7 => RequestStatuses.Pending,
            <= 12 => RequestStatuses.Approved,
            <= 15 => RequestStatuses.Rejected,
            <= 18 => RequestStatuses.Completed,
            _ => RequestStatuses.Cancelled
        };
    }

    private static string GetPriority(int number)
    {
        return (number % 10) switch
        {
            <= 2 => RequestPriorities.High,
            <= 6 => RequestPriorities.Medium,
            _ => RequestPriorities.Low
        };
    }

    private static decimal? GetEstimatedCost(
        string category,
        int quantity,
        int number)
    {
        return category switch
        {
            RequestCategories.Equipment => 2_500_000m * quantity,
            RequestCategories.Software => 1_200_000m * quantity,
            RequestCategories.Other when number % 2 == 0 =>
                500_000m * quantity,
            _ => null
        };
    }

    private static bool TryGetDemoNumber(
        string title,
        out int number)
    {
        number = 0;

        var numberStart = DemoTitlePrefix.Length + 1;

        if (title.Length <= numberStart)
        {
            return false;
        }

        var separatorIndex = title.IndexOf(
            " - ",
            numberStart,
            StringComparison.Ordinal);

        return separatorIndex > numberStart &&
            int.TryParse(
                title[numberStart..separatorIndex],
                out number) &&
            number > 0;
    }

    private static string GetDemoEmail(
        IConfiguration configuration,
        string role)
    {
        return configuration[$"DemoUsers:{role}:Email"]
            ?? throw new InvalidOperationException(
                $"Demo {role} email not found.");
    }
}
