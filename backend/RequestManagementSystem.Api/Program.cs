using Microsoft.EntityFrameworkCore;
using RequestManagementSystem.Api.Data;
using RequestManagementSystem.Api.Services;
using Microsoft.AspNetCore.Identity;
using RequestManagementSystem.Api.Models;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RequestManagementSystem.Api.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration
        .GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Connection string 'DefaultConnection' not found.");

    options.UseSqlServer(connectionString);
});

builder.Services.AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<AppDbContext>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "JWT key not found.");

        var issuer = builder.Configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT issuer not found.");

        var audience = builder.Configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "JWT audience not found.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
       AppPolicies.CanAccessRequests,
       policy => policy.RequireRole(
           AppRoles.Employee,
           AppRoles.Manager,
           AppRoles.Admin));

    options.AddPolicy(
        AppPolicies.ManagerOrAdmin,
        policy => policy.RequireRole(
            AppRoles.Manager,
            AppRoles.Admin));
});

builder.Services.AddScoped<RequestService>();

builder.Services.AddScoped<TokenService>();

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    await RoleSeeder.SeedAsync(scope.ServiceProvider);

    if (app.Environment.IsDevelopment())
    {
        await DemoUserSeeder.SeedAsync(scope.ServiceProvider);
    }
}

var seedOptionIndex =
    Array.IndexOf(args, "--seed-demo-data");

if (seedOptionIndex >= 0)
{
    if (!app.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "Demo request seeding is only allowed in Development.");
    }

    var requestCount = 10_000;

    if (seedOptionIndex + 1 < args.Length &&
        !int.TryParse(
            args[seedOptionIndex + 1],
            out requestCount))
    {
        throw new InvalidOperationException(
            "Demo request count must be a valid integer.");
    }

    using var scope = app.Services.CreateScope();

    var context =
        scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var insertedCount = await DemoRequestSeeder.SeedAsync(
        context,
        app.Configuration,
        requestCount);

    Console.WriteLine(
        insertedCount == 0
            ? "Demo requests already exist. Nothing was inserted."
            : $"Inserted {insertedCount} demo requests.");

    return;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{

}
