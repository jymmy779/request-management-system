using Microsoft.EntityFrameworkCore;
using RequestManagementSystem.Api.Data;
using RequestManagementSystem.Api.DTOs;
using RequestManagementSystem.Api.Models;
using RequestModel = RequestManagementSystem.Api.Models.Request;

namespace RequestManagementSystem.Api.Services;

public class RequestService
{
    private readonly AppDbContext _context;

    public RequestService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<RequestResponseDto>> GetAllAsync(
        string requesterId,
        bool canViewAll,
        RequestQueryParameters parameters)
    {
        var query = _context.Requests.AsNoTracking();

        if (!canViewAll)
        {
            query = query.Where(
                request => request.RequesterId == requesterId);
        }

        return await GetPagedRequestsAsync(query, parameters);
    }

    public async Task<PagedResult<RequestResponseDto>> GetMyApprovalsAsync(
        string actorUserId,
        RequestQueryParameters parameters)
    {
        var query = _context.Requests
            .AsNoTracking()
            .Where(request =>
                request.Status == RequestStatuses.Pending &&
                request.RequesterId != actorUserId);

        return await GetPagedRequestsAsync(query, parameters);
    }

    public async Task<PagedResult<RequestResponseDto>> GetApprovalHistoryAsync(
        string actorUserId,
        RequestQueryParameters parameters)
    {
        var query = _context.Requests
            .AsNoTracking()
            .Where(request => request.StatusHistory.Any(history =>
                history.ChangedByUserId == actorUserId &&
                (history.ToStatus == RequestStatuses.Approved ||
                 history.ToStatus == RequestStatuses.Rejected)));

        return await GetPagedRequestsAsync(query, parameters);
    }

    private static async Task<PagedResult<RequestResponseDto>>
        GetPagedRequestsAsync(
            IQueryable<RequestModel> query,
            RequestQueryParameters parameters)
    {
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim();

            query = query.Where(
                request =>
                    request.Title.Contains(search) ||
                    request.Description.Contains(search));
        }

        if (parameters.Status is not null)
        {
            query = query.Where(
                request => request.Status == parameters.Status);
        }

        if (parameters.Category is not null)
        {
            query = query.Where(
                request => request.Category == parameters.Category);
        }

        if (parameters.Priority is not null)
        {
            query = query.Where(
                request => request.Priority == parameters.Priority);
        }

        if (parameters.DepartmentId is not null)
        {
            query = query.Where(
                request => request.Requester.DepartmentId ==
                    parameters.DepartmentId);
        }

        var totalCount = await query.CountAsync();

        query = (parameters.SortBy, parameters.SortDirection) switch
        {
            ("title", "asc") =>
                query.OrderBy(request => request.Title)
                    .ThenBy(request => request.Id),

            ("title", "desc") =>
                query.OrderByDescending(request => request.Title)
                    .ThenByDescending(request => request.Id),

            ("status", "asc") =>
                query.OrderBy(request => request.Status)
                    .ThenBy(request => request.Id),

            ("status", "desc") =>
                query.OrderByDescending(request => request.Status)
                    .ThenByDescending(request => request.Id),

            ("createdAt", "asc") =>
                query.OrderBy(request => request.CreatedAt)
                    .ThenBy(request => request.Id),

            _ =>
                query.OrderByDescending(request => request.CreatedAt)
                    .ThenByDescending(request => request.Id)
        };

        var items = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(request => new RequestResponseDto
            {
                Id = request.Id,
                Title = request.Title,
                Description = request.Description,
                Category = request.Category,
                Priority = request.Priority,
                Quantity = request.Quantity,
                EstimatedCost = request.EstimatedCost,
                Status = request.Status,
                RequesterId = request.RequesterId,
                RequesterFullName = request.Requester.FullName,
                RequesterEmail = request.Requester.Email ?? string.Empty,
                RequesterDepartmentName = request.Requester.Department != null
                    ? request.Requester.Department.Name
                    : null,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt
            })
            .ToListAsync();

        return new PagedResult<RequestResponseDto>
        {
            Items = items,
            Page = parameters.Page,
            PageSize = parameters.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(
                totalCount / (double)parameters.PageSize)
        };
    }

    public async Task<RequestResponseDto> CreateAsync(
        CreateRequestDto dto,
        string requesterId)
    {
        var now = DateTime.UtcNow;

        var requester = await _context.Users
            .AsNoTracking()
            .Where(user => user.Id == requesterId)
            .Select(user => new
            {
                user.FullName,
                user.Email,
                DepartmentName = user.Department != null
                    ? user.Department.Name
                    : null
            })
            .SingleAsync();

        var request = new Request
        {
            Title = dto.Title,
            Description = dto.Description,
            Category = dto.Category,
            Priority = dto.Priority,
            Quantity = dto.Quantity,
            EstimatedCost = dto.EstimatedCost,
            RequesterId = requesterId,
            CreatedAt = now
        };

        request.StatusHistory.Add(new RequestStatusHistory
        {
            FromStatus = null,
            ToStatus = RequestStatuses.Pending,
            ChangedByUserId = requesterId,
            ChangedAt = now
        });

        _context.Requests.Add(request);
        await _context.SaveChangesAsync();

        return new RequestResponseDto
        {
            Id = request.Id,
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Priority = request.Priority,
            Quantity = request.Quantity,
            EstimatedCost = request.EstimatedCost,
            Status = request.Status,
            RequesterId = request.RequesterId,
            RequesterFullName = requester.FullName,
            RequesterEmail = requester.Email ?? string.Empty,
            RequesterDepartmentName = requester.DepartmentName,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt
        };
    }

    public async Task<RequestUpdateResult> UpdateAsync(
        int requestId,
        string actorUserId,
        UpdateRequestDto dto)
    {
        var request = await _context.Requests
            .Include(item => item.Requester)
            .ThenInclude(requester => requester.Department)
            .SingleOrDefaultAsync(item => item.Id == requestId);

        if (request is null)
        {
            return new RequestUpdateResult
            {
                Outcome = RequestUpdateOutcome.NotFound
            };
        }

        if (request.RequesterId != actorUserId)
        {
            return new RequestUpdateResult
            {
                Outcome = RequestUpdateOutcome.Forbidden
            };
        }

        if (request.Status != RequestStatuses.Pending)
        {
            return new RequestUpdateResult
            {
                Outcome = RequestUpdateOutcome.Conflict
            };
        }

        request.Title = dto.Title;
        request.Description = dto.Description;
        request.Category = dto.Category;
        request.Priority = dto.Priority;
        request.Quantity = dto.Quantity;
        request.EstimatedCost = dto.EstimatedCost;
        request.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return new RequestUpdateResult
            {
                Outcome = RequestUpdateOutcome.Conflict
            };
        }

        return new RequestUpdateResult
        {
            Outcome = RequestUpdateOutcome.Success,
            Request = new RequestResponseDto
            {
                Id = request.Id,
                Title = request.Title,
                Description = request.Description,
                Category = request.Category,
                Priority = request.Priority,
                Quantity = request.Quantity,
                EstimatedCost = request.EstimatedCost,
                Status = request.Status,
                RequesterId = request.RequesterId,
                RequesterFullName = request.Requester.FullName,
                RequesterEmail = request.Requester.Email ?? string.Empty,
                RequesterDepartmentName = request.Requester.Department?.Name,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt
            }
        };
    }

    public async Task<RequestResponseDto?> GetByIdAsync(
        int id,
        string requesterId,
        bool canViewAll)
    {
        var query = _context.Requests
            .AsNoTracking()
            .Where(request => request.Id == id);

        if (!canViewAll)
        {
            query = query.Where(
                request => request.RequesterId == requesterId);
        }

        return await query
            .Select(request => new RequestResponseDto
            {
                Id = request.Id,
                Title = request.Title,
                Description = request.Description,
                Category = request.Category,
                Priority = request.Priority,
                Quantity = request.Quantity,
                EstimatedCost = request.EstimatedCost,
                Status = request.Status,
                RequesterId = request.RequesterId,
                RequesterFullName = request.Requester.FullName,
                RequesterEmail = request.Requester.Email ?? string.Empty,
                RequesterDepartmentName = request.Requester.Department != null
                    ? request.Requester.Department.Name
                    : null,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt
            })
            .SingleOrDefaultAsync();
    }

    public async Task<List<RequestStatusHistoryResponseDto>?> GetHistoryAsync(
        int requestId,
        string requesterId,
        bool canViewAll)
    {
        var requestQuery = _context.Requests
            .AsNoTracking()
            .Where(request => request.Id == requestId);

        if (!canViewAll)
        {
            requestQuery = requestQuery.Where(
                request => request.RequesterId == requesterId);
        }

        if (!await requestQuery.AnyAsync())
        {
            return null;
        }

        return await _context.RequestStatusHistories
            .AsNoTracking()
            .Where(history => history.RequestId == requestId)
            .OrderBy(history => history.ChangedAt)
            .ThenBy(history => history.Id)
            .Select(history => new RequestStatusHistoryResponseDto
            {
                Id = history.Id,
                RequestId = history.RequestId,
                FromStatus = history.FromStatus,
                ToStatus = history.ToStatus,
                ChangedByUserId = history.ChangedByUserId,
                ChangedByFullName = history.ChangedByUser.FullName,
                ChangedByEmail = history.ChangedByUser.Email ?? string.Empty,
                Note = history.Note,
                ChangedAt = history.ChangedAt
            })
            .ToListAsync();
    }

    public async Task<RequestTransitionResult> ApproveAsync(
        int requestId,
        string actorUserId)
    {
        var request = await _context.Requests
            .Include(request => request.Requester)
            .ThenInclude(requester => requester.Department)
            .SingleOrDefaultAsync(request => request.Id == requestId);

        if (request is null)
        {
            return new RequestTransitionResult
            {
                Outcome = RequestTransitionOutcome.NotFound
            };
        }

        if (request.RequesterId == actorUserId)
        {
            return new RequestTransitionResult
            {
                Outcome = RequestTransitionOutcome.Forbidden
            };
        }

        if (!RequestStateMachine.CanTransition(
                request.Status,
                RequestStatuses.Approved))
        {
            return new RequestTransitionResult
            {
                Outcome = RequestTransitionOutcome.Conflict
            };
        }

        var previousStatus = request.Status;
        var changedAt = DateTime.UtcNow;

        request.Status = RequestStatuses.Approved;
        request.UpdatedAt = changedAt;
        request.StatusHistory.Add(new RequestStatusHistory
        {
            FromStatus = previousStatus,
            ToStatus = RequestStatuses.Approved,
            ChangedByUserId = actorUserId,
            ChangedAt = changedAt
        });

        if (!await TrySaveTransitionAsync())
        {
            return new RequestTransitionResult
            {
                Outcome = RequestTransitionOutcome.Conflict
            };
        }

        return new RequestTransitionResult
        {
            Outcome = RequestTransitionOutcome.Success,
            Request = new RequestResponseDto
            {
                Id = request.Id,
                Title = request.Title,
                Description = request.Description,
                Category = request.Category,
                Priority = request.Priority,
                Quantity = request.Quantity,
                EstimatedCost = request.EstimatedCost,
                Status = request.Status,
                RequesterId = request.RequesterId,
                RequesterFullName = request.Requester.FullName,
                RequesterEmail = request.Requester.Email ?? string.Empty,
                RequesterDepartmentName = request.Requester.Department?.Name,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt
            }
        };
    }

    public async Task<RequestTransitionResult> RejectAsync(
        int requestId,
        string actorUserId,
        RejectRequestDto dto)
    {
        var request = await _context.Requests
            .Include(request => request.Requester)
            .ThenInclude(requester => requester.Department)
            .SingleOrDefaultAsync(request => request.Id == requestId);

        if (request is null)
        {
            return new RequestTransitionResult
            {
                Outcome = RequestTransitionOutcome.NotFound
            };
        }

        if (request.RequesterId == actorUserId)
        {
            return new RequestTransitionResult
            {
                Outcome = RequestTransitionOutcome.Forbidden
            };
        }

        if (!RequestStateMachine.CanTransition(
                request.Status,
                RequestStatuses.Rejected))
        {
            return new RequestTransitionResult
            {
                Outcome = RequestTransitionOutcome.Conflict
            };
        }

        var previousStatus = request.Status;
        var changedAt = DateTime.UtcNow;

        request.Status = RequestStatuses.Rejected;
        request.UpdatedAt = changedAt;
        request.StatusHistory.Add(new RequestStatusHistory
        {
            FromStatus = previousStatus,
            ToStatus = RequestStatuses.Rejected,
            ChangedByUserId = actorUserId,
            Note = dto.Reason,
            ChangedAt = changedAt
        });

        if (!await TrySaveTransitionAsync())
        {
            return new RequestTransitionResult
            {
                Outcome = RequestTransitionOutcome.Conflict
            };
        }

        return new RequestTransitionResult
        {
            Outcome = RequestTransitionOutcome.Success,
            Request = new RequestResponseDto
            {
                Id = request.Id,
                Title = request.Title,
                Description = request.Description,
                Category = request.Category,
                Priority = request.Priority,
                Quantity = request.Quantity,
                EstimatedCost = request.EstimatedCost,
                Status = request.Status,
                RequesterId = request.RequesterId,
                RequesterFullName = request.Requester.FullName,
                RequesterEmail = request.Requester.Email ?? string.Empty,
                RequesterDepartmentName = request.Requester.Department?.Name,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt
            }
        };
    }

    public async Task<RequestTransitionResult> CompleteAsync(
        int requestId,
        string actorUserId)
    {
        var request = await _context.Requests
            .Include(request => request.Requester)
            .ThenInclude(requester => requester.Department)
            .SingleOrDefaultAsync(request => request.Id == requestId);

        if (request is null)
        {
            return new RequestTransitionResult
            {
                Outcome = RequestTransitionOutcome.NotFound
            };
        }

        if (!RequestStateMachine.CanTransition(
                request.Status,
                RequestStatuses.Completed))
        {
            return new RequestTransitionResult
            {
                Outcome = RequestTransitionOutcome.Conflict
            };
        }

        var previousStatus = request.Status;
        var changedAt = DateTime.UtcNow;

        request.Status = RequestStatuses.Completed;
        request.UpdatedAt = changedAt;
        request.StatusHistory.Add(new RequestStatusHistory
        {
            FromStatus = previousStatus,
            ToStatus = RequestStatuses.Completed,
            ChangedByUserId = actorUserId,
            ChangedAt = changedAt
        });

        if (!await TrySaveTransitionAsync())
        {
            return new RequestTransitionResult
            {
                Outcome = RequestTransitionOutcome.Conflict
            };
        }

        return new RequestTransitionResult
        {
            Outcome = RequestTransitionOutcome.Success,
            Request = new RequestResponseDto
            {
                Id = request.Id,
                Title = request.Title,
                Description = request.Description,
                Category = request.Category,
                Priority = request.Priority,
                Quantity = request.Quantity,
                EstimatedCost = request.EstimatedCost,
                Status = request.Status,
                RequesterId = request.RequesterId,
                RequesterFullName = request.Requester.FullName,
                RequesterEmail = request.Requester.Email ?? string.Empty,
                RequesterDepartmentName = request.Requester.Department?.Name,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt
            }
        };
    }

    public async Task<RequestTransitionResult> CancelAsync(
        int requestId,
        string actorUserId)
    {
        var request = await _context.Requests
            .Include(request => request.Requester)
            .ThenInclude(requester => requester.Department)
            .SingleOrDefaultAsync(request => request.Id == requestId);

        if (request is null)
        {
            return new RequestTransitionResult
            {
                Outcome = RequestTransitionOutcome.NotFound
            };
        }

        if (request.RequesterId != actorUserId)
        {
            return new RequestTransitionResult
            {
                Outcome = RequestTransitionOutcome.Forbidden
            };
        }

        if (!RequestStateMachine.CanTransition(
                request.Status,
                RequestStatuses.Cancelled))
        {
            return new RequestTransitionResult
            {
                Outcome = RequestTransitionOutcome.Conflict
            };
        }

        var previousStatus = request.Status;
        var changedAt = DateTime.UtcNow;

        request.Status = RequestStatuses.Cancelled;
        request.UpdatedAt = changedAt;
        request.StatusHistory.Add(new RequestStatusHistory
        {
            FromStatus = previousStatus,
            ToStatus = RequestStatuses.Cancelled,
            ChangedByUserId = actorUserId,
            ChangedAt = changedAt
        });

        if (!await TrySaveTransitionAsync())
        {
            return new RequestTransitionResult
            {
                Outcome = RequestTransitionOutcome.Conflict
            };
        }

        return new RequestTransitionResult
        {
            Outcome = RequestTransitionOutcome.Success,
            Request = new RequestResponseDto
            {
                Id = request.Id,
                Title = request.Title,
                Description = request.Description,
                Category = request.Category,
                Priority = request.Priority,
                Quantity = request.Quantity,
                EstimatedCost = request.EstimatedCost,
                Status = request.Status,
                RequesterId = request.RequesterId,
                RequesterFullName = request.Requester.FullName,
                RequesterEmail = request.Requester.Email ?? string.Empty,
                RequesterDepartmentName = request.Requester.Department?.Name,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt
            }
        };
    }

    private async Task<bool> TrySaveTransitionAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}
