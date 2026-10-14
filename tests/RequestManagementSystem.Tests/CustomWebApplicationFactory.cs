using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using RequestManagementSystem.Api.Data;

namespace RequestManagementSystem.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(
                service => service.ServiceType ==
                    typeof(IDbContextOptionsConfiguration<AppDbContext>));

            if (dbContextDescriptor is not null)
            {
                services.Remove(dbContextDescriptor);
            }

            services.AddSingleton<DbConnection>(_ =>
            {
                var connection =
                    new SqliteConnection("DataSource=:memory:");

                connection.Open();
                return connection;
            });

            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                var connection =
                    serviceProvider.GetRequiredService<DbConnection>();

                options.UseSqlite(connection);
            });
        });
    }
}