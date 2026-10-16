using Microsoft.EntityFrameworkCore;
using RequestManagementSystem.Api.Data;
using RequestManagementSystem.Api.Models;

namespace RequestManagementSystem.Api.Services;

public class RequestService
{
    private readonly AppDbContext _context;

    public RequestService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Request>> GetAllAsync(
        string requesterId,
        bool canViewAll)
    {
        var query = _context.Requests.AsNoTracking();

        if (!canViewAll)
        {
            query = query.Where(
                request => request.RequesterId == requesterId);
        }

        return await query.ToListAsync();
    }
}