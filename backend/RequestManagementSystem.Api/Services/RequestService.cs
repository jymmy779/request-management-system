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
        var request = new Request
        {
            Title = dto.Title,
            Description = dto.Description,
            Category = dto.Category,
            RequesterId = requesterId
        };

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
}
