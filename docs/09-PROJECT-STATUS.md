# 09 — Current Project Status (living document)

**Last written:** 2026-10-14  
**Source of truth:** Actual repository + verified commands + developer confirmation.

## Goal

Learn C# and ASP.NET Core by building a real internal request approval system. Use solo/no-cost setup and progress in small steps with Codex. Microservices **after** a usable MVP.

## M0 verification — complete

- [x] Created repository folder: `request-management-system`.
- [x] Created backend project: `backend/RequestManagementSystem.Api` using Web API Controller template.
- [x] Installed .NET SDK; ASP.NET Core project has run successfully.
- [x] Accessed the default `/weatherforecast` endpoint and received JSON.
- [x] `Models/Request.cs` contains `Id`, `Title`, `Description`, `Status`, and `CreatedAt`.
- [x] `RequestService.GetAll()` returns `List<Request>` with demo data.
- [x] `Program.cs` registers `RequestService` with `AddScoped<RequestService>()`.
- [x] `RequestsController : ControllerBase` receives the service through conventional constructor injection.
- [x] `GET /api/requests` calls `_requestService.GetAll()` and returns `Ok(...)`.
- [x] `dotnet build` succeeded on 2026-10-14 with 0 errors.
- [x] Developer confirms `GET /api/requests` is running and returns JSON successfully.

### Naming note

The class names are correct, but the current filenames are `Services/RequestServices.cs` and
`Controllers/RequestsControllers.cs`. Singular filenames would better match the public classes,
but this does not affect compilation or runtime behavior. No source rename was made because M0 is
working and the developer requested no code changes unless there is an error.

## Not implemented / no evidence yet

- [ ] `RequestService.GetAll()` still uses hardcoded data; it does not query EF Core yet.
- [ ] Stored request data, create/detail endpoints, validation DTOs.
- [ ] Authentication/JWT/roles/ownership.
- [ ] Approval/rejection workflow and status history.
- [ ] Frontend React, test project, CI, Docker.
- [ ] RabbitMQ or independent microservices.

## CURRENT milestone

**M1 — EF Core + SQL Server + first tests.** In progress.

### M1 progress

- [x] Selected the existing local default SQL Server instance (`MSSQLSERVER`); Docker is not needed.
- [x] Added EF Core SQL Server and Design packages version `10.0.13`.
- [x] Added `Data/AppDbContext.cs` with `DbSet<Request>`.
- [x] Stored `DefaultConnection` in .NET User Secrets using Windows authentication.
- [x] Registered `AppDbContext` with `UseSqlServer()` in `Program.cs`.
- [x] Installed local `dotnet-ef` tool version `10.0.13`.
- [x] Generated the `InitialCreate` migration.
- [x] Developer confirmed the migration was applied and the SQL Server database was created successfully.
- [x] `RequestService.GetAllAsync()` queries SQL Server through `AppDbContext` with `AsNoTracking()`.
- [x] `RequestsController.GetAll()` awaits the service; developer confirmed `GET /api/requests` returns the 3 manually inserted rows.
- [x] Developer chose not to add automatic development seed data; local demo rows are managed manually to avoid leftover sample data.
- [x] Created `tests/RequestManagementSystem.Tests` with xUnit and a project reference to the API.
- [x] Added `Microsoft.AspNetCore.Mvc.Testing` 10.0.13 and exposed the generated `Program` entry point for `WebApplicationFactory`.
- [x] Added `CustomWebApplicationFactory` that replaces SQL Server with an isolated SQLite in-memory database for M1 tests.
- [x] Replaced the empty template test with one meaningful integration test for `GET /api/requests`.
- [x] Integration test verifies HTTP 200, two returned records, and expected response content.
- [x] README documents SQL Server/User Secrets setup, local EF tool restore, migration update, API run, and test commands.

### NEXT SINGLE TASK for Codex

Resolve the current NuGet vulnerability warnings by updating `Microsoft.AspNetCore.OpenApi` to
10.0.13 and pinning `System.Security.Cryptography.Pkcs` to a patched compatible version, then rerun
build/test/package audit. Do not start M2 or initialize/push Git in the same task.

### M1 completion review

- **PASS:** EF Core + SQL Server configuration, initial migration, persistent local database, and async GET query.
- **PASS:** `GET /api/requests` returns SQL Server data, confirmed by the developer.
- **PASS:** SQLite-isolated integration test exercises HTTP -> Controller -> Service -> EF Core and passes 1/1.
- **PASS:** README documents local setup, migrations, API run, and test commands.
- **FAIL:** dependency audit/build reports high-severity `NU1903` warnings for vulnerable transitive packages.
- **NOT READY TO PUSH:** project root has no `.git` repository or `.gitignore`; Git currently resolves an unrelated parent repository.

## Last verification report

- Date: 2026-10-14
- Build command: `dotnet build backend/RequestManagementSystem.Api/RequestManagementSystem.Api.csproj`
- Build result on 2026-10-14: **succeeded**, 0 errors, with unresolved `NU1902`/`NU1903` vulnerability warnings.
- Endpoint: developer confirms `GET /api/requests` returns the 3 rows inserted manually in SQL Server.
- Test project: `tests/RequestManagementSystem.Tests` exists and references the API project.
- Test command: `dotnet test tests/RequestManagementSystem.Tests/RequestManagementSystem.Tests.csproj`
- Test result: **passed**, 1/1 meaningful GET integration test. In the restricted Codex process, Windows Event Log access required the temporary environment override `Logging__EventLog__LogLevel__Default=None`; the developer also reported a successful local run.
- Database: developer confirms `dotnet ef database update` completed and created the SQL Server database.
- Blockers before marking M1 complete/pushing: vulnerable dependency versions; missing project-level `.gitignore` and Git repository setup.
- Next single task: update/pin the vulnerable packages and rerun build/test/audit.
