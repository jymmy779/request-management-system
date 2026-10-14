using Microsoft.EntityFrameworkCore;
using RequestManagementSystem.Api.Models;

namespace RequestManagementSystem.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Request> Requests => Set<Request>();
}