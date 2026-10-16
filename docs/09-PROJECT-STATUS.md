# 09 — Current Project Status (living document)

**Last updated:** 2026-10-16  
**Source of truth:** Actual repository, verified commands, and developer confirmation.

## Goal

Learn C# and ASP.NET Core by building a small internal request approval system. Work incrementally and keep the MVP as one maintainable application.

## Completed milestones

### M0 — Bootstrap and first API

- [x] Created the ASP.NET Core Web API project.
- [x] Added `Request`, `RequestService`, and `RequestsController`.
- [x] Registered the service through dependency injection.
- [x] Verified `GET /api/requests` returns JSON.

### M1 — EF Core, SQL Server, and first test

- [x] Added EF Core SQL Server and `AppDbContext`.
- [x] Stored the local connection string in .NET User Secrets.
- [x] Generated and applied the `InitialCreate` migration.
- [x] Changed `RequestService.GetAllAsync()` to query SQL Server with `AsNoTracking()`.
- [x] Verified `GET /api/requests` returns rows stored in SQL Server.
- [x] Added an xUnit integration-test project.
- [x] Tests replace SQL Server with an isolated SQLite in-memory database.
- [x] Added a meaningful integration test for `GET /api/requests`.
- [x] Documented restore, migration, run, and test commands in README.
- [x] Initialized the Git repository, added `.gitignore`, and pushed the initial project to GitHub.

## Current milestone

**M2 — Login, users, and RBAC. In progress.**

### M2 verified progress

- [x] Added ASP.NET Core Identity with `ApplicationUser` and Identity-backed `AppDbContext`.
- [x] Generated and applied the `AddIdentity` migration to local SQL Server.
- [x] Added the Employee, Manager, and Admin role constants.
- [x] Added an idempotent role seeder and verified the roles exist in SQL Server.
- [x] Configured JWT Bearer authentication using a signing key from User Secrets.
- [x] Added JWT authentication and authorization middleware in the correct order.
- [x] Added `TokenService` to create signed access tokens containing user and role claims.
- [x] Added an idempotent development-only seeder for Employee, Manager, and Admin accounts; no password is committed.
- [x] Verified all three demo users and their role assignments in local SQL Server.
- [x] Registered the named `ManagerOrAdmin` authorization policy with restrictive user-delete behavior planned for owned requests.
- [x] Added `POST /api/auth/login` and manually verified login against SQL Server.
- [x] Added authenticated `GET /api/auth/me`, returning only `id`, `email`, and `role` through a DTO.
- [x] Added required `Request.RequesterId` ownership with an EF Core foreign key to `AspNetUsers` and `DeleteBehavior.Restrict`.
- [x] Backfilled the three existing local requests to the demo Employee before making `RequesterId` required.
- [x] Protected `GET /api/requests` with JWT authentication.
- [x] Filtered request lists by verified JWT identity and role: Employee sees owned requests; Manager/Admin see all.
- [x] Added integration tests for valid login, invalid password, `/me` without a token, and `/me` with a valid token.
- [x] Added request authorization tests for 401, Employee ownership isolation, Manager access, and Admin access.
- [x] Developer confirmed the full integration suite passes: 8/8 tests on 2026-10-16.

### Remaining M2 work

- [ ] Apply role-based authorization policies to protected business endpoints.
- [ ] Add Manager/Admin login-role coverage and a 403 test when a real Manager/Admin-only endpoint is implemented.

## Known technical debt

- NuGet currently reports `NU1902`/`NU1903` vulnerability warnings for `Microsoft.OpenApi` 2.0.0 and `System.Security.Cryptography.Pkcs` 9.0.11. These warnings have not been resolved yet.

## Last verification

- Date: 2026-10-16
- API build: developer confirmed `dotnet build backend/RequestManagementSystem.Api/RequestManagementSystem.Api.csproj` succeeded after extending the demo-user seeder.
- Database: developer confirmed Employee, Manager, and Admin demo users exist and each has the correct role in local SQL Server.
- Manual auth: developer confirmed login returns an access token.
- Tests: developer confirmed `dotnet test tests/RequestManagementSystem.Tests/RequestManagementSystem.Tests.csproj` passes 8/8 tests after JWT protection and ownership filtering.

## Next single task

Add authentication integration coverage proving Manager and Admin logins receive their correct role through `GET /api/auth/me`.
