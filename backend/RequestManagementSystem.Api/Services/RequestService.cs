using Microsoft.EntityFrameworkCore;
using RequestManagementSystem.Api.Data;
using RequestManagementSystem.Api.DTOs;
using RequestManagementSystem.Api.Models;

namespace RequestManagementSystem.Api.Services;

public class RequestService
{
    private readonly AppDbContext _context;

    public RequestService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<RequestResponseDto>> GetAllAsync(
        string requesterId,
        bool canViewAll)
    {
        var query = _context.Requests.AsNoTracking();

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
                Status = request.Status,
                RequesterId = request.RequesterId,
                CreatedAt = request.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<RequestResponseDto> CreateAsync(
        CreateRequestDto dto,
        string requesterId)
    {
        var now = DateTime.UtcNow;

        var request = new Request
        {
            Title = dto.Title,
            Description = dto.Description,
            Category = dto.Category,
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
            Status = request.Status,
            RequesterId = request.RequesterId,
            CreatedAt = request.CreatedAt
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
                Status = request.Status,
                RequesterId = request.RequesterId,
                CreatedAt = request.CreatedAt
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
                Status = request.Status,
                RequesterId = request.RequesterId,
                CreatedAt = request.CreatedAt
            }
        };
    }

    public async Task<RequestTransitionResult> RejectAsync(
        int requestId,
        string actorUserId,
        RejectRequestDto dto)
    {
        var request = await _context.Requests
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
                Status = request.Status,
                RequesterId = request.RequesterId,
                CreatedAt = request.CreatedAt
            }
        };
    }

    public async Task<RequestTransitionResult> CompleteAsync(
        int requestId,
        string actorUserId)
    {
        var request = await _context.Requests
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
                Status = request.Status,
                RequesterId = request.RequesterId,
                CreatedAt = request.CreatedAt
            }
        };
    }

    public async Task<RequestTransitionResult> CancelAsync(
        int requestId,
        string actorUserId)
    {
        var request = await _context.Requests
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
                Status = request.Status,
                RequesterId = request.RequesterId,
                CreatedAt = request.CreatedAt
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
