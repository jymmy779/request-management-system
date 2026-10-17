using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RequestManagementSystem.Api.Models;

namespace RequestManagementSystem.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Request> Requests => Set<Request>();

    public DbSet<RequestStatusHistory> RequestStatusHistories =>
        Set<RequestStatusHistory>();

    protected override void OnModelCreating(
        ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Request>()
            .HasOne(request => request.Requester)
            .WithMany()
            .HasForeignKey(request => request.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        if (Database.IsSqlServer())
        {
            builder.Entity<Request>()
                .Property(request => request.RowVersion)
                .IsRowVersion();
        }

        builder.Entity<RequestStatusHistory>()
            .HasOne(history => history.Request)
            .WithMany(request => request.StatusHistory)
            .HasForeignKey(history => history.RequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RequestStatusHistory>()
            .HasOne(history => history.ChangedByUser)
            .WithMany()
            .HasForeignKey(history => history.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RequestStatusHistory>()
            .Property(history => history.Note)
            .HasMaxLength(500);

        builder.Entity<RequestStatusHistory>()
            .Property(history => history.FromStatus)
            .HasMaxLength(50);

        builder.Entity<RequestStatusHistory>()
            .Property(history => history.ToStatus)
            .HasMaxLength(50);
    }
}
