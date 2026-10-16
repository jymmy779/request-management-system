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

## Previous completed milestone

**M2 — Login, users, and RBAC. Complete.**

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
- [x] Registered the named `ManagerOrAdmin` and `CanAccessRequests` authorization policies.
- [x] Added `POST /api/auth/login` and manually verified login against SQL Server.
- [x] Added authenticated `GET /api/auth/me`, returning only `id`, `email`, and `role` through a DTO.
- [x] Added required `Request.RequesterId` ownership with an EF Core foreign key to `AspNetUsers` and `DeleteBehavior.Restrict`.
- [x] Backfilled the three existing local requests to the demo Employee before making `RequesterId` required.
- [x] Protected `GET /api/requests` with JWT authentication.
- [x] Filtered request lists by verified JWT identity and role: Employee sees owned requests; Manager/Admin see all.
- [x] Added integration tests for valid login, invalid password, `/me` without a token, and `/me` with a valid token.
- [x] Added request authorization tests for 401, Employee ownership isolation, Manager access, and Admin access.
- [x] Added login and `/me` role coverage for Employee, Manager, and Admin.
- [x] Applied `CanAccessRequests` to request endpoints, requiring Employee, Manager, or Admin.
- [x] Verified a valid JWT without a recognized application role receives `403 Forbidden`.
- [x] Developer confirmed the full integration suite passes: 11/11 tests on 2026-10-16.

### M2 completion note

- `ManagerOrAdmin` is reserved for approval workflow endpoints introduced in M4; M2 does not add a placeholder endpoint solely to exercise this policy.
- M2 acceptance criteria are complete: all three demo roles can authenticate, request visibility follows ownership and role, and 401/403 behavior is covered by integration tests.

## Latest completed milestone

**M3 — Create/detail and validation. Complete.**

### M3 verified progress

- [x] Added required `Request.Category` with the default value `Other`.
- [x] Centralized the three fixed MVP category values in `RequestCategories`; no category table is needed yet.
- [x] Generated and applied the `AddRequestCategory` migration; existing requests were assigned `Other`.
- [x] Verified the API project builds after adding the property: 0 errors on 2026-10-16.
- [x] Added `CreateRequestDto` validation for title, description, and the three supported categories.
- [x] Added `RequestResponseDto` so request APIs do not expose the EF navigation object.
- [x] Added `RequestService.CreateAsync()` to persist a new Pending request owned by the authenticated user and return a response DTO.
- [x] Added authenticated `POST /api/requests`; requester ownership comes only from the JWT and successful creation returns `201 Created`.
- [x] Renamed the shared request policy to `CanAccessRequests` so its name covers both viewing and creating requests.
- [x] Added an integration test proving valid creation returns 201, assigns JWT ownership, and persists the row.
- [x] Cleared platform-specific logging providers in the test host so integration tests do not require Windows Event Log access.
- [x] Added validation integration coverage for invalid title, description, and category; all return 400 without persisting data.
- [x] Added ownership-aware `RequestService.GetByIdAsync()` with DTO projection; inaccessible requests return no result.
- [x] Added `GET /api/requests/{id}`, returning 200 for a visible request and 404 when missing or outside an Employee's ownership scope.
- [x] Added request-detail integration coverage for owner access, Employee isolation, Manager/Admin access, and a missing ID.
- [x] Changed request-list queries to return `RequestResponseDto` projections instead of EF entities.

### M3 completion note

- Request creation and detail retrieval persist correctly and enforce validation, authentication, roles, and ownership.
- Request history is intentionally deferred to the complete audit implementation in M4, as allowed by the milestone plan.
- The full integration suite passes 20/20 tests.

## Current milestone

**M4 — Approval workflow and audit. Not started.**

## Known technical debt

- NuGet currently reports `NU1902`/`NU1903` vulnerability warnings for `Microsoft.OpenApi` 2.0.0 and `System.Security.Cryptography.Pkcs` 9.0.11. These warnings have not been resolved yet.

## Last verification

- Date: 2026-10-16
- API build: developer confirmed `dotnet build backend/RequestManagementSystem.Api/RequestManagementSystem.Api.csproj` succeeded after extending the demo-user seeder.
- Database: developer confirmed Employee, Manager, and Admin demo users exist and each has the correct role in local SQL Server.
- Manual auth: developer confirmed login returns an access token.
- Tests: `dotnet test tests/RequestManagementSystem.Tests/RequestManagementSystem.Tests.csproj` passes 20/20 tests after adding request-detail authorization and not-found coverage.

## Next single task

Start M4 by defining the five request status constants and replacing the existing raw `Pending` strings; do not implement transitions yet.
