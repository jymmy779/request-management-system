using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RequestManagementSystem.Api.Data;
using RequestManagementSystem.Api.DTOs;
using RequestManagementSystem.Api.Models;

namespace RequestManagementSystem.Api.Services;

public class UserService
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(
        AppDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<PagedResult<UserResponseDto>> GetAllAsync(
        UserQueryParameters parameters)
    {
        var query = _context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim();

            query = query.Where(user =>
                user.FullName.Contains(search) ||
                (user.Email != null && user.Email.Contains(search)));
        }

        if (parameters.Role is not null)
        {
            query = query.Where(user =>
                _context.UserRoles.Any(userRole =>
                    userRole.UserId == user.Id &&
                    _context.Roles.Any(role =>
                        role.Id == userRole.RoleId &&
                        role.Name == parameters.Role)));
        }

        if (parameters.DepartmentId.HasValue)
        {
            query = query.Where(
                user => user.DepartmentId == parameters.DepartmentId);
        }

        query = (parameters.SortBy, parameters.SortDirection) switch
        {
            ("email", "desc") => query.OrderByDescending(user => user.Email),
            ("email", _) => query.OrderBy(user => user.Email),
            ("fullName", "desc") =>
                query.OrderByDescending(user => user.FullName),
            _ => query.OrderBy(user => user.FullName)
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(user => new UserResponseDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = (
                    from userRole in _context.UserRoles
                    join role in _context.Roles
                        on userRole.RoleId equals role.Id
                    where userRole.UserId == user.Id
                    orderby role.Name
                    select role.Name).FirstOrDefault() ?? string.Empty,
                DepartmentId = user.DepartmentId,
                DepartmentName = user.Department == null
                    ? null
                    : user.Department.Name
            })
            .ToListAsync();

        return new PagedResult<UserResponseDto>
        {
            Items = items,
            Page = parameters.Page,
            PageSize = parameters.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    totalCount / (double)parameters.PageSize)
        };
    }

    public async Task<UserCreationResult> CreateAsync(
        CreateUserDto input)
    {
        var normalizedEmail = input.Email.Trim();

        if (await _userManager.FindByEmailAsync(normalizedEmail) is not null)
        {
            return new UserCreationResult
            {
                Outcome = UserCreationOutcome.Conflict,
                Errors = ["A user with this email already exists."]
            };
        }

        Department? department = null;

        if (input.DepartmentId.HasValue)
        {
            department = await _context.Departments
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.Id == input.DepartmentId.Value);

            if (department is null)
            {
                return new UserCreationResult
                {
                    Outcome = UserCreationOutcome.InvalidDepartment,
                    Errors = ["The selected department does not exist."]
                };
            }
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            EmailConfirmed = true,
            FullName = input.FullName.Trim(),
            DepartmentId = input.DepartmentId
        };

        var createResult = await _userManager.CreateAsync(
            user,
            input.Password);

        if (!createResult.Succeeded)
        {
            var hasConflict = createResult.Errors.Any(error =>
                error.Code == nameof(IdentityErrorDescriber.DuplicateEmail) ||
                error.Code == nameof(IdentityErrorDescriber.DuplicateUserName));

            return new UserCreationResult
            {
                Outcome = hasConflict
                    ? UserCreationOutcome.Conflict
                    : UserCreationOutcome.ValidationFailed,
                Errors = createResult.Errors
                    .Select(error => error.Description)
                    .ToList()
            };
        }

        var roleResult = await _userManager.AddToRoleAsync(
            user,
            input.Role);

        if (!roleResult.Succeeded)
        {
            return new UserCreationResult
            {
                Outcome = UserCreationOutcome.ValidationFailed,
                Errors = roleResult.Errors
                    .Select(error => error.Description)
                    .ToList()
            };
        }

        await transaction.CommitAsync();

        return new UserCreationResult
        {
            Outcome = UserCreationOutcome.Created,
            User = new UserResponseDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = input.Role,
                DepartmentId = user.DepartmentId,
                DepartmentName = department?.Name
            }
        };
    }
}
